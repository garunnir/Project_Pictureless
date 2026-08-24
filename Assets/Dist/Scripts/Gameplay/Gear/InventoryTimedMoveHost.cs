// ============================================================
// InventoryTimedMoveHost — MoveStacks 소요 시간 + 입력 차단(최소 UI)
// ============================================================

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// InventoryTransferDuration SSOT. 다중 스택은 합산 없이 1스택씩 순차.
/// 진행 중 IsBusy면 추가 이동 거부.
/// </summary>
public sealed class InventoryTimedMoveHost : MonoBehaviour
{
    [SerializeField] TimeScaleChannel _timeChannel = TimeScaleChannel.World;

    CharacterActionHost _actionHost;
    readonly GearTimedAction _timed = new();
    readonly List<ItemStack> _queue = new(8);
    readonly List<ItemStack> _activeStacks = new(1);
    readonly List<ItemStack> _single = new(1);

    InventorySession _session;
    InventoryContainer _from;
    InventoryContainer _to;
    Action _onApplied;
    bool _transferActive;
    bool _advancing;
    ItemStack _current;

    public static InventoryTimedMoveHost Active { get; private set; }

    public void ClaimActive() => Active = this;

    public bool IsBusy => _transferActive;
    public float Progress01 => _timed.Progress01;
    public GearTimedAction Timed => _timed;
    public IReadOnlyList<ItemStack> ActiveStacks => _activeStacks;

    public event Action Changed;

    void Awake()
    {
        TryGetComponent(out _actionHost);
    }

    void OnEnable()
    {
        _timed.Changed += OnTimedChanged;
        _timed.Completed += OnTimedCompleted;
        _timed.Cancelled += OnTimedCancelled;
    }

    void OnDisable()
    {
        _timed.Changed -= OnTimedChanged;
        _timed.Completed -= OnTimedCompleted;
        _timed.Cancelled -= OnTimedCancelled;
        if (Active == this)
            Active = null;
        Cancel();
    }

    void Update()
    {
        if (!_timed.IsRunning)
            return;
        float dt = TimeScaleService.Delta(_timeChannel);
        if (_actionHost != null)
            dt *= _actionHost.ActionTickScale;
        _timed.Tick(dt);
    }

    public bool IsStackActive(ItemStack stack)
    {
        if (stack == null || _activeStacks.Count == 0)
            return false;

        for (int i = 0; i < _activeStacks.Count; i++)
        {
            if (ReferenceEquals(_activeStacks[i], stack))
                return true;
        }

        return false;
    }

    public bool TryBeginMove(
        InventorySession session,
        InventoryContainer from,
        InventoryContainer to,
        IReadOnlyList<ItemStack> stacks,
        Action onApplied = null)
    {
        return RunOrEnqueue(() => TryBeginQueue(session, from, to, stacks, onApplied));
    }

    public bool TryBeginSequentialUntilFull(
        InventorySession session,
        InventoryContainer from,
        InventoryContainer to,
        IReadOnlyList<ItemStack> stacks,
        Action onApplied = null)
    {
        // 용량 초과 시 해당 스택에서 중단 — 순차 1스택 MoveStacks와 동일.
        return RunOrEnqueue(() => TryBeginQueue(session, from, to, stacks, onApplied));
    }

    public bool TryBegin(float durationSeconds, Action apply)
    {
        return TryBegin(durationSeconds, apply, activeStack: null);
    }

    public bool TryBegin(float durationSeconds, Action apply, ItemStack activeStack)
    {
        return RunOrEnqueue(() => TryBeginCore(durationSeconds, apply, activeStack));
    }

    bool TryBeginCore(float durationSeconds, Action apply, ItemStack activeStack)
    {
        if (IsBusy || apply == null)
            return false;

        _transferActive = true;
        _session = null;
        _from = null;
        _to = null;
        _onApplied = null;
        _queue.Clear();
        _current = null;
        ClearActiveStacks();
        if (activeStack != null)
            SetActiveStack(activeStack);

        if (!_timed.TryBegin(GearTimedAction.Kind.InventoryTransfer, durationSeconds, apply))
        {
            _transferActive = false;
            ClearActiveStacks();
            return false;
        }

        return true;
    }

    public void Cancel()
    {
        if (!_transferActive && !_timed.IsRunning)
            return;

        _queue.Clear();
        _current = null;
        _onApplied = null;
        _session = null;
        _from = null;
        _to = null;

        if (_timed.IsRunning)
            _timed.Cancel();
        else
            EndTransfer();
    }

    bool RunOrEnqueue(Func<bool> start)
    {
        if (MoodGameplayGate.IsBlocked)
            return false;
        if (start == null)
            return false;
        if (_actionHost == null)
            return start();
        return _actionHost.TryRunOrEnqueue(CharacterActionKind.Inventory, start);
    }

    bool TryBeginQueue(
        InventorySession session,
        InventoryContainer from,
        InventoryContainer to,
        IReadOnlyList<ItemStack> stacks,
        Action onApplied)
    {
        if (IsBusy || session == null || from == null || to == null || stacks == null || stacks.Count == 0)
            return false;

        _queue.Clear();
        for (int i = 0; i < stacks.Count; i++)
        {
            if (stacks[i] != null)
                _queue.Add(stacks[i]);
        }

        if (_queue.Count == 0)
            return false;

        _session = session;
        _from = from;
        _to = to;
        _onApplied = onApplied;
        _transferActive = true;
        _current = null;

        AdvanceQueue();
        return true;
    }

    void AdvanceQueue()
    {
        if (_advancing)
            return;

        _advancing = true;
        try
        {
            while (_queue.Count > 0)
            {
                ItemStack stack = _queue[0];
                _queue.RemoveAt(0);
                if (stack == null)
                    continue;

                _current = stack;
                SetActiveStack(stack);

                float duration = InventoryTransferDuration.SecondsForStackFrom(_from, stack);
                if (duration <= 0f)
                {
                    if (!ApplyCurrentStack())
                    {
                        FinishQueue();
                        return;
                    }

                    continue;
                }

                if (!_timed.TryBegin(GearTimedAction.Kind.InventoryTransfer, duration, RunQueuedApply))
                {
                    FinishQueue();
                    return;
                }

                return;
            }

            FinishQueue();
        }
        finally
        {
            _advancing = false;
        }
    }

    void RunQueuedApply() => ApplyCurrentStack();

    bool ApplyCurrentStack()
    {
        ItemStack stack = _current;
        _current = null;
        if (stack == null || _session == null || _from == null || _to == null)
            return false;

        _single.Clear();
        _single.Add(stack);
        if (!_session.MoveStacks(_from, _to, _single))
        {
            _queue.Clear();
            return false;
        }

        return true;
    }

    void OnTimedCompleted()
    {
        if (!_transferActive)
            return;

        if (_queue.Count > 0)
        {
            AdvanceQueue();
            return;
        }

        FinishQueue();
    }

    void OnTimedCancelled()
    {
        _queue.Clear();
        _current = null;
        _onApplied = null;
        EndTransfer();
    }

    void FinishQueue()
    {
        Action applied = _onApplied;
        _onApplied = null;
        EndTransfer();
        applied?.Invoke();
    }

    void EndTransfer()
    {
        _transferActive = false;
        _session = null;
        _from = null;
        _to = null;
        _queue.Clear();
        _current = null;
        ClearActiveStacks();
    }

    void SetActiveStack(ItemStack stack)
    {
        _activeStacks.Clear();
        if (stack != null)
            _activeStacks.Add(stack);
        Changed?.Invoke();
    }

    void ClearActiveStacks()
    {
        if (_activeStacks.Count == 0)
            return;
        _activeStacks.Clear();
        Changed?.Invoke();
    }

    void OnTimedChanged() => Changed?.Invoke();
}
