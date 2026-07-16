// ============================================================
// InventoryWindowLabels — 인벤 창 헤더·용량 표시 문구 SSOT
// ============================================================

public static class InventoryWindowLabels
{
    const string KeyPrimaryTitle = "Inventory.PrimaryTitle";
    const string KeyLootTitle = "Inventory.LootTitle";
    const string KeyEmptyWeight = "Inventory.EmptyWeight";
    const string KeyEmptyVolume = "Inventory.EmptyVolume";

    public static string PrimaryTitle => Loc.Get(KeyPrimaryTitle, "Inventory");
    public static string LootTitle => Loc.Get(KeyLootTitle, "Loot");
    public static string EmptyWeight => Loc.Get(KeyEmptyWeight, "— kg");
    public static string EmptyVolume => Loc.Get(KeyEmptyVolume, "— L");
}
