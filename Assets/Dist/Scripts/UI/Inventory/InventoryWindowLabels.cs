// ============================================================
// InventoryWindowLabels — 인벤 창 헤더·용량·스택 표시 문구 SSOT
// ============================================================

public static class InventoryWindowLabels
{
    const string KeyPrimaryTitle = "Inventory.PrimaryTitle";
    const string KeyLootTitle = "Inventory.LootTitle";
    const string KeyEmptyWeight = "Inventory.EmptyWeight";
    const string KeyEmptyVolume = "Inventory.EmptyVolume";
    const string KeyWeightCapacity = "Inventory.WeightCapacity";
    const string KeyVolumeCapacity = "Inventory.VolumeCapacity";
    const string KeyStackCount = "Inventory.StackCount";
    const string KeyStackWeightValue = "Inventory.StackWeightValue";
    const string KeyStackWeightUnit = "Inventory.StackWeightUnit";
    const string KeyStackVolumeValue = "Inventory.StackVolumeValue";
    const string KeyStackVolumeUnit = "Inventory.StackVolumeUnit";
    const string KeyColumnCategory = "Inventory.Column.Category";
    const string KeyColumnName = "Inventory.Column.Name";
    const string KeyColumnCount = "Inventory.Column.Count";
    const string KeyColumnWeight = "Inventory.Column.Weight";
    const string KeyColumnVolume = "Inventory.Column.Volume";
    const string KeyItemCategoryPrefix = "ItemCategory.";

    public static string PrimaryTitle => Loc.Get(KeyPrimaryTitle);
    public static string LootTitle => Loc.Get(KeyLootTitle);
    public static string EmptyWeight => Loc.Get(KeyEmptyWeight);
    public static string EmptyVolume => Loc.Get(KeyEmptyVolume);
    public static string StackWeightUnit => Loc.Get(KeyStackWeightUnit);
    public static string StackVolumeUnit => Loc.Get(KeyStackVolumeUnit);
    public static string ColumnCategory => Loc.Get(KeyColumnCategory);
    public static string ColumnName => Loc.Get(KeyColumnName);
    public static string ColumnCount => Loc.Get(KeyColumnCount);
    public static string ColumnWeight => Loc.Get(KeyColumnWeight);
    public static string ColumnVolume => Loc.Get(KeyColumnVolume);

    public static string FormatWeightCapacity(float used, float max) =>
        Loc.Format(KeyWeightCapacity, used, max);

    public static string FormatVolumeCapacity(float used, float max) =>
        Loc.Format(KeyVolumeCapacity, used, max);

    public static string FormatStackCount(int count) =>
        Loc.Format(KeyStackCount, count);

    public static string FormatStackWeightValue(float weight) =>
        Loc.Format(KeyStackWeightValue, weight);

    public static string FormatStackVolumeValue(float volume) =>
        Loc.Format(KeyStackVolumeValue, volume);

    public static string GetItemCategory(string categoryId)
    {
        if (string.IsNullOrEmpty(categoryId))
            return string.Empty;

        return Loc.TryGet(KeyItemCategoryPrefix + categoryId, out string localizedCategory)
            ? localizedCategory
            : categoryId;
    }
}
