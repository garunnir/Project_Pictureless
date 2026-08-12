// ============================================================
// WeaponChamber — 약실 SSOT 조회·메거진 보급 1발 (Attack이 호출)
// ============================================================

using Garunnir.Runtime.Gameplay.Data;

/// <summary>
/// 약실은 ItemInstance. 메거진은 Nested/장착 보급만. clip_size는 용량 힌트.
/// </summary>
public static class WeaponChamber
{
    public const int MagazineFedCapacity = 1;

    public static bool FeedsOnFire(WeaponAttack attack) =>
        attack == null || attack.FeedsChamberOnFire;

    public static int ChamberCapacity(ItemData item, bool hasMagazine)
    {
        if (hasMagazine)
            return MagazineFedCapacity;

        int clip = item?.gun != null ? item.gun.clip_size : 0;
        return clip > 0 ? clip : 0;
    }

    public static bool TryGetMagazine(ItemStack weapon, out ItemStack magazine)
    {
        magazine = null;
        InventoryContainer nested = weapon?.Nested;
        if (nested == null)
            return false;

        var stacks = nested.Stacks;
        for (int i = 0; i < stacks.Count; i++)
        {
            ItemStack candidate = stacks[i];
            if (candidate?.Item?.magazine == null)
                continue;
            magazine = candidate;
            return true;
        }

        return false;
    }

    public static bool HasSupply(ItemStack magazine)
    {
        if (magazine == null)
            return false;
        if (magazine.Instance != null && magazine.Instance.SupplyRounds > 0)
            return true;
        return FindAmmoStack(magazine.Nested) != null;
    }

    public static bool CanCommitFire(
        ItemData item,
        ItemInstance instance,
        ItemStack weapon,
        WeaponAttack attack)
    {
        if (item?.gun == null || instance == null)
            return false;
        if (instance.ChamberRounds > 0)
            return true;
        if (!FeedsOnFire(attack))
            return false;
        return TryGetMagazine(weapon, out ItemStack magazine) && HasSupply(magazine);
    }

    public static bool EnsureChamberForFire(
        ItemInstance instance,
        ItemStack weapon,
        ItemData item,
        WeaponAttack attack)
    {
        if (instance == null)
            return false;
        if (instance.ChamberRounds > 0)
            return true;
        if (!FeedsOnFire(attack))
            return false;
        return TryFeedFromMagazine(instance, weapon, item);
    }

    public static bool TryFeedFromMagazine(
        ItemInstance instance,
        ItemStack weapon,
        ItemData item)
    {
        if (instance == null || instance.ChamberRounds > 0)
            return instance != null && instance.ChamberRounds > 0;
        if (!TryGetMagazine(weapon, out ItemStack magazine))
            return false;

        int capacity = ChamberCapacity(item, hasMagazine: true);
        if (capacity <= 0 || instance.ChamberRounds >= capacity)
            return false;
        if (!TryTakeOneFromMagazine(magazine, out string ammoId))
            return false;
        return instance.TryAddChamberRound(capacity, ammoId);
    }

    public static bool TryConsume(ItemInstance instance) =>
        instance != null && instance.TryConsumeChamberRound();

    /// <summary>기어/어택이 호출. 메거진 보급 1발 → 약실. WeaponAction.Reload 없음.</summary>
    public static bool TryReload(ItemInstance instance, ItemStack weapon, ItemData item) =>
        TryFeedFromMagazine(instance, weapon, item);

    /// <summary>약실에 기록된 탄, 없으면 메거진/총 Nested의 탄 스택.</summary>
    public static ItemData ResolveAmmo(ItemStack weapon, ItemInstance instance = null)
    {
        instance ??= weapon?.Instance;
        if (instance != null && !string.IsNullOrEmpty(instance.ChamberAmmoId))
        {
            ItemData loaded = GameplayData.GetItem(instance.ChamberAmmoId);
            if (loaded?.ammo != null)
                return loaded;
        }

        if (TryGetMagazine(weapon, out ItemStack magazine))
        {
            ItemStack magAmmo = FindAmmoStack(magazine.Nested);
            if (magAmmo?.Item?.ammo != null)
                return magAmmo.Item;
        }

        ItemStack loose = FindAmmoStack(weapon?.Nested);
        return loose?.Item?.ammo != null ? loose.Item : null;
    }

    public static int ResolvePierce(ItemStack weapon, ItemInstance instance = null)
    {
        ItemData ammo = ResolveAmmo(weapon, instance);
        int pierce = ammo?.ammo != null ? ammo.ammo.pierce : 0;
        return pierce > 0 ? pierce : 0;
    }

    static bool TryTakeOneFromMagazine(ItemStack magazine, out string ammoId)
    {
        ammoId = null;
        ItemStack ammo = FindAmmoStack(magazine?.Nested);
        if (ammo?.Item != null && magazine.Nested.RemoveItem(ammo.Item, 1) > 0)
        {
            ammoId = ammo.Item.id;
            return true;
        }

        return magazine?.Instance != null && magazine.Instance.TryTakeSupplyRound();
    }

    static ItemStack FindAmmoStack(InventoryContainer nested)
    {
        if (nested == null)
            return null;

        var stacks = nested.Stacks;
        for (int i = 0; i < stacks.Count; i++)
        {
            ItemStack candidate = stacks[i];
            if (candidate?.Item?.ammo == null || candidate.Count <= 0)
                continue;
            return candidate;
        }

        return null;
    }
}
