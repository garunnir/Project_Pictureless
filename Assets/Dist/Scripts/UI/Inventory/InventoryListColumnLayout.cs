// ============================================================
// InventoryListColumnLayout — Settings 에셋 파사드 (bake·런타임 읽기 SSOT)
// ============================================================

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

    static InventoryListColumnLayoutSettings ResolveSettings()
    {
        InventoryListColumnLayoutSettings settings = InventoryListColumnLayoutSettings.ResolveDefault();
        return settings != null
            ? settings
            : throw new System.InvalidOperationException(
                "InventoryListColumnLayoutSettings missing. " +
                "Run Dist/Inventory/Sync List Column Layout or add " +
                "Resources/Inventory/InventoryListColumnLayoutSettings.asset.");
    }
}
