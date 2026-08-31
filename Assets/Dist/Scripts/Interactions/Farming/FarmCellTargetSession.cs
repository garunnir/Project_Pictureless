// ============================================================
// FarmCellTargetSession — 농사 셀 클릭 타겟팅 (커서·프리뷰·취소)
// ============================================================

using Garunnir.Runtime.Gameplay.Data;
using IsoTilemap;
using UnityEngine;

public sealed class FarmCellTargetSession : MonoBehaviour, IFarmCellTargetSession, IUiCancelConsumer
{
    static FarmCellTargetSession _active;

    GridCursor _gridCursor;
    FarmCellActionHost _actionHost;
    FarmCellActionKind _kind;
    ItemStack _stack;
    InventoryContainer _container;
    bool _showPlantPreview;
    CellTargetPreview3D _preview;

    public static bool IsActive => _active != null;

    public int CancelPriority => UiCancelPriority.FarmCellTarget;

    public static bool TryBegin(
        FarmCellActionKind kind,
        ItemStack stack,
        InventoryContainer container)
    {
        if (IsActive || UIConstruction.IsOpen || ConstructionCellTargetSession.IsActive)
            return false;

        FarmCellTargetSession session = EnsureInstance();
        return session.BeginInternal(kind, stack, container);
    }

    public static bool TryConsumeRightClick()
    {
        if (_active == null)
            return false;

        _active.Cancel();
        return true;
    }

    static FarmCellTargetSession EnsureInstance()
    {
        if (_active != null)
            return _active;

        var go = new GameObject(nameof(FarmCellTargetSession));
        _active = go.AddComponent<FarmCellTargetSession>();
        return _active;
    }

    void OnEnable() => UiCancelRouter.Register(this);

    void OnDisable() => UiCancelRouter.Unregister(this);

    void Update()
    {
        _gridCursor?.SyncFromPointer();
        TryHandlePrimaryClick();
    }

    void TryHandlePrimaryClick()
    {
        InputManager input = InputManager.Instance;
        if (input == null ||
            !input.TryReadPointerPressedThisFrame(out bool pressed) ||
            !pressed)
            return;

        _gridCursor?.TryConfirmTargetingClick();
    }

    bool BeginInternal(
        FarmCellActionKind kind,
        ItemStack stack,
        InventoryContainer container)
    {
        _kind = kind;
        _stack = stack;
        _container = container;
        _showPlantPreview = kind == FarmCellActionKind.Plant;

        _gridCursor = FindFirstObjectByType<GridCursor>(FindObjectsInactive.Include);
        if (_gridCursor == null)
        {
            Debug.LogError("[FarmCellTargetSession] GridCursor not found.");
            return false;
        }

        ResolveActionHost();
        if (_actionHost == null)
        {
            Debug.LogError("[FarmCellTargetSession] FarmCellActionHost missing on possessed body.");
            return false;
        }

        if (_showPlantPreview)
        {
            _preview = new CellTargetPreview3D();
            _preview.BeginPlantMode();
        }

        _active = this;
        _gridCursor.BeginTargeting(this);
        return true;
    }

    void ResolveActionHost()
    {
        _actionHost = null;
        PlayerGearHost gear = PlayerGearHost.Active;
        if (gear != null)
        {
            if (!gear.TryGetComponent(out _actionHost))
                _actionHost = gear.gameObject.AddComponent<FarmCellActionHost>();
        }

        if (_actionHost == null && PlayerInventoryRuntime.Active?.Host != null)
            PlayerInventoryRuntime.Active.Host.TryGetComponent(out _actionHost);
    }

    public bool CanApply(Vector3Int cell) =>
        MapPlantService.CanApplyAtCell(_kind, cell, _stack, _container);

    public void OnCellHover(Vector3Int cell, bool canApply)
    {
        Color tint = canApply
            ? MapPlantConsts.TargetPreviewValid
            : MapPlantConsts.TargetPreviewInvalid;
        _gridCursor?.SetTargetTint(tint);

        if (!_showPlantPreview || _preview == null)
            return;

        MapPlantHost host = MapPlantHost.Runtime;
        float cellSize = host != null ? host.CellSize : 1f;
        string seedItemId = _stack != null ? _stack.ItemId : null;
        _preview.ShowPlant(
            cell,
            cellSize,
            PlantGrowthStage.Harvestable,
            seedItemId,
            canApply);
    }

    public bool TryConfirm(Vector3Int cell)
    {
        if (!CanApply(cell))
            return false;

        FarmCellActionKind kind = _kind;
        ItemStack stack = _stack;
        InventoryContainer container = _container;
        FarmCellActionHost host = _actionHost;

        EndTargeting();
        host.TryRun(kind, cell, stack, container);
        Destroy(gameObject);
        return true;
    }

    public void OnCancel() => Cancel();

    public void Cancel()
    {
        if (!IsActive || _active != this)
            return;

        EndTargeting();
        Destroy(gameObject);
    }

    public bool TryHandleCancel()
    {
        if (_active != this)
            return false;

        Cancel();
        return true;
    }

    void EndTargeting()
    {
        _gridCursor?.EndTargeting();
        _preview?.Dispose();
        _preview = null;
        if (ReferenceEquals(_active, this))
            _active = null;
    }

    void OnDestroy()
    {
        EndTargeting();
    }
}
