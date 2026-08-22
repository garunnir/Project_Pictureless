// ============================================================
// InventoryListColumnLayout — Settings 에셋 파사드 (bake·런타임 읽기 SSOT)
// ============================================================

using UnityEngine;
using UnityEngine.UI;

public static class InventoryListColumnLayout
{
    public static float IconSize => ResolveSettings().IconSize;
    public static float CategoryWidth => ResolveSettings().CategoryWidth;
    public static float CountWidth => ResolveSettings().CountWidth;
    public static float WeightValueWidth => ResolveSettings().WeightValueWidth;
    public static float WeightUnitWidth => ResolveSettings().WeightUnitWidth;
    public static float VolumeValueWidth => ResolveSettings().VolumeValueWidth;
    public static float VolumeUnitWidth => ResolveSettings().VolumeUnitWidth;
    public static float NameMinWidth => ResolveSettings().NameMinWidth;
    public static int ColumnCount => InventoryListColumnLayoutSettings.ColumnCount;
    public static float MinRowWidth => ResolveSettings().MinRowWidth;
    public static int RowPaddingH => ResolveSettings().RowPaddingH;
    public static int RowPaddingV => ResolveSettings().RowPaddingV;
    public static float RowSpacing => ResolveSettings().Spacing;
    public static int ContentPadding => ResolveSettings().ContentPadding;
    public static int ListInsetHorizontal => ResolveSettings().ListInsetHorizontal;
    public static float RowHeight => ResolveSettings().RowHeight;
    public static float ColumnHeaderHeight => ResolveSettings().ColumnHeaderHeight;
    public static float FontCategory => ResolveSettings().FontCategory;
    public static float FontName => ResolveSettings().FontName;
    public static float FontDetail => ResolveSettings().FontDetail;
    public static float FontHeader => ResolveSettings().FontHeader;

    public static int ContentPaddingTopWithStickyHeader =>
        ResolveSettings().ContentPaddingTopWithStickyHeader;

    public static float MeasureMinRowWidth(UIItemListRow row)
    {
        if (row == null)
            return MinRowWidth;

        RectTransform root = row.RectTransform;
        if (root == null || !row.TryGetComponent(out HorizontalLayoutGroup layout))
            return MinRowWidth;

        float width = layout.padding.left + layout.padding.right;
        int counted = 0;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (!child.TryGetComponent(out LayoutElement element) || element.ignoreLayout)
                continue;

            float min = element.minWidth > 0f ? element.minWidth : 0f;
            if (counted > 0)
                width += layout.spacing;
            width += min;
            counted++;
        }

        return counted > 0 ? width : MinRowWidth;
    }

    static InventoryListColumnLayoutSettings ResolveSettings()
    {
        InventoryListColumnLayoutSettings settings = InventoryListColumnLayoutSettings.ResolveDefault();
        return settings != null
            ? settings
            : throw new System.InvalidOperationException(
                "InventoryListColumnLayoutSettings missing. " +
                "Run Dist/MCP/Inventory/Sync List Column Layout or add " +
                "Resources/Inventory/InventoryListColumnLayoutSettings.asset.");
    }
}
