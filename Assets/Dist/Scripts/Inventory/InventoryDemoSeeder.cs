// ============================================================

// InventoryDemoSeeder — 씬 테스트용 샘플 스택 주입

// ============================================================



using Garunnir.Runtime.Gameplay.Item;

using UnityEngine;



public static class InventoryDemoSeeder

{

    public static void SeedIfEmpty(InventoryContainer container)

    {

        if (container == null || container.Stacks.Count > 0)

            return;



        ItemCatalogSO catalog = GameplayData.ItemCatalog;

        if (catalog == null)

            return;



        ItemDefinitionSO weapon = catalog.GetByIndex(0);

        ItemDefinitionSO clothing = catalog.GetByIndex(1);



        if (weapon != null)

            container.MutableStacks.Add(new ItemStack(weapon, 1));

        if (clothing != null)

            container.MutableStacks.Add(new ItemStack(clothing, 1));

    }

}


