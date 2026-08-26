// ============================================================
// FishTrapContextContributor — 인벤 fish_trap DeployTrap
// ============================================================

using System.Collections.Generic;
using IsoTilemap;

public sealed class FishTrapContextContributor : IContextMenuContributor
{
    public void Contribute(
        ItemStack stack,
        InventoryContainer container,
        InventorySession session,
        List<ContextMenuEntry> roots)
    {
        if (MoodGameplayGate.IsBlocked || roots == null)
            return;
        if (!MapFishService.IsFishTrapItem(stack?.Item))
            return;

        roots.Add(ContextMenuEntry.Leaf(
            "fish-trap-deploy",
            FishTrapContextLabels.Deploy,
            new FishTrapContextAction(stack, container)));
    }
}
