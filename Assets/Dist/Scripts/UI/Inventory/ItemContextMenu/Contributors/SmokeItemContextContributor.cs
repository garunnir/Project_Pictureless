// ============================================================
// SmokeItemContextContributor — smoking_result simple smoke action
// ============================================================

using System.Collections.Generic;
using Garunnir.Runtime.Gameplay.Data;

public sealed class SmokeItemContextContributor : IContextMenuContributor
{
    public void Contribute(
        ItemStack stack,
        InventoryContainer container,
        InventorySession session,
        List<ContextMenuEntry> roots)
    {
        if (stack?.Item?.comestible == null || roots == null)
            return;

        string resultId = stack.Item.comestible.smoking_result;
        if (string.IsNullOrEmpty(resultId) || GameplayData.GetItem(resultId) == null)
            return;

        roots.Add(ContextMenuEntry.Leaf(
            "smoke-item",
            ItemContextMenuLabels.Smoke,
            new CookItemContextAction(stack, container, session, resultId, smoke: true)));
    }
}
