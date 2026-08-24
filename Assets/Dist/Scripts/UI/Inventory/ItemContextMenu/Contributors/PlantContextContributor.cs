// ============================================================
// PlantContextContributor — 인벤 씨앗 심기
// ============================================================

using System.Collections.Generic;

public sealed class PlantContextContributor : IContextMenuContributor
{
    public void Contribute(
        ItemStack stack,
        InventoryContainer container,
        InventorySession session,
        List<ContextMenuEntry> roots)
    {
        if (MoodGameplayGate.IsBlocked)
            return;
        if (stack?.Item?.seed == null || roots == null)
            return;

        roots.Add(ContextMenuEntry.Leaf(
            "plant-seed",
            ItemContextMenuLabels.Plant,
            new PlantContextAction(stack, container)));
    }
}
