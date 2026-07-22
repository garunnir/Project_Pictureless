#if UNITY_EDITOR
using UnityEngine;

// Single inventory UI chrome SSOT for bake (Primary + Loot windows share this layout).
// Row column widths come from InventoryListColumnLayout (runtime SSOT).
readonly struct InventoryUIPrefabStyleSpec
{
    public readonly Vector2 WindowSize;
    public readonly float HeaderHeight;
    public readonly float HeaderFontSize;
    public readonly float SidebarWidth;
    public readonly float ChromeMargin;
    public readonly float RowHeight;
    public readonly float RowIconSize;
    public readonly float RowCategoryWidth;
    public readonly float RowCountWidth;
    public readonly float RowWeightValueWidth;
    public readonly float RowWeightUnitWidth;
    public readonly float RowVolumeValueWidth;
    public readonly float RowVolumeUnitWidth;
    public readonly float RowFontCategory;
    public readonly float RowFontName;
    public readonly float RowFontDetail;
    public readonly int RowPaddingH;
    public readonly int RowPaddingV;
    public readonly float RowSpacing;
    public readonly float SlotHeight;
    public readonly float SlotFontSize;
    public readonly int SlotLabelInset;
    public readonly float ContentSpacing;
    public readonly int ContentPadding;
    public readonly float SidebarSlotSpacing;
    public readonly int SidebarSlotPadding;
    public readonly float EdgeThickness;
    public readonly float CornerSize;
    public readonly float ScrollbarWidth;
    public readonly Color ScrollbarTrackColor;
    public readonly Color ScrollbarHandleColor;

    public InventoryUIPrefabStyleSpec(
        Vector2 windowSize,
        float headerHeight,
        float headerFontSize,
        float sidebarWidth,
        float chromeMargin,
        float rowHeight,
        float rowIconSize,
        float rowCategoryWidth,
        float rowCountWidth,
        float rowWeightValueWidth,
        float rowWeightUnitWidth,
        float rowVolumeValueWidth,
        float rowVolumeUnitWidth,
        float rowFontCategory,
        float rowFontName,
        float rowFontDetail,
        int rowPaddingH,
        int rowPaddingV,
        float rowSpacing,
        float slotHeight,
        float slotFontSize,
        int slotLabelInset,
        float contentSpacing,
        int contentPadding,
        float sidebarSlotSpacing,
        int sidebarSlotPadding,
        float edgeThickness,
        float cornerSize,
        float scrollbarWidth,
        Color scrollbarTrackColor,
        Color scrollbarHandleColor)
    {
        WindowSize = windowSize;
        HeaderHeight = headerHeight;
        HeaderFontSize = headerFontSize;
        SidebarWidth = sidebarWidth;
        ChromeMargin = chromeMargin;
        RowHeight = rowHeight;
        RowIconSize = rowIconSize;
        RowCategoryWidth = rowCategoryWidth;
        RowCountWidth = rowCountWidth;
        RowWeightValueWidth = rowWeightValueWidth;
        RowWeightUnitWidth = rowWeightUnitWidth;
        RowVolumeValueWidth = rowVolumeValueWidth;
        RowVolumeUnitWidth = rowVolumeUnitWidth;
        RowFontCategory = rowFontCategory;
        RowFontName = rowFontName;
        RowFontDetail = rowFontDetail;
        RowPaddingH = rowPaddingH;
        RowPaddingV = rowPaddingV;
        RowSpacing = rowSpacing;
        SlotHeight = slotHeight;
        SlotFontSize = slotFontSize;
        SlotLabelInset = slotLabelInset;
        ContentSpacing = contentSpacing;
        ContentPadding = contentPadding;
        SidebarSlotSpacing = sidebarSlotSpacing;
        SidebarSlotPadding = sidebarSlotPadding;
        EdgeThickness = edgeThickness;
        CornerSize = cornerSize;
        ScrollbarWidth = scrollbarWidth;
        ScrollbarTrackColor = scrollbarTrackColor;
        ScrollbarHandleColor = scrollbarHandleColor;
    }

    public static InventoryUIPrefabStyleSpec Default => new(
        windowSize: InventoryWindowLayout.DefaultPrimaryWindowSize,
        headerHeight: InventoryWindowLayout.HeaderHeight,
        headerFontSize: 14f,
        sidebarWidth: 120f,
        chromeMargin: 10f,
        rowHeight: InventoryListColumnLayout.RowHeight,
        rowIconSize: InventoryListColumnLayout.IconSize,
        rowCategoryWidth: InventoryListColumnLayout.CategoryWidth,
        rowCountWidth: InventoryListColumnLayout.CountWidth,
        rowWeightValueWidth: InventoryListColumnLayout.WeightValueWidth,
        rowWeightUnitWidth: InventoryListColumnLayout.WeightUnitWidth,
        rowVolumeValueWidth: InventoryListColumnLayout.VolumeValueWidth,
        rowVolumeUnitWidth: InventoryListColumnLayout.VolumeUnitWidth,
        rowFontCategory: InventoryListColumnLayout.FontCategory,
        rowFontName: InventoryListColumnLayout.FontName,
        rowFontDetail: InventoryListColumnLayout.FontDetail,
        rowPaddingH: InventoryListColumnLayout.RowPaddingH,
        rowPaddingV: InventoryListColumnLayout.RowPaddingV,
        rowSpacing: InventoryListColumnLayout.RowSpacing,
        slotHeight: 48f,
        slotFontSize: 14f,
        slotLabelInset: 6,
        contentSpacing: InventoryListColumnLayout.RowSpacing,
        contentPadding: InventoryListColumnLayout.ContentPadding,
        sidebarSlotSpacing: 6f,
        sidebarSlotPadding: 4,
        edgeThickness: 6f,
        cornerSize: 10f,
        scrollbarWidth: 8f,
        scrollbarTrackColor: new Color(0.12f, 0.12f, 0.12f, 0.85f),
        scrollbarHandleColor: new Color(0.42f, 0.42f, 0.42f, 0.95f));
}
#endif
