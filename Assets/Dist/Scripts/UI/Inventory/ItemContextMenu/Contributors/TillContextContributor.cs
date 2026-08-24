// ============================================================
// TillContextContributor — 인벤 DIG 품질 도구로 발밑 경작
// ============================================================

using System.Collections.Generic;

public sealed class TillContextContributor : IContextMenuContributor
{
    public void Contribute(
        ItemStack stack,
        InventoryContainer container,
        InventorySession session,
        List<ContextMenuEntry> roots)
    {
        if (MoodGameplayGate.IsBlocked)
            return;
        if (!MapPlantService.HasDigQuality(stack?.Item) || roots == null)
            return;

        roots.Add(ContextMenuEntry.Leaf(
            "till-cell",
            ItemContextMenuLabels.Till,
            new TillContextAction(stack, container)));
    }
}
