// ============================================================
// MulticookerContextContributor — open crafting filtered to multi_cooker
// ============================================================

using System.Collections.Generic;
using Garunnir.Runtime.Gameplay.Data;

public sealed class MulticookerContextContributor : IContextMenuContributor
{
    public void Contribute(
        ItemStack stack,
        InventoryContainer container,
        InventorySession session,
        List<ContextMenuEntry> roots)
    {
        if (stack?.Item?.use_action == null || roots == null)
            return;

        string type = stack.Item.use_action.type;
        if (string.IsNullOrEmpty(type) ||
            !type.Equals(CraftingPseudoIds.UseActionMulticooker, System.StringComparison.OrdinalIgnoreCase))
            return;

        roots.Add(ContextMenuEntry.Leaf(
            "multicooker",
            ItemContextMenuLabels.Multicooker,
            new MulticookerContextAction()));
    }
}
