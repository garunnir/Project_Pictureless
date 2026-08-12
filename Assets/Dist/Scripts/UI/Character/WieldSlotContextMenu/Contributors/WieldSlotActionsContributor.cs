// ============================================================
// WieldSlotActionsContributor — HandAction 그룹 + Unwield/Floor
// ============================================================

using System.Collections.Generic;

public sealed class WieldSlotActionsContributor : IWieldSlotContextMenuContributor
{
    public void Contribute(WieldSlotContextRequest request, List<ContextMenuEntry> roots)
    {
        if (request?.Gear == null || roots == null)
            return;

        ItemStack stack = request.Gear.Wield?.Get(request.Slot);
        if (stack?.Item != null)
        {
            WeaponPresentation presentation = WeaponActionRows.Resolve(
                request.Gear.PresentationCatalog,
                stack);
            WeaponActionMask mask = WeaponActionRows.Available(presentation);
            var actionChildren = new List<ContextMenuEntry>(WeaponActionUtil.All.Length + 1);
            TryAddAction(actionChildren, request, mask, WeaponAction.Swing, "hand-swing", CharacterGearLabels.ActionSwing);
            TryAddAction(actionChildren, request, mask, WeaponAction.Thrust, "hand-thrust", CharacterGearLabels.ActionThrust);
            TryAddAction(actionChildren, request, mask, WeaponAction.Trigger, "hand-trigger", CharacterGearLabels.ActionTrigger);
            TryAddAction(actionChildren, request, mask, WeaponAction.Raise, "hand-raise", CharacterGearLabels.ActionRaise);
            actionChildren.Add(ContextMenuEntry.Leaf(
                "hand-none",
                CharacterGearLabels.ActionNone,
                new SetHandActionContextAction(request, null)));

            roots.Add(ContextMenuEntry.Group(
                "hand-action",
                CharacterGearLabels.HandActionGroup,
                actionChildren));
        }

        roots.Add(ContextMenuEntry.Leaf(
            "unwield",
            CharacterGearLabels.Unwield,
            new UnwieldSlotContextAction(request, toFloor: false)));
        roots.Add(ContextMenuEntry.Leaf(
            "drop-floor",
            CharacterGearLabels.DropFloor,
            new UnwieldSlotContextAction(request, toFloor: true)));
    }

    static void TryAddAction(
        List<ContextMenuEntry> children,
        WieldSlotContextRequest request,
        WeaponActionMask mask,
        WeaponAction action,
        string id,
        string label)
    {
        if ((mask & WeaponActionUtil.ToMask(action)) == 0)
            return;

        children.Add(ContextMenuEntry.Leaf(
            id,
            label,
            new SetHandActionContextAction(request, action)));
    }
}
