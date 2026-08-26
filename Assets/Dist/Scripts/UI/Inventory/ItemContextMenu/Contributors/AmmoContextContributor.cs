// ============================================================
// AmmoContextContributor — 인벤 우클릭 삽탄·장착·교체·분리·탄 빼기
// ============================================================

using System.Collections.Generic;

public sealed class AmmoContextContributor : IContextMenuContributor
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
        var reachable = new List<ItemStack>(32);
        WeaponAmmoService.CollectReachableStacks(session, gear, reachable);

        if (stack.Item.ammo != null)
        {
            ContributeAmmo(stack, session, reachable, roots);
            return;
        }

        if (stack.Item.magazine != null)
        {
            ContributeMagazine(stack, session, reachable, roots);
            return;
        }

        if (stack.Item.gun != null)
            ContributeGun(stack, session, reachable, roots);

        if (WeaponAmmoFit.IsToolAmmoHost(stack.Item))
            ContributeTool(stack, session, reachable, roots);
    }

    static void ContributeTool(
        ItemStack tool,
        InventorySession session,
        List<ItemStack> reachable,
        List<ContextMenuEntry> roots)
    {
        var children = new List<ContextMenuEntry>();
        for (int i = 0; i < reachable.Count; i++)
        {
            ItemStack ammo = reachable[i];
            if (ammo?.Item?.ammo == null || ReferenceEquals(ammo, tool))
                continue;
            if (!WeaponAmmoFit.AcceptsToolAmmoType(tool.Item, ammo.Item))
                continue;

            children.Add(ContextMenuEntry.Leaf(
                "ammo:tool-load:" + i,
                FormatStackLabel(ammo),
                new AmmoLoadContextAction(ammo, tool, session)));
        }

        if (children.Count == 0)
            return;

        roots.Add(ContextMenuEntry.Group("ammo:tool-load", WeaponAmmoLabels.Load, children));
    }

    static void ContributeAmmo(
        ItemStack ammo,
        InventorySession session,
        List<ItemStack> reachable,
        List<ContextMenuEntry> roots)
    {
        var children = new List<ContextMenuEntry>();
        for (int i = 0; i < reachable.Count; i++)
        {
            ItemStack target = reachable[i];
            if (target == null || ReferenceEquals(target, ammo))
                continue;

            ItemStack mag = WeaponAmmoFit.ResolveLoadMagazine(target);
            bool clip = mag == null && WeaponAmmoFit.IsClipFed(target.Item);
            bool tool = mag == null && !clip && WeaponAmmoFit.IsToolAmmoHost(target.Item);
            if (mag != null)
            {
                if (!WeaponAmmoFit.AcceptsAmmoType(mag.Item, ammo.Item))
                    continue;
            }
            else if (clip)
            {
                if (!WeaponAmmoFit.AcceptsGunAmmoType(target.Item, ammo.Item))
                    continue;
            }
            else if (tool)
            {
                if (!WeaponAmmoFit.AcceptsToolAmmoType(target.Item, ammo.Item))
                    continue;
            }
            else
            {
                continue;
            }

            children.Add(ContextMenuEntry.Leaf(
                "ammo:load:" + i,
                FormatStackLabel(target),
                new AmmoLoadContextAction(ammo, target, session)));
        }

        if (children.Count == 0)
            return;

        roots.Add(ContextMenuEntry.Group("ammo:load", WeaponAmmoLabels.Load, children));
    }

    static void ContributeMagazine(
        ItemStack magazine,
        InventorySession session,
        List<ItemStack> reachable,
        List<ContextMenuEntry> roots)
    {
        if (magazine.Instance != null && magazine.Instance.SupplyRounds > 0)
        {
            roots.Add(ContextMenuEntry.Leaf(
                "ammo:unload",
                WeaponAmmoLabels.Unload,
                new AmmoUnloadContextAction(magazine, session)));
        }

        var attachChildren = new List<ContextMenuEntry>();
        bool anyAttach = false;
        bool anySwap = false;
        for (int i = 0; i < reachable.Count; i++)
        {
            ItemStack gun = reachable[i];
            if (gun?.Item?.gun == null || ReferenceEquals(gun, magazine))
                continue;
            if (ReferenceEquals(gun.LoadedMagazine, magazine))
                continue;
            if (!WeaponAmmoFit.AcceptsMagazine(gun.Item, magazine.Item))
                continue;

            if (gun.LoadedMagazine != null)
                anySwap = true;
            else
                anyAttach = true;

            attachChildren.Add(ContextMenuEntry.Leaf(
                "ammo:attach:" + i,
                FormatStackLabel(gun),
                new AmmoAttachContextAction(magazine, gun, session)));
        }

        if (attachChildren.Count == 0)
            return;

        string groupLabel = anySwap && !anyAttach ? WeaponAmmoLabels.Swap : WeaponAmmoLabels.Attach;
        roots.Add(ContextMenuEntry.Group("ammo:attach", groupLabel, attachChildren));
    }

    static void ContributeGun(
        ItemStack gun,
        InventorySession session,
        List<ItemStack> reachable,
        List<ContextMenuEntry> roots)
    {
        if (gun.LoadedMagazine != null)
        {
            roots.Add(ContextMenuEntry.Leaf(
                "ammo:detach",
                WeaponAmmoLabels.Detach,
                new AmmoDetachContextAction(gun, session)));

            if (gun.LoadedMagazine.Instance != null && gun.LoadedMagazine.Instance.SupplyRounds > 0)
            {
                roots.Add(ContextMenuEntry.Leaf(
                    "ammo:unload-gun",
                    WeaponAmmoLabels.Unload,
                    new AmmoUnloadContextAction(gun.LoadedMagazine, session)));
            }
        }

        var magChildren = new List<ContextMenuEntry>();
        for (int i = 0; i < reachable.Count; i++)
        {
            ItemStack magazine = reachable[i];
            if (magazine?.Item?.magazine == null || ReferenceEquals(magazine, gun))
                continue;
            if (ReferenceEquals(gun.LoadedMagazine, magazine))
                continue;
            if (!WeaponAmmoFit.AcceptsMagazine(gun.Item, magazine.Item))
                continue;

            magChildren.Add(ContextMenuEntry.Leaf(
                "ammo:gun-attach:" + i,
                FormatStackLabel(magazine),
                new AmmoAttachContextAction(magazine, gun, session)));
        }

        if (magChildren.Count == 0)
            return;

        roots.Add(ContextMenuEntry.Group(
            "ammo:gun-attach",
            gun.LoadedMagazine != null ? WeaponAmmoLabels.Swap : WeaponAmmoLabels.Attach,
            magChildren));
    }

    static string FormatStackLabel(ItemStack stack)
    {
        if (stack?.Item == null)
            return WeaponAmmoLabels.Blocked;
        return ItemAmmoLabels.AppendState(UITextPresenter.GetItemName(stack.Item), stack);
    }
}
