// ============================================================
// FishCellActionHost — 낚시 Arrive + Work + MapFishService 적용
// ============================================================

using IsoTilemap;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public sealed class FishCellActionHost : MonoBehaviour
{
    CharacterActionHost _actionHost;
    CharacterArriveHost _arriveHost;
    CharacterFishWorkHost _workHost;
    bool _moveCancelSubscribed;
    bool _pipelineActive;

    public bool IsBusy =>
        _pipelineActive ||
        (_arriveHost != null && _arriveHost.IsBusy) ||
        (_workHost != null && _workHost.IsBusy);

    public float WorkProgress01 =>
        _workHost != null && _workHost.IsBusy ? _workHost.Progress01 : 0f;

    void Awake()
    {
        TryGetComponent(out _actionHost);
        TryGetComponent(out _arriveHost);
        TryGetComponent(out _workHost);
        if (_arriveHost == null)
            _arriveHost = gameObject.AddComponent<CharacterArriveHost>();
        if (_workHost == null)
            _workHost = gameObject.AddComponent<CharacterFishWorkHost>();
        if (FishWorkClipCatalog.Runtime != null)
            _workHost.SetClipCatalog(FishWorkClipCatalog.Runtime);
    }

    void OnDisable()
    {
        UnsubscribeMoveCancel();
        _pipelineActive = false;
    }

    public void Cancel()
    {
        UnsubscribeMoveCancel();
        _pipelineActive = false;
        _arriveHost?.Cancel();
        _workHost?.Cancel();
    }

    public bool TryRun(
        FishCellActionKind kind,
        Vector3Int cell,
        ItemStack stack,
        InventoryContainer container)
    {
        if (_arriveHost == null)
            return false;

        if (_actionHost == null)
            return BeginPipeline(kind, cell, stack, container);

        return _actionHost.TryRunOrEnqueue(
            CharacterActionKind.Cell,
            () => BeginPipeline(kind, cell, stack, container));
    }

    bool BeginPipeline(
        FishCellActionKind kind,
        Vector3Int cell,
        ItemStack stack,
        InventoryContainer container)
    {
        Vector3 destination = MapFishService.CellArriveWorld(cell);
        float stopping = MapFishService.CellArriveStoppingDistance();
        System.Func<bool> tryIsArrived = null;

        if (kind == FishCellActionKind.Cast)
        {
            tryIsArrived = () =>
                MapFishService.TryResolveActorCell(out Vector3Int playerCell) &&
                MapFishService.IsWithinCastActionRange(playerCell, cell);
        }
        else if (kind == FishCellActionKind.DeployTrap || kind == FishCellActionKind.CollectTrap)
        {
            tryIsArrived = () =>
                MapFishService.TryResolveActorCell(out Vector3Int playerCell) &&
                MapFishService.IsWithinCastActionRange(playerCell, cell);
        }

        bool started = _arriveHost.TryBegin(
            destination,
            stopping,
            () => OnArrived(kind, cell, stack, container),
            onCancelled: EndPipeline,
            suppressInput: true,
            tryIsArrived: tryIsArrived);

        if (!started)
            return false;

        _pipelineActive = true;
        SubscribeMoveCancel();
        return true;
    }

    void OnArrived(
        FishCellActionKind kind,
        Vector3Int cell,
        ItemStack stack,
        InventoryContainer container)
    {
        if ((kind == FishCellActionKind.Cast ||
             kind == FishCellActionKind.DeployTrap ||
             kind == FishCellActionKind.CollectTrap) &&
            !(MapFishService.TryResolveActorCell(out Vector3Int playerCell) &&
              MapFishService.IsWithinCastActionRange(playerCell, cell)))
        {
            EndPipeline();
            return;
        }

        if (!NeedsWork(kind))
        {
            Apply(kind, cell, stack, container);
            EndPipeline();
            return;
        }

        if (_workHost == null)
        {
            Apply(kind, cell, stack, container);
            EndPipeline();
            return;
        }

        if (!_workHost.TryBegin(kind, () =>
            {
                Apply(kind, cell, stack, container);
                EndPipeline();
            }))
        {
            EndPipeline();
        }
    }

    void EndPipeline()
    {
        UnsubscribeMoveCancel();
        _pipelineActive = false;
    }

    void SubscribeMoveCancel()
    {
        if (_moveCancelSubscribed)
            return;

        InputManager input = InputManager.Instance;
        if (input == null)
            return;

        input.PlayerMovePerformed += OnMovePerformedWhileBusy;
        _moveCancelSubscribed = true;
    }

    void UnsubscribeMoveCancel()
    {
        if (!_moveCancelSubscribed)
            return;

        InputManager input = InputManager.Instance;
        if (input != null)
            input.PlayerMovePerformed -= OnMovePerformedWhileBusy;

        _moveCancelSubscribed = false;
    }

    void OnMovePerformedWhileBusy(InputAction.CallbackContext ctx)
    {
        if (!IsBusy)
            return;

        Vector2 dir = ctx.ReadValue<Vector2>();
        if (dir.sqrMagnitude <= Mathf.Epsilon)
            return;

        if (_actionHost != null)
            _actionHost.CancelAll();
        else
            Cancel();
    }

    static bool NeedsWork(FishCellActionKind kind) =>
        kind == FishCellActionKind.Cast ||
        kind == FishCellActionKind.DeployTrap ||
        kind == FishCellActionKind.CollectTrap;

    static void Apply(
        FishCellActionKind kind,
        Vector3Int cell,
        ItemStack stack,
        InventoryContainer container)
    {
        switch (kind)
        {
            case FishCellActionKind.Cast:
                MapFishService.TryCastAt(cell, stack, container);
                break;
            case FishCellActionKind.DeployTrap:
                MapFishService.TryDeployTrapAt(cell, stack, container);
                break;
            case FishCellActionKind.CollectTrap:
                MapFishService.TryCollectTrapAt(cell);
                break;
        }
    }
}
