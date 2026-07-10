// ============================================================
// SmallItemObject — ItemDefinitionSO의 월드 인스턴스 (1 GO = 1 ItemStack)
// ============================================================

using Garunnir.Runtime.Gameplay.Item;
using IsoTilemap;
using Sirenix.OdinInspector;
using UnityEngine;

public sealed class SmallItemObject : MonoBehaviour
{
    [Required, SerializeField] ItemDefinitionSO _definition;
    [SerializeField, Min(1)] int _count = 1;
    [SerializeField] SpriteRenderer _spriteRenderer;

    IWorldGrid _worldGrid;
    ItemStack _stack;
    Vector3Int _ownerCell;
    bool _ownerCellResolved;

    public ItemStack Stack => _stack;
    public Vector3Int OwnerCell => _ownerCell;

    public void Configure(ItemDefinitionSO definition, int count)
    {
        _definition = definition;
        _count = Mathf.Max(1, count);
        _stack = _definition != null ? new ItemStack(_definition, _count) : null;
        ApplyIcon();
    }

    public void BindStack(ItemStack stack)
    {
        if (stack?.Item == null)
            return;

        _definition = stack.Item;
        _count = stack.Count;
        _stack = stack;
        ApplyIcon();
    }

    public void BindWorldGrid(IWorldGrid worldGrid)
    {
        if (_worldGrid == worldGrid)
            return;

        bool shouldRegister = isActiveAndEnabled && _definition != null && _stack != null;
        if (shouldRegister && _ownerCellResolved)
            SmallItemRegistry.Unregister(this);

        _worldGrid = worldGrid;
        _ownerCellResolved = false;
        ResolveOwnerCell();

        if (shouldRegister && _ownerCellResolved)
            SmallItemRegistry.Register(this);
    }

    internal void NotifyPickedUp()
    {
        if (this != null && gameObject != null)
            Destroy(gameObject);
    }

    void Awake()
    {
        if (_definition == null)
        {
            Debug.LogWarning("[SmallItemObject] ItemDefinitionSO is not assigned.", this);
            return;
        }

        if (_stack == null)
            Configure(_definition, _count);
    }

    void OnEnable()
    {
        if (_definition == null || _stack == null)
            return;

        ResolveOwnerCell();
        SmallItemRegistry.Register(this);
    }

    void OnDisable() => SmallItemRegistry.Unregister(this);

    void ApplyIcon()
    {
        if (_spriteRenderer == null || _definition == null)
            return;

        _spriteRenderer.sprite = ItemVisualPresenter.GetDisplayIcon(_definition);
    }

    void ResolveOwnerCell()
    {
        if (_ownerCellResolved)
            return;

        _ownerCell = ResolveGridCell(transform.position);
        _ownerCellResolved = true;
    }

    Vector3Int ResolveGridCell(Vector3 worldPos) =>
        _worldGrid != null
            ? _worldGrid.WorldToCell(worldPos)
            : TileHelper.ConvertWorldToGrid(worldPos, 1f);

#if UNITY_EDITOR
    void OnValidate()
    {
        _count = Mathf.Max(1, _count);
        if (_spriteRenderer == null)
            TryGetComponent(out _spriteRenderer);

        ApplyIcon();
    }
#endif
}
