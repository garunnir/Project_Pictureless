// ============================================================
// WieldSlotActionsContributor — HandAction Family + 잡기(반대손/양손) + Unwield/Floor
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
            var actionChildren = new List<ContextMenuEntry>(WeaponActionUtil.All.Length + 4);

            var melee = new List<ContextMenuEntry>(2);
            var trigger = new List<ContextMenuEntry>(3);
            var leaves = new List<WeaponAction>(WeaponActionUtil.All.Length);
            WeaponActionUtil.CollectAvailableLeaves(mask, leaves);
            for (int i = 0; i < leaves.Count; i++)
            {
                WeaponAction leaf = leaves[i];
                string id = "hand-" + WeaponActionUtil.LeafLabel(leaf).ToLowerInvariant();
                string label = CharacterGearLabels.ActionLabel(leaf);
                var entry = ContextMenuEntry.Leaf(
                    id,
                    label,
                    new SetHandActionContextAction(request, leaf));

                if (!WeaponActionUtil.TryGetFamily(leaf, out WeaponActionFamily family))
                {
                    actionChildren.Add(entry);
                    continue;
                }

                if (family == WeaponActionFamily.Melee)
                    melee.Add(entry);
                else if (family == WeaponActionFamily.Trigger)
                    trigger.Add(entry);
            }

            if (melee.Count > 0)
            {
                actionChildren.Add(ContextMenuEntry.Group(
                    "hand-family-melee",
                    CharacterGearLabels.FamilyMelee,
                    melee));
            }

            if (trigger.Count > 0)
            {
                actionChildren.Add(ContextMenuEntry.Group(
                    "hand-family-trigger",
                    CharacterGearLabels.FamilyTrigger,
                    trigger));
            }

            actionChildren.Add(ContextMenuEntry.Leaf(
                "hand-none",
                CharacterGearLabels.ActionNone,
                new SetHandActionContextAction(request, null)));

            roots.Add(ContextMenuEntry.Group(
                "hand-action",
                CharacterGearLabels.HandActionGroup,
                actionChildren));

            WieldHand opposite = WieldSlots.OppositeHand(request.Slot);
            var gripChildren = new List<ContextMenuEntry>(2)
            {
                ContextMenuEntry.Leaf(
                    "grip-opposite",
                    CharacterGearLabels.WieldOpposite,
                    new WieldGripContextAction(request, opposite)),
                ContextMenuEntry.Leaf(
                    "grip-twohand",
                    CharacterGearLabels.WieldTwoHand,
                    new WieldGripContextAction(request, WieldHand.TwoHand)),
            };
            roots.Add(ContextMenuEntry.Group(
                "grip",
                CharacterGearLabels.WieldGripGroup,
                gripChildren));
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
}
