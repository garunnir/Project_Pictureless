// ============================================================
// FarmCellTargetSession — 농사 셀 클릭 타겟팅 (커서·프리뷰·취소)
// ============================================================

using Garunnir.Runtime.Gameplay.Data;
using IsoTilemap;
using UnityEngine;
using UnityEngine.Rendering;

public sealed class FarmCellTargetSession : MonoBehaviour, IFarmCellTargetSession, IUiCancelConsumer
{
    static FarmCellTargetSession _active;

    GridCursor _gridCursor;
    FarmCellActionHost _actionHost;
    FarmCellActionKind _kind;
    ItemStack _stack;
    InventoryContainer _container;
    bool _showPlantPreview;

    GameObject _plantPreviewRoot;
    MeshFilter _plantPreviewFilter;
    MeshRenderer _plantPreviewMeshRenderer;
    SpriteRenderer _plantPreviewSpriteRenderer;
    Material _plantPreviewMaterial;

    public static bool IsActive => _active != null;

    public int CancelPriority => UiCancelPriority.FarmCellTarget;

    public static bool TryBegin(
        FarmCellActionKind kind,
        ItemStack stack,
        InventoryContainer container)
    {
        if (IsActive || UIConstruction.IsOpen)
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
            EnsurePlantPreview();

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

        if (!_showPlantPreview)
            return;

        EnsurePlantPreview();
        MapPlantHost host = MapPlantHost.Runtime;
        float cellSize = host != null ? host.CellSize : 1f;
        string seedItemId = _stack != null ? _stack.ItemId : null;
        MapPlantOverlayVisual.Apply(
            _plantPreviewRoot.transform,
            _plantPreviewFilter,
            _plantPreviewMeshRenderer,
            _plantPreviewSpriteRenderer,
            cell,
            cellSize,
            PlantGrowthStage.Harvestable,
            seedItemId);
        ApplyPreviewTint(tint);
        _plantPreviewRoot.SetActive(true);
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
        HidePlantPreview();
        if (ReferenceEquals(_active, this))
            _active = null;
    }

    void OnDestroy()
    {
        EndTargeting();
        if (_plantPreviewMaterial != null)
            Destroy(_plantPreviewMaterial);
    }

    void EnsurePlantPreview()
    {
        if (_plantPreviewRoot != null)
            return;

        GameObject prefab = Resources.Load<GameObject>(MapPlantConsts.TargetPreviewResourcesName);
        if (prefab != null)
        {
            _plantPreviewRoot = Object.Instantiate(prefab);
            _plantPreviewRoot.name = "FarmPlantTargetPreview";
        }
        else
        {
            Debug.LogWarning(
                "[FarmCellTargetSession] Prefab missing at Resources/" +
                MapPlantConsts.TargetPreviewResourcesName +
                " — building Mesh/Sprite children at runtime.");
            _plantPreviewRoot = new GameObject("FarmPlantTargetPreview");
            MapPlantVisualHierarchy.EnsureChildren(
                _plantPreviewRoot.transform,
                out _,
                out _,
                out _);
        }

        MapPlantVisualHierarchy.CacheFromRoot(
            _plantPreviewRoot.transform,
            out _plantPreviewFilter,
            out _plantPreviewMeshRenderer,
            out _plantPreviewSpriteRenderer);
        if (_plantPreviewMeshRenderer != null)
        {
            _plantPreviewMeshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            _plantPreviewMeshRenderer.receiveShadows = false;
        }

        if (_plantPreviewSpriteRenderer != null)
        {
            _plantPreviewSpriteRenderer.shadowCastingMode = ShadowCastingMode.Off;
            _plantPreviewSpriteRenderer.receiveShadows = false;
            _plantPreviewSpriteRenderer.enabled = false;
        }

        _plantPreviewRoot.SetActive(false);
    }

    void HidePlantPreview()
    {
        if (_plantPreviewRoot != null)
            _plantPreviewRoot.SetActive(false);
    }

    void ApplyPreviewTint(Color tint)
    {
        if (_plantPreviewSpriteRenderer != null &&
            _plantPreviewSpriteRenderer.enabled &&
            _plantPreviewSpriteRenderer.sprite != null)
        {
            _plantPreviewSpriteRenderer.color = tint;
            return;
        }

        if (_plantPreviewMeshRenderer == null)
            return;

        if (_plantPreviewMaterial == null)
        {
            Shader shader = Shader.Find(MapPlantConsts.OverlayShaderUrpUnlit);
            if (shader == null)
                shader = Shader.Find(MapPlantConsts.OverlayShaderUnlitColor);
            if (shader == null)
                return;

            _plantPreviewMaterial = new Material(shader) { name = "FarmPlantTargetPreview" };
            _plantPreviewMeshRenderer.sharedMaterial = _plantPreviewMaterial;
        }

        if (_plantPreviewMaterial.HasProperty("_BaseColor"))
            _plantPreviewMaterial.SetColor("_BaseColor", tint);
        else if (_plantPreviewMaterial.HasProperty("_Color"))
            _plantPreviewMaterial.SetColor("_Color", tint);
    }
}
