// ============================================================
// WieldSlotContextMenuCatalog — 들기 슬롯 RMB Contributor 등록 SSOT
// ============================================================

using System.Collections.Generic;

public static class WieldSlotContextMenuCatalog
{
    static readonly IWieldSlotContextMenuContributor[] Contributors =
    {
        new WieldSlotActionsContributor(),
    };

    public static IReadOnlyList<IWieldSlotContextMenuContributor> All => Contributors;
}
