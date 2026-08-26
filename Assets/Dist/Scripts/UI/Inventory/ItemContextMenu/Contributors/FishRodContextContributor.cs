// ============================================================
// FishRodContextContributor — 인벤 낚싯대(FISHING) Cast
// ============================================================

using System.Collections.Generic;
using Garunnir.Runtime.Gameplay.Data;
using IsoTilemap;

public sealed class FishRodContextContributor : IContextMenuContributor
{
    public void Contribute(
        ItemStack stack,
        InventoryContainer container,
        InventorySession session,
        List<ContextMenuEntry> roots)
    {
        if (MoodGameplayGate.IsBlocked)
            return;
        if (!MapFishService.HasFishingQuality(stack?.Item) || roots == null)
            return;

        roots.Add(ContextMenuEntry.Leaf(
            "fish-cast",
            ItemContextMenuLabels.Fish,
            new FishRodContextAction(stack, container)));
    }
}
