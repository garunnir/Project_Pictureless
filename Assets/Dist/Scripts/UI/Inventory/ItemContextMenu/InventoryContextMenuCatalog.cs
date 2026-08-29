// ============================================================
// InventoryContextMenuCatalog — 인벤 우클릭 Contributor 등록 SSOT
// ============================================================

using System.Collections.Generic;

public static class InventoryContextMenuCatalog
{
    static readonly IContextMenuContributor[] Contributors =
    {
        new ConsumeContextContributor(),
        new PlantContextContributor(),
        new FishRodContextContributor(),
        new FishTrapContextContributor(),
        new DiveTankContextContributor(),
        new TillContextContributor(),
        new FertilizeContextContributor(),
        new GearContextContributor(),
        new AmmoContextContributor(),
        new CraftContextContributor(),
        new CookItemContextContributor(),
        new SmokeItemContextContributor(),
        new MulticookerContextContributor(),
        new UncraftContextContributor(),
    };

    public static IReadOnlyList<IContextMenuContributor> All => Contributors;
}
