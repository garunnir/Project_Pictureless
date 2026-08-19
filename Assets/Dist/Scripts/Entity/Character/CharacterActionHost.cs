// ============================================================
// CharacterActionHost — 행위자 1줄 행동 큐 + CancelAll + TickScale
// ============================================================

using System;
using System.Collections.Generic;
using Garunnir.Runtime.Gameplay.Data;
using UnityEngine;

[DefaultExecutionOrder(-50)]
[DisallowMultipleComponent]
public sealed class CharacterActionHost : MonoBehaviour
{
    struct Job
    {
        public CharacterActionKind Kind;
        public Func<bool> Start;
    }

    [SerializeField] UICraftingController _crafting;

    readonly Queue<Job> _queue = new();
    readonly List<BodyPartEffect> _effectScratch = new(16);

    CharacterBodyHost _bodyHost;
    PlayerGearHost _gearHost;
    InventoryTimedMoveHost _moveHost;
    CharacterAttacker _attacker;
    CharacterActionKind _currentKind;
    bool _dispatching;
    float _tickScale = 1f;

    public CharacterActionKind CurrentKind => _currentKind;
    public int QueueCount => _queue.Count;
    public bool IsDispatching => _dispatching;
    public float ActionTickScale => _tickScale;
    public bool IsBusy => _currentKind != CharacterActionKind.None || _queue.Count > 0;

    public bool HasCancellableWork =>
        _queue.Count > 0 ||
        (_currentKind != CharacterActionKind.None && _currentKind != CharacterActionKind.Combat);

    public event Action Changed;

    public float Progress01
    {
        get
        {
            switch (_currentKind)
            {
                case CharacterActionKind.Gear:
                    return _gearHost != null && _gearHost.Timed != null
                        ? _gearHost.Timed.Progress01
                        : 0f;
                case CharacterActionKind.Inventory:
                    return _moveHost != null ? _moveHost.Progress01 : 0f;
                case CharacterActionKind.Craft:
                    return _crafting != null ? _crafting.CraftProgress01 : 0f;
                case CharacterActionKind.Combat:
                    return _attacker != null ? _attacker.CooldownProgress01 : 0f;
                default:
                    return 0f;
            }
        }
    }

    void Awake()
    {
        TryGetComponent(out _bodyHost);
        TryGetComponent(out _gearHost);
        TryGetComponent(out _moveHost);
        TryGetComponent(out _attacker);
        if (_crafting == null)
            _crafting = FindAnyObjectByType<UICraftingController>();
        RefreshTickScale();
    }

    void Update()
    {
        // Rule 6: scratch 재사용. TickScale·소스 idle 폴링만. 할당 없음.
        RefreshTickScale();
        if (_currentKind == CharacterActionKind.None)
        {
            TryDequeue();
            return;
        }

        if (IsSourceBusy(_currentKind))
            return;

        _currentKind = CharacterActionKind.None;
        Changed?.Invoke();
        TryDequeue();
    }

    public bool TryRunOrEnqueue(CharacterActionKind kind, Func<bool> start)
    {
        if (start == null || kind == CharacterActionKind.None)
            return false;

        if (_dispatching)
            return start();

        if (_currentKind != CharacterActionKind.None)
        {
            _queue.Enqueue(new Job { Kind = kind, Start = start });
            Changed?.Invoke();
            return true;
        }

        return BeginNow(kind, start);
    }

    public void CancelAll()
    {
        _queue.Clear();
        if (_currentKind == CharacterActionKind.Combat)
        {
            Changed?.Invoke();
            return;
        }

        CancelCurrentWork();
        _currentKind = CharacterActionKind.None;
        Changed?.Invoke();
    }

    void RefreshTickScale()
    {
        ICharacterBody body = _bodyHost != null ? _bodyHost.Body : null;
        _tickScale = CharacterActionDelay.TickScale(body, _effectScratch);
    }

    bool BeginNow(CharacterActionKind kind, Func<bool> start)
    {
        _currentKind = kind;
        _dispatching = true;
        bool ok;
        try
        {
            ok = start();
        }
        finally
        {
            _dispatching = false;
        }

        if (!ok)
        {
            _currentKind = CharacterActionKind.None;
            return false;
        }

        if (!IsSourceBusy(kind))
        {
            _currentKind = CharacterActionKind.None;
            Changed?.Invoke();
            TryDequeue();
            return true;
        }

        Changed?.Invoke();
        return true;
    }

    void TryDequeue()
    {
        while (_currentKind == CharacterActionKind.None && _queue.Count > 0)
        {
            Job job = _queue.Dequeue();
            if (BeginNow(job.Kind, job.Start))
                return;
        }
    }

    bool IsSourceBusy(CharacterActionKind kind)
    {
        switch (kind)
        {
            case CharacterActionKind.Gear:
                return _gearHost != null && _gearHost.Service != null && _gearHost.Service.IsBusy;
            case CharacterActionKind.Inventory:
                return _moveHost != null && _moveHost.IsBusy;
            case CharacterActionKind.Craft:
                return _crafting != null && _crafting.IsCraftRunning;
            case CharacterActionKind.Combat:
                return _attacker != null && _attacker.IsActionBusy;
            default:
                return false;
        }
    }

    void CancelCurrentWork()
    {
        switch (_currentKind)
        {
            case CharacterActionKind.Gear:
                _gearHost?.Timed?.Cancel();
                break;
            case CharacterActionKind.Inventory:
                _moveHost?.Cancel();
                break;
            case CharacterActionKind.Craft:
                _crafting?.CancelRunningCraft();
                break;
        }
    }

    void OnDisable()
    {
        _queue.Clear();
        if (_currentKind != CharacterActionKind.Combat)
            CancelCurrentWork();
        _currentKind = CharacterActionKind.None;
    }
}
