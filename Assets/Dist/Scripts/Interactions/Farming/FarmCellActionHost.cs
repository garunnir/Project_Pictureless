// ============================================================
// FarmCellActionHost — 농사 Arrive + Work + MapPlantService 적용
// ============================================================

using IsoTilemap;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public sealed class FarmCellActionHost : MonoBehaviour
{
    CharacterActionHost _actionHost;
    CharacterArriveHost _arriveHost;
    CharacterFarmWorkHost _workHost;
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
            _workHost = gameObject.AddComponent<CharacterFarmWorkHost>();
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
        FarmCellActionKind kind,
        Vector3Int cell,
        ItemStack stack,
        InventoryContainer container)
    {
        if (_arriveHost == null)
            return false;

        if (_actionHost == null)
            return BeginPipeline(kind, cell, stack, container);

        return _actionHost.TryRunOrEnqueue(
            CharacterActionKind.Map,
            () => BeginPipeline(kind, cell, stack, container));
    }

    bool BeginPipeline(
        FarmCellActionKind kind,
        Vector3Int cell,
        ItemStack stack,
        InventoryContainer container)
    {
        Vector3 destination = MapPlantService.CellArriveWorld(cell);
        float stopping = MapPlantService.CellArriveStoppingDistance();
        System.Func<bool> tryIsArrived = null;

        if (kind == FarmCellActionKind.Plant)
        {
            tryIsArrived = () =>
                MapPlantService.TryResolveActorCell(out Vector3Int playerCell) &&
                MapPlantService.IsWithinPlantActionRange(playerCell, cell);
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
        FarmCellActionKind kind,
        Vector3Int cell,
        ItemStack stack,
        InventoryContainer container)
    {
        if (kind == FarmCellActionKind.Plant &&
            !(MapPlantService.TryResolveActorCell(out Vector3Int playerCell) &&
              MapPlantService.IsWithinPlantActionRange(playerCell, cell)))
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

    static bool NeedsWork(FarmCellActionKind kind) =>
        kind == FarmCellActionKind.Plant ||
        kind == FarmCellActionKind.Till ||
        kind == FarmCellActionKind.Harvest ||
        kind == FarmCellActionKind.Chop;

    static void Apply(
        FarmCellActionKind kind,
        Vector3Int cell,
        ItemStack stack,
        InventoryContainer container)
    {
        switch (kind)
        {
            case FarmCellActionKind.Plant:
                MapPlantService.TryPlantAt(cell, stack, container);
                break;
            case FarmCellActionKind.Till:
                if (stack != null && container != null)
                    MapPlantService.TryTillAt(cell, stack, container);
                else
                    MapPlantService.TryTill(cell);
                break;
            case FarmCellActionKind.Fertilize:
                if (stack != null && container != null)
                    MapPlantService.TryFertilizeAt(cell, stack, container);
                else
                    MapPlantService.TryFertilize(cell);
                break;
            case FarmCellActionKind.Harvest:
                MapPlantService.TryHarvest(cell);
                break;
            case FarmCellActionKind.Chop:
                MapPlantService.TryChop(cell);
                break;
        }
    }
}
