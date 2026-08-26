// ============================================================
// FishCellTargetSession — 낚시 셀 클릭 타겟팅 (커서·취소)
// ============================================================

using IsoTilemap;
using UnityEngine;

public sealed class FishCellTargetSession : MonoBehaviour, IFarmCellTargetSession, IUiCancelConsumer
{
    static FishCellTargetSession _active;

    GridCursor _gridCursor;
    FishCellActionHost _actionHost;
    FishCellActionKind _kind;
    ItemStack _stack;
    InventoryContainer _container;

    public static bool IsActive => _active != null;

    public int CancelPriority => UiCancelPriority.FishCellTarget;

    public static bool TryBegin(
        FishCellActionKind kind,
        ItemStack stack,
        InventoryContainer container)
    {
        if (IsActive || UIConstruction.IsOpen)
            return false;

        FishCellTargetSession session = EnsureInstance();
        return session.BeginInternal(kind, stack, container);
    }

    public static bool TryConsumeRightClick()
    {
        if (_active == null)
            return false;

        _active.Cancel();
        return true;
    }

    static FishCellTargetSession EnsureInstance()
    {
        if (_active != null)
            return _active;

        var go = new GameObject(nameof(FishCellTargetSession));
        _active = go.AddComponent<FishCellTargetSession>();
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
        FishCellActionKind kind,
        ItemStack stack,
        InventoryContainer container)
    {
        _kind = kind;
        _stack = stack;
        _container = container;

        _gridCursor = FindFirstObjectByType<GridCursor>(FindObjectsInactive.Include);
        if (_gridCursor == null)
        {
            Debug.LogError("[FishCellTargetSession] GridCursor not found.");
            return false;
        }

        ResolveActionHost();
        if (_actionHost == null)
        {
            Debug.LogError("[FishCellTargetSession] FishCellActionHost missing on possessed body.");
            return false;
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
                _actionHost = gear.gameObject.AddComponent<FishCellActionHost>();
        }

        if (_actionHost == null && PlayerInventoryRuntime.Active?.Host != null)
            PlayerInventoryRuntime.Active.Host.TryGetComponent(out _actionHost);
    }

    public bool CanApply(Vector3Int cell)
    {
        switch (_kind)
        {
            case FishCellActionKind.Cast:
                return MapFishService.CanCastAt(cell, _stack, _container);
            case FishCellActionKind.DeployTrap:
                return MapFishService.CanDeployTrapAt(cell, _stack, _container);
            case FishCellActionKind.CollectTrap:
                return MapFishService.CanCollectTrapAt(cell);
            default:
                return false;
        }
    }

    public void OnCellHover(Vector3Int cell, bool canApply)
    {
        Color tint = canApply
            ? MapFishConsts.TargetPreviewValid
            : MapFishConsts.TargetPreviewInvalid;
        _gridCursor?.SetTargetTint(tint);
    }

    public bool TryConfirm(Vector3Int cell)
    {
        if (!CanApply(cell))
            return false;

        FishCellActionKind kind = _kind;
        ItemStack stack = _stack;
        InventoryContainer container = _container;
        FishCellActionHost host = _actionHost;

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
        if (ReferenceEquals(_active, this))
            _active = null;
    }

    void OnDestroy() => EndTargeting();
}
