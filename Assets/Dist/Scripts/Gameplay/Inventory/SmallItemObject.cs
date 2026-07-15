// ============================================================
// SmallItemObject — ItemData의 월드 인스턴스 (1 GO = 1 ItemStack)
// ============================================================

using Garunnir.Runtime.Gameplay.Data;
using IsoTilemap;
using Sirenix.OdinInspector;
using UnityEngine;

public sealed class SmallItemObject : MonoBehaviour
{
    [SerializeField] string _itemId;
    [SerializeField, Min(1)] int _count = 1;
    [SerializeField] SpriteRenderer _spriteRenderer;

    IWorldGrid _worldGrid;
    ItemStack _stack;
    Vector3Int _ownerCell;
    bool _ownerCellResolved;

    public ItemStack Stack => _stack;
    public Vector3Int OwnerCell => _ownerCell;
    public string ItemId => _itemId;

    public void Configure(ItemData definition, int count)
    {
        _itemId = definition?.id;
        _count = Mathf.Max(1, count);
        _stack = definition != null ? new ItemStack(definition, _count) : null;
        ApplyIcon();
    }

    public void Configure(string itemId, int count)
    {
        _itemId = itemId;
        _count = Mathf.Max(1, count);

        ItemData item = ResolveItem(itemId);
        _stack = item != null ? new ItemStack(item, _count) : null;
        ApplyIcon();
    }

    public void BindStack(ItemStack stack)
    {
        if (stack?.Item == null)
            return;

        _itemId = stack.ItemId;
        _count = stack.Count;
        _stack = stack;
        ApplyIcon();
    }

    public void BindWorldGrid(IWorldGrid worldGrid)
    {
        if (_worldGrid == worldGrid)
            return;

        bool shouldRegister = isActiveAndEnabled && !string.IsNullOrEmpty(_itemId) && _stack != null;
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
        if (string.IsNullOrEmpty(_itemId))
        {
            Debug.LogWarning("[SmallItemObject] ItemId is not assigned.", this);
            return;
        }

        if (_stack == null)
        {
            ItemData item = ResolveItem(_itemId);
            if (item != null)
                Configure(item, _count);
        }
    }

    void OnEnable()
    {
        if (string.IsNullOrEmpty(_itemId) || _stack == null)
            return;

        ResolveOwnerCell();
        SmallItemRegistry.Register(this);
    }

    void OnDisable() => SmallItemRegistry.Unregister(this);

    void ApplyIcon()
    {
        if (_spriteRenderer == null || string.IsNullOrEmpty(_itemId))
            return;

        _spriteRenderer.sprite = ItemVisualPresenter.GetDisplayIcon(_itemId);
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

    static ItemData ResolveItem(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return null;
        return GameplayData.GetItem(itemId);
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        _count = Mathf.Max(1, _count);
        if (_spriteRenderer == null)
            TryGetComponent(out _spriteRenderer);
    }
#endif
}
