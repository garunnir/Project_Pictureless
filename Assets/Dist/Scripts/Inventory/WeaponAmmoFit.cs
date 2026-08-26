// ============================================================
// WeaponAmmoFit — 탄창 탄종·총 허용 탄창 id (비컨테이너)
// ============================================================

using System;
using Garunnir.Runtime.Gameplay.Data;

public static class WeaponAmmoFit
{
    public static bool HasMagazineWell(ItemData gun)
    {
        if (gun?.gun?.magazines == null)
            return false;

        for (int i = 0; i < gun.gun.magazines.Count; i++)
        {
            GunMagazineGroup group = gun.gun.magazines[i];
            if (group?.magazines == null || group.magazines.Count == 0)
                continue;
            return true;
        }

        return false;
    }

    public static bool IsClipFed(ItemData gun) =>
        gun?.gun != null && gun.gun.clip_size > 0 && !HasMagazineWell(gun);

    public static bool AcceptsMagazine(ItemData gun, ItemData magazine)
    {
        if (gun?.gun?.magazines == null || magazine?.magazine == null)
            return false;
        if (string.IsNullOrEmpty(magazine.id))
            return false;

        for (int i = 0; i < gun.gun.magazines.Count; i++)
        {
            GunMagazineGroup group = gun.gun.magazines[i];
            if (group?.magazines == null)
                continue;
            for (int m = 0; m < group.magazines.Count; m++)
            {
                if (string.Equals(group.magazines[m], magazine.id, StringComparison.Ordinal))
                    return true;
            }
        }

        return false;
    }

    public static bool AcceptsAmmoType(ItemData magazine, ItemData ammo)
    {
        if (magazine?.magazine?.ammo_type == null || ammo?.ammo == null)
            return false;
        if (string.IsNullOrEmpty(ammo.ammo.ammo_type))
            return false;

        for (int i = 0; i < magazine.magazine.ammo_type.Count; i++)
        {
            if (string.Equals(
                    magazine.magazine.ammo_type[i],
                    ammo.ammo.ammo_type,
                    StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    public static bool AcceptsGunAmmoType(ItemData gun, ItemData ammo)
    {
        if (gun?.gun?.ammo == null || ammo?.ammo == null)
            return false;
        if (string.IsNullOrEmpty(ammo.ammo.ammo_type))
            return false;

        for (int i = 0; i < gun.gun.ammo.Count; i++)
        {
            if (string.Equals(gun.gun.ammo[i], ammo.ammo.ammo_type, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    public static bool CanLoadMagazine(ItemStack magazine, ItemData ammo)
    {
        if (magazine?.Instance == null || ammo?.ammo == null)
            return false;
        if (!AcceptsAmmoType(magazine.Item, ammo))
            return false;

        int capacity = magazine.Item.magazine != null ? magazine.Item.magazine.capacity : 0;
        if (capacity <= 0 || magazine.Instance.SupplyRounds >= capacity)
            return false;

        if (magazine.Instance.SupplyRounds > 0 &&
            !string.Equals(magazine.Instance.SupplyAmmoId, ammo.id, StringComparison.Ordinal))
            return false;

        return true;
    }

    public static bool CanLoadClip(ItemStack gun, ItemData ammo)
    {
        if (gun?.Instance == null || ammo?.ammo == null)
            return false;
        if (!IsClipFed(gun.Item) || !AcceptsGunAmmoType(gun.Item, ammo))
            return false;

        int capacity = gun.Item.gun.clip_size;
        if (gun.Instance.ChamberRounds >= capacity)
            return false;

        if (gun.Instance.ChamberRounds > 0 &&
            !string.Equals(gun.Instance.ChamberAmmoId, ammo.id, StringComparison.Ordinal))
            return false;

        return true;
    }

    /// <summary>tool.ammo + max_charges 공구 (fish_trap 등).</summary>
    public static bool IsToolAmmoHost(ItemData tool) =>
        tool?.tool != null &&
        tool.tool.max_charges > 0 &&
        tool.tool.ammo != null &&
        tool.tool.ammo.Count > 0;

    public static int ResolveToolCapacity(ItemData tool) =>
        tool?.tool != null ? Math.Max(0, tool.tool.max_charges) : 0;

    public static bool AcceptsToolAmmoType(ItemData tool, ItemData ammo)
    {
        if (!IsToolAmmoHost(tool) || ammo?.ammo == null)
            return false;

        string ammoId = ammo.id;
        string ammoType = ammo.ammo.ammo_type;
        for (int i = 0; i < tool.tool.ammo.Count; i++)
        {
            string allowed = tool.tool.ammo[i];
            if (string.IsNullOrEmpty(allowed))
                continue;
            if (allowed.Equals(ammoId, StringComparison.OrdinalIgnoreCase))
                return true;
            if (!string.IsNullOrEmpty(ammoType) &&
                allowed.Equals(ammoType, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    public static bool CanLoadTool(ItemStack toolStack, ItemData ammo)
    {
        if (toolStack?.Instance == null || !IsToolAmmoHost(toolStack.Item))
            return false;
        if (!AcceptsToolAmmoType(toolStack.Item, ammo))
            return false;

        int capacity = ResolveToolCapacity(toolStack.Item);
        if (capacity <= 0 || toolStack.Instance.SupplyRounds >= capacity)
            return false;

        if (toolStack.Instance.SupplyRounds > 0 &&
            !string.Equals(toolStack.Instance.SupplyAmmoId, ammo.id, StringComparison.Ordinal))
            return false;

        return true;
    }

    public static ItemStack ResolveLoadMagazine(ItemStack target)
    {
        if (target?.Item?.magazine != null)
            return target;
        if (target?.Item?.gun != null)
            return target.LoadedMagazine;
        return null;
    }
}
