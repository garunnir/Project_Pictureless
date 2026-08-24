// ============================================================
// TileObjectContextMenuCatalog — 타일 오브젝트 우클릭 Contributor 등록 SSOT
// ============================================================

using System.Collections.Generic;

public static class TileObjectContextMenuCatalog
{
    static readonly ITileObjectContextMenuContributor[] Contributors =
    {
        new DoorToggleContextContributor(),
        new OpenLootContextContributor(),
        new HarvestContextContributor(),
        new TillContextContributor(),
        new FertilizeContextContributor(),
    };

    public static IReadOnlyList<ITileObjectContextMenuContributor> All => Contributors;
}
