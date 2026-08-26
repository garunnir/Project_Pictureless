// ============================================================
// CookItemContextContributor — cooks_like simple cook action
// ============================================================

using System.Collections.Generic;
using Garunnir.Runtime.Gameplay.Data;

public sealed class CookItemContextContributor : IContextMenuContributor
{
    public void Contribute(
        ItemStack stack,
        InventoryContainer container,
        InventorySession session,
        List<ContextMenuEntry> roots)
    {
        if (stack?.Item?.comestible == null || roots == null)
            return;

        string resultId = stack.Item.comestible.cooks_like;
        if (string.IsNullOrEmpty(resultId) || GameplayData.GetItem(resultId) == null)
            return;

        roots.Add(ContextMenuEntry.Leaf(
            "cook-like",
            ItemContextMenuLabels.Cook,
            new CookItemContextAction(stack, container, session, resultId, smoke: false)));
    }
}
