// ============================================================
// GearContextContributor — 인벤 우클릭 착용/들기 메뉴
// ============================================================

using System.Collections.Generic;

public sealed class GearContextContributor : IContextMenuContributor
{
    public void Contribute(
        ItemStack stack,
        InventoryContainer container,
        InventorySession session,
        List<ContextMenuEntry> roots)
    {
        if (stack?.Item == null || roots == null)
            return;

        CharacterGearService gear = PlayerGearHost.Active?.Service;
        if (gear == null)
            return;

        if (GearHandleRules.IsWearable(stack.Item))
        {
            roots.Add(ContextMenuEntry.Leaf(
                "gear:wear",
                CharacterGearLabels.Wear,
                new GearWearContextAction(stack, container)));
        }

        var wieldChildren = new List<ContextMenuEntry>(3)
        {
            ContextMenuEntry.Leaf(
                "gear:wield-l",
                CharacterGearLabels.WieldLeft,
                new GearWieldContextAction(stack, container, WieldHand.Left)),
            ContextMenuEntry.Leaf(
                "gear:wield-r",
                CharacterGearLabels.WieldRight,
                new GearWieldContextAction(stack, container, WieldHand.Right)),
            ContextMenuEntry.Leaf(
                "gear:wield-2h",
                CharacterGearLabels.WieldTwoHand,
                new GearWieldContextAction(stack, container, WieldHand.TwoHand)),
        };
        roots.Add(ContextMenuEntry.Group(
            "gear:wield",
            CharacterGearLabels.WieldGroup,
            wieldChildren));
    }
}
