// ============================================================
// ConstructionActionHost — 건설 Arrive + Work + ConstructionService 적용
// ============================================================

using Garunnir.Runtime.Gameplay.Data;
using IsoTilemap;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public sealed class ConstructionActionHost : MonoBehaviour
{
    CharacterActionHost _actionHost;
    CharacterArriveHost _arriveHost;
    CharacterConstructionWorkHost _workHost;
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
            _workHost = gameObject.AddComponent<CharacterConstructionWorkHost>();
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

    public bool TryRun(ConstructionData data, Vector3Int cell, int facingQuarters)
    {
        if (_arriveHost == null || data == null)
            return false;

        if (_actionHost == null)
            return BeginPipeline(data, cell, facingQuarters);

        return _actionHost.TryRunOrEnqueue(
            CharacterActionKind.Cell,
            () => BeginPipeline(data, cell, facingQuarters));
    }

    bool BeginPipeline(ConstructionData data, Vector3Int cell, int facingQuarters)
    {
        Vector3 destination = ConstructionService.CellArriveWorld(cell);
        float stopping = ConstructionService.CellArriveStoppingDistance();

        bool started = _arriveHost.TryBegin(
            destination,
            stopping,
            () => OnArrived(data, cell, facingQuarters),
            onCancelled: EndPipeline,
            suppressInput: true);

        if (!started)
            return false;

        _pipelineActive = true;
        SubscribeMoveCancel();
        return true;
    }

    void OnArrived(ConstructionData data, Vector3Int cell, int facingQuarters)
    {
        float duration = ConstructionService.ResolveWorkDurationSeconds(data);
        if (_workHost == null)
        {
            Apply(data, cell, facingQuarters);
            EndPipeline();
            return;
        }

        if (!_workHost.TryBegin(duration, () =>
            {
                Apply(data, cell, facingQuarters);
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

    static void Apply(ConstructionData data, Vector3Int cell, int facingQuarters)
    {
        CraftingMaterialPool pool = ConstructionService.CreatePoolFromActivePlayer();
        TileMapManager map = Object.FindFirstObjectByType<TileMapManager>();
        TileMapController controller = Object.FindFirstObjectByType<TileMapController>();
        InventorySession session = PlayerInventoryRuntime.Active?.Session;

        ConstructionService.TryBuildAt(
            data,
            cell,
            pool,
            map,
            controller,
            facingQuarters,
            session);
    }
}
