// ============================================================
// InventoryDemoSeeder — 씬 테스트용 샘플 스택 주입
// ============================================================

using UnityEngine;

public static class InventoryDemoSeeder
{
    public static void SeedIfEmpty(InventoryContainer container)
    {
        if (container == null || container.Stacks.Count > 0)
            return;

        var db = GameplayData.GameItems;
        if (db == null)
            return;

        var items = db.Items;
        if (items.Count < 2)
            return;

        container.AddItem(items[0], 1);
        container.AddItem(items[1], 1);
    }
}
