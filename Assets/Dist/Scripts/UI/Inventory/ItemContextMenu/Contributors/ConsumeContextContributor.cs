// ============================================================
// ConsumeContextContributor — 인벤 우클릭 먹기/마시기/사용
// ============================================================

using System.Collections.Generic;
using Garunnir.Runtime.Gameplay.Data;

public sealed class ConsumeContextContributor : IContextMenuContributor
{
    public void Contribute(
        ItemStack stack,
        InventoryContainer container,
        InventorySession session,
        List<ContextMenuEntry> roots)
    {
        if (MoodGameplayGate.IsBlocked)
            return;
        if (stack?.Item == null || roots == null)
            return;

        ConsumeKind? kind = ConsumeService.Classify(stack.Item);
        if (kind == null)
            return;

        switch (kind.Value)
        {
            case ConsumeKind.Eat:
                roots.Add(ContextMenuEntry.Leaf(
                    ConsumeMenuIds.Eat,
                    ItemContextMenuLabels.Eat,
                    new ConsumeContextAction(stack, container)));
                break;
            case ConsumeKind.Drink:
                roots.Add(ContextMenuEntry.Leaf(
                    ConsumeMenuIds.Drink,
                    ItemContextMenuLabels.Drink,
                    new ConsumeContextAction(stack, container)));
                break;
            case ConsumeKind.Use:
                ContributeUse(stack, container, roots);
                break;
        }
    }

    static void ContributeUse(ItemStack stack, InventoryContainer container, List<ContextMenuEntry> roots)
    {
        if (ConsumeService.IsHealItem(stack.Item))
        {
            var leaves = new List<ContextMenuEntry>();
            HealConsumeContextMenuEntries.AppendPartLeavesFromItem(stack, container, leaves);
            if (leaves.Count == 0)
                return;

            roots.Add(ContextMenuEntry.Group(ConsumeMenuIds.Use, ItemContextMenuLabels.Use, leaves));
            return;
        }

        roots.Add(ContextMenuEntry.Leaf(
            ConsumeMenuIds.Use,
            ItemContextMenuLabels.Use,
            new ConsumeContextAction(stack, container)));
    }
}
