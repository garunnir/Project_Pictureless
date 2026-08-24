// ============================================================
// WieldSlotConsumeContributor — 들기 슬롯 RMB Eat/Drink/Use(heal 부위)
// ============================================================

using System.Collections.Generic;
using Garunnir.Runtime.Gameplay.Data;

public sealed class WieldSlotConsumeContributor : IWieldSlotContextMenuContributor
{
    public void Contribute(WieldSlotContextRequest request, List<ContextMenuEntry> roots)
    {
        if (request?.Gear == null || roots == null)
            return;
        if (MoodGameplayGate.IsBlocked)
            return;

        ItemStack stack = request.Gear.Wield?.Get(request.Slot);
        if (stack?.Item == null || stack.Count < 1)
            return;

        ConsumeKind? kind = ConsumeService.Classify(stack.Item);
        if (kind == null)
            return;

        switch (kind.Value)
        {
            case ConsumeKind.Eat:
                if (!ConsumeService.CanConsume(stack, container: null))
                    return;
                roots.Insert(0, ContextMenuEntry.Leaf(
                    ConsumeMenuIds.Eat,
                    ItemContextMenuLabels.Eat,
                    new ConsumeContextAction(stack, container: null)));
                break;
            case ConsumeKind.Drink:
                if (!ConsumeService.CanConsume(stack, container: null))
                    return;
                roots.Insert(0, ContextMenuEntry.Leaf(
                    ConsumeMenuIds.Drink,
                    ItemContextMenuLabels.Drink,
                    new ConsumeContextAction(stack, container: null)));
                break;
            case ConsumeKind.Use:
                ContributeUse(stack, roots);
                break;
        }
    }

    static void ContributeUse(ItemStack stack, List<ContextMenuEntry> roots)
    {
        if (ConsumeService.IsHealItem(stack.Item))
        {
            var leaves = new List<ContextMenuEntry>();
            HealConsumeContextMenuEntries.AppendPartLeavesFromItem(stack, container: null, leaves);
            if (leaves.Count == 0)
                return;

            roots.Insert(0, ContextMenuEntry.Group(
                ConsumeMenuIds.Use,
                ItemContextMenuLabels.Use,
                leaves));
            return;
        }

        if (!ConsumeService.CanConsume(stack, container: null))
            return;

        roots.Insert(0, ContextMenuEntry.Leaf(
            ConsumeMenuIds.Use,
            ItemContextMenuLabels.Use,
            new ConsumeContextAction(stack, container: null)));
    }
}
