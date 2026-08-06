// ============================================================
// InventoryContextMenuCatalog — 인벤 우클릭 Contributor 등록 SSOT
// ============================================================

using System.Collections.Generic;

public static class InventoryContextMenuCatalog
{
    static readonly IContextMenuContributor[] Contributors =
    {
        new GearContextContributor(),
        new CraftContextContributor(),
        new UncraftContextContributor(),
    };

    public static IReadOnlyList<IContextMenuContributor> All => Contributors;
}
