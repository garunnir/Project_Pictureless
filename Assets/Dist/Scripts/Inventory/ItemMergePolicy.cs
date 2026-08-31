// ============================================================
// ItemMergePolicy — 스택 병합 가능 여부 SSOT. 조건 변경은 이 파일만
// ============================================================

using System;
using System.Collections.Generic;
using Garunnir.Runtime.Gameplay.Data;

/// <summary>
/// AddItem/이전은 여기만 호출. 총기·태그·탄창 분기는 이 스크립트 밖 금지.
/// 같음은 <see cref="ItemMergeKey"/> 값 비교.
/// </summary>
public static class ItemMergePolicy
{
    /// <summary>이 플래그가 있으면 KindId만 같으면 합침. 목록 변경은 여기만.</summary>
    static readonly string[] AlwaysMergeFlags = { "STACK_ALWAYS" };

    public static bool CanMerge(ItemStack existing, ItemStack incoming)
    {
        if (existing?.Instance == null || incoming?.Instance == null)
            return false;
        return CanMerge(existing.Item, ItemMergeKey.From(existing), ItemMergeKey.From(incoming));
    }

    public static bool CanMerge(
        ItemStack existing,
        ItemData incomingItem,
        int incomingDamage,
        bool incomingCooked,
        bool incomingHot)
    {
        if (existing?.Instance == null || incomingItem == null)
            return false;
        return CanMerge(
            existing.Item,
            ItemMergeKey.From(existing),
            new ItemMergeKey(
                incomingItem.id,
                incomingDamage,
                chamberRounds: 0,
                hasMagazine: false,
                toolCharges: incomingItem.tool != null
                    ? Math.Max(0, incomingItem.tool.initial_charges)
                    : 0,
                isCooked: incomingCooked,
                isHot: incomingHot));
    }

    public static bool CanMerge(ItemStack existing, ItemData incomingItem, int incomingDamage)
    {
        return CanMerge(existing, incomingItem, incomingDamage, incomingCooked: false, incomingHot: false);
    }

    static bool CanMerge(ItemData existingItem, ItemMergeKey have, ItemMergeKey incoming)
    {
        if (existingItem == null)
            return false;

        if (have.IsRotten != incoming.IsRotten)
            return false;
        if (have.IsCooked != incoming.IsCooked)
            return false;
        if (have.IsHot != incoming.IsHot)
            return false;

        if (AlwaysMerges(existingItem))
            return SameKind(have, incoming);

        if (IsGun(existingItem))
        {
            if (!SameKind(have, incoming) || have.DamageLevel != incoming.DamageLevel)
                return false;
            if (have.HasMagazine || incoming.HasMagazine)
                return false;
            return have.ChamberRounds == incoming.ChamberRounds
                && string.Equals(have.ChamberAmmoId, incoming.ChamberAmmoId, StringComparison.Ordinal)
                && have.ToolCharges == incoming.ToolCharges;
        }

        if (existingItem.magazine != null)
        {
            return SameKind(have, incoming)
                && have.DamageLevel == incoming.DamageLevel
                && have.SupplyRounds == incoming.SupplyRounds
                && string.Equals(have.SupplyAmmoId, incoming.SupplyAmmoId, StringComparison.Ordinal)
                && have.ToolCharges == incoming.ToolCharges;
        }

        return SameKind(have, incoming)
            && have.DamageLevel == incoming.DamageLevel
            && have.ToolCharges == incoming.ToolCharges;
    }

    static bool SameKind(ItemMergeKey a, ItemMergeKey b) =>
        !string.IsNullOrEmpty(a.KindId)
        && string.Equals(a.KindId, b.KindId, StringComparison.Ordinal);

    static bool AlwaysMerges(ItemData item)
    {
        if (item.ammo != null)
            return true;

        List<string> flags = item.flags;
        if (flags == null || flags.Count == 0)
            return false;

        for (int i = 0; i < flags.Count; i++)
        {
            string flag = flags[i];
            if (string.IsNullOrEmpty(flag))
                continue;
            for (int t = 0; t < AlwaysMergeFlags.Length; t++)
            {
                if (flag.Equals(AlwaysMergeFlags[t], StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }

        return false;
    }

    static bool IsGun(ItemData item) => item.gun != null;
}
