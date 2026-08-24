// ============================================================
// FertilizeContextContributor — 인벤 비료 아이템으로 발밑 식물 시비
// ============================================================

using System.Collections.Generic;

public sealed class FertilizeContextContributor : IContextMenuContributor
{
    public void Contribute(
        ItemStack stack,
        InventoryContainer container,
        InventorySession session,
        List<ContextMenuEntry> roots)
    {
        if (MoodGameplayGate.IsBlocked)
            return;
        if (!MapPlantService.IsFertilizerItem(stack?.Item) || roots == null)
            return;

        roots.Add(ContextMenuEntry.Leaf(
            "fertilize-plant",
            ItemContextMenuLabels.Fertilize,
            new FertilizeContextAction(stack, container)));
    }
}
