// ============================================================
// ConstructionCellTargetSession — 건설 셀 타겟팅 (커서·3D 고스트·회전·취소)
// ============================================================

using Garunnir.Runtime.Gameplay.Data;
using IsoTilemap;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class ConstructionCellTargetSession : MonoBehaviour, IFarmCellTargetSession, IUiCancelConsumer
{
    static ConstructionCellTargetSession _active;

    GridCursor _gridCursor;
    ConstructionActionHost _actionHost;
    ConstructionData _data;
    CellTargetPreview3D _preview;
    CraftingMaterialPool _pool;
    TileMapManager _mapManager;

    public static bool IsActive => _active != null;

    public int CancelPriority => UiCancelPriority.ConstructionCellTarget;

    public static bool TryBegin(ConstructionData data)
    {
        if (data == null ||
            IsActive ||
            UIConstruction.IsOpen ||
            UIConstructionController.IsGameplayOpen ||
            FarmCellTargetSession.IsActive ||
            FishCellTargetSession.IsActive)
            return false;

        ConstructionCellTargetSession session = EnsureInstance();
        return session.BeginInternal(data);
    }

    public static bool TryConsumeRightClick()
    {
        if (_active == null)
            return false;

        _active.Cancel();
        return true;
    }

    static ConstructionCellTargetSession EnsureInstance()
    {
        if (_active != null)
            return _active;

        var go = new GameObject(nameof(ConstructionCellTargetSession));
        _active = go.AddComponent<ConstructionCellTargetSession>();
        return _active;
    }

    void OnEnable() => UiCancelRouter.Register(this);

    void OnDisable() => UiCancelRouter.Unregister(this);

    void Update()
    {
        _gridCursor?.SyncFromPointer();
        TryHandleRotate();
        TryHandlePrimaryClick();
    }

    void TryHandleRotate()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null || !kb.rKey.wasPressedThisFrame)
            return;

        _preview?.RotateStep(+1);
        if (_gridCursor != null)
            NotifyHoverForCurrent();
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

    bool BeginInternal(ConstructionData data)
    {
        _data = data;
        _pool = ConstructionService.CreatePoolFromActivePlayer();
        _mapManager = FindFirstObjectByType<TileMapManager>();

        _gridCursor = FindFirstObjectByType<GridCursor>(FindObjectsInactive.Include);
        if (_gridCursor == null)
        {
            Debug.LogError("[ConstructionCellTargetSession] GridCursor not found.");
            return false;
        }

        ResolveActionHost();
        if (_actionHost == null)
        {
            Debug.LogError("[ConstructionCellTargetSession] ConstructionActionHost missing on possessed body.");
            return false;
        }

        _preview = new CellTargetPreview3D();
        TilePlacementSlot slot = ConstructionService.ResolvePostSlot(data);
        _preview.BeginTileGhostMode(slot);

        if (_mapManager != null &&
            _mapManager.PrefabDB != null &&
            _mapManager.PrefabDB.TryGetDefinition(data.post_prefab_id, out TileDefinition def) &&
            def != null &&
            def.prefab != null)
        {
            _preview.SetTileGhostPrefab(def.prefab);
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
                _actionHost = gear.gameObject.AddComponent<ConstructionActionHost>();
        }

        if (_actionHost == null && PlayerInventoryRuntime.Active?.Host != null)
        {
            if (!PlayerInventoryRuntime.Active.Host.TryGetComponent(out _actionHost))
                _actionHost = PlayerInventoryRuntime.Active.Host.gameObject
                    .AddComponent<ConstructionActionHost>();
        }
    }

    public bool CanApply(Vector3Int cell)
    {
        _pool = ConstructionService.CreatePoolFromActivePlayer();
        return ConstructionService.CanBuild(
            _data,
            cell,
            _pool,
            _mapManager,
            _preview != null ? _preview.FacingQuarters : 0);
    }

    public void OnCellHover(Vector3Int cell, bool canApply)
    {
        Color tint = canApply
            ? ConstructionConsts.TargetPreviewValid
            : ConstructionConsts.TargetPreviewInvalid;
        _gridCursor?.SetTargetTint(tint);

        float cellSize = _mapManager != null && _mapManager.WorldGrid != null
            ? _mapManager.WorldGrid.CellSize
            : 1f;
        _preview?.ShowTileAtCell(cell, cellSize, canApply);
    }

    void NotifyHoverForCurrent()
    {
        // Force hover refresh after rotate — GridCursor keeps last cell via Sync.
        _gridCursor?.SyncFromPointer();
    }

    public bool TryConfirm(Vector3Int cell)
    {
        if (!CanApply(cell))
            return false;

        ConstructionData data = _data;
        int facing = _preview != null ? _preview.FacingQuarters : 0;
        ConstructionActionHost host = _actionHost;

        EndTargeting();
        host.TryRun(data, cell, facing);
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
