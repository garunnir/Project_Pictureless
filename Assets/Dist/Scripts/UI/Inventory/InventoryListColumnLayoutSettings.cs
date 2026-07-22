// ============================================================
// InventoryListColumnLayoutSettings — 리스트 열·행 HLG SSOT (에셋, Inspector 노출)
// ============================================================

using UnityEngine;

[CreateAssetMenu(
    fileName = "InventoryListColumnLayoutSettings",
    menuName = "Dist/Inventory/List Column Layout Settings")]
public sealed class InventoryListColumnLayoutSettings : ScriptableObject
{
    const string DefaultResourcePath = "Inventory/InventoryListColumnLayoutSettings";

    static InventoryListColumnLayoutSettings _cachedDefault;

    [Header("Columns (px)")]
    [SerializeField] float _iconSize = 32f;
    [SerializeField] float _categoryWidth = 100f;
    [SerializeField] float _countWidth = 28f;
    [SerializeField] float _weightValueWidth = 40f;
    [SerializeField] float _weightUnitWidth = 22f;
    [SerializeField] float _volumeValueWidth = 40f;
    [SerializeField] float _volumeUnitWidth = 18f;

    [Header("Name (flex)")]
    [SerializeField] float _nameMinWidth = 48f;

    [Header("Row HLG")]
    [SerializeField] int _rowPaddingH = 8;
    [SerializeField] int _rowPaddingV = 2;
    [SerializeField] float _spacing = 4f;

    [Header("Content (Viewport/Content VLG)")]
    [SerializeField] int _contentPadding = 4;

    [Header("Heights")]
    [SerializeField] float _rowHeight = 36f;
    [SerializeField] float _columnHeaderHeight = 28f;

    [Header("Fonts")]
    [SerializeField] float _fontCategory = 14f;
    [SerializeField] float _fontName = 16f;
    [SerializeField] float _fontDetail = 14f;
    [SerializeField] float _fontHeader = 13f;

    public float IconSize => _iconSize;
    public float CategoryWidth => _categoryWidth;
    public float CountWidth => _countWidth;
    public float WeightValueWidth => _weightValueWidth;
    public float WeightUnitWidth => _weightUnitWidth;
    public float VolumeValueWidth => _volumeValueWidth;
    public float VolumeUnitWidth => _volumeUnitWidth;
    public float NameMinWidth => _nameMinWidth;
    public int RowPaddingH => _rowPaddingH;
    public int RowPaddingV => _rowPaddingV;
    public float Spacing => _spacing;
    public int ContentPadding => _contentPadding;
    public int ListInsetHorizontal => _rowPaddingH + _contentPadding;
    public float RowHeight => _rowHeight;
    public float ColumnHeaderHeight => _columnHeaderHeight;
    public float FontCategory => _fontCategory;
    public float FontName => _fontName;
    public float FontDetail => _fontDetail;
    public float FontHeader => _fontHeader;

    public int ContentPaddingTopWithStickyHeader =>
        _contentPadding + (int)_columnHeaderHeight;

    public static InventoryListColumnLayoutSettings ResolveDefault()
    {
        if (_cachedDefault != null)
            return _cachedDefault;

        _cachedDefault = Resources.Load<InventoryListColumnLayoutSettings>(DefaultResourcePath);
#if UNITY_EDITOR
        if (_cachedDefault == null)
        {
            _cachedDefault = UnityEditor.AssetDatabase.LoadAssetAtPath<InventoryListColumnLayoutSettings>(
                "Assets/Dist/Resources/Inventory/InventoryListColumnLayoutSettings.asset");
        }
#endif
        return _cachedDefault;
    }

#if UNITY_EDITOR
    public static void SetCachedDefault(InventoryListColumnLayoutSettings settings) =>
        _cachedDefault = settings;
#endif
}
