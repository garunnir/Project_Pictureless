// ============================================================
// WieldSlotActionsContributor — HandAction 그룹 + Unwield/Floor
// ============================================================

using System.Collections.Generic;
using Garunnir.Runtime.Gameplay.Data;

public sealed class WieldSlotActionsContributor : IWieldSlotContextMenuContributor
{
    public void Contribute(WieldSlotContextRequest request, List<ContextMenuEntry> roots)
    {
        if (request?.Gear == null || roots == null || string.IsNullOrEmpty(request.ItemId))
            return;

        ItemData item = request.Gear.Wield?.Get(request.Slot)?.Item;
        if (item != null)
        {
            WeaponActionMask mask = CombatMath.AvailableModes(item);
            var actionChildren = new List<ContextMenuEntry>(4);
            TryAddAction(actionChildren, request, mask, WeaponAction.Bashing, "hand-bash", CharacterGearLabels.ActionBash);
            TryAddAction(actionChildren, request, mask, WeaponAction.Cutting, "hand-cut", CharacterGearLabels.ActionCut);
            TryAddAction(actionChildren, request, mask, WeaponAction.Gun, "hand-gun", CharacterGearLabels.ActionGun);
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
