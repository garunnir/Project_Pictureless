// ============================================================
// ConsumeContextContributor — 인벤 우클릭 먹기/마시기/사용
// ============================================================

using System.Collections.Generic;

public sealed class ConsumeContextContributor : IContextMenuContributor
{
    public void Contribute(
        ItemStack stack,
        InventoryContainer container,
        InventorySession session,
        List<ContextMenuEntry> roots)
    {
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
                roots.Add(ContextMenuEntry.Leaf(
                    ConsumeMenuIds.Use,
                    ItemContextMenuLabels.Use,
                    new ConsumeContextAction(stack, container)));
                break;
        }
    }
}
