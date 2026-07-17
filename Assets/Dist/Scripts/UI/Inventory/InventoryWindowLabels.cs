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
    const string KeyStackDetail = "Inventory.StackDetail";
    const string KeyStackCount = "Inventory.StackCount";
    const string KeyItemCategoryPrefix = "ItemCategory.";

    public static string PrimaryTitle => Loc.Get(KeyPrimaryTitle);
    public static string LootTitle => Loc.Get(KeyLootTitle);
    public static string EmptyWeight => Loc.Get(KeyEmptyWeight);
    public static string EmptyVolume => Loc.Get(KeyEmptyVolume);

    public static string FormatWeightCapacity(float used, float max) =>
        Loc.Format(KeyWeightCapacity, used, max);

    public static string FormatVolumeCapacity(float used, float max) =>
        Loc.Format(KeyVolumeCapacity, used, max);

    public static string FormatStackDetail(int count, float weight, float volume) =>
        Loc.Format(KeyStackDetail, count, weight, volume);

    public static string FormatStackCount(int count) =>
        Loc.Format(KeyStackCount, count);

    public static string GetItemCategory(string categoryId)
    {
        if (string.IsNullOrEmpty(categoryId))
            return string.Empty;

        return Loc.TryGet(KeyItemCategoryPrefix + categoryId, out string localizedCategory)
            ? localizedCategory
            : categoryId;
    }
}
