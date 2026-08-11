// ============================================================
// WearOverlapRules — 착용 부위·레이어·sided 겹침 충돌 SSOT
// ============================================================

using System;
using System.Collections.Generic;
using Garunnir.Runtime.Gameplay.Data;

/// <summary>
/// Same body part + same layer → conflict unless both sided and under MaxSidedPerLayer.
/// Policy: reject (no auto-replace). See docs/equipment/GEAR.md Phase C.
/// </summary>
public static class WearOverlapRules
{
    public static string NormalizeLayer(ArmorDetailData armor)
    {
        if (armor == null || string.IsNullOrWhiteSpace(armor.layer))
            return GearConstants.DefaultArmorLayer;
        return armor.layer.Trim().ToUpperInvariant();
    }

    public static bool IsSided(ArmorDetailData armor) =>
        armor != null && armor.sided;

    /// <summary>
    /// Returns true if wearing <paramref name="candidate"/> would conflict with current wear.
    /// </summary>
    public static bool HasConflict(EquipmentWearState wear, ItemData candidate)
    {
        return TryFindConflict(wear, candidate, out _);
    }

    public static bool TryFindConflict(
        EquipmentWearState wear,
        ItemData candidate,
        out ItemStack conflicting)
    {
        conflicting = null;
        if (wear == null || candidate?.armor == null)
            return false;
        if (!GearHandleRules.IsWearable(candidate))
            return false;

        ArmorDetailData candArmor = candidate.armor;
        string candLayer = NormalizeLayer(candArmor);
        bool candSided = IsSided(candArmor);
        IReadOnlyList<ItemStack> worn = wear.Worn;

        if (candSided)
        {
            int sidedPeers = 0;
            ItemStack firstPeer = null;
            for (int i = 0; i < worn.Count; i++)
            {
                ItemStack stack = worn[i];
                if (!OverlapsLayerAndPart(candArmor, candLayer, stack?.Item?.armor))
                    continue;
                if (!IsSided(stack.Item.armor))
                {
                    conflicting = stack;
                    return true;
                }

                sidedPeers++;
                if (firstPeer == null)
                    firstPeer = stack;
            }

            if (sidedPeers >= GearConstants.MaxSidedPerLayer)
            {
                conflicting = firstPeer;
                return true;
            }

            return false;
        }

        for (int i = 0; i < worn.Count; i++)
        {
            ItemStack stack = worn[i];
            if (!OverlapsLayerAndPart(candArmor, candLayer, stack?.Item?.armor))
                continue;
            conflicting = stack;
            return true;
        }

        return false;
    }

    public static string GetBlockedReason(EquipmentWearState wear, ItemData candidate)
    {
        if (!TryFindConflict(wear, candidate, out ItemStack conflict) || conflict == null)
            return null;

        string otherName = conflict.Item != null
            ? UITextPresenter.GetItemName(conflict.Item)
            : null;
        if (string.IsNullOrEmpty(otherName) || otherName.StartsWith("[Missing:", StringComparison.Ordinal))
            otherName = conflict.Item?.id ?? "?";
        return CharacterGearLabels.FormatWearOverlap(otherName);
    }

    static bool OverlapsLayerAndPart(
        ArmorDetailData candidate,
        string candidateLayer,
        ArmorDetailData wornArmor)
    {
        if (wornArmor == null)
            return false;
        if (!string.Equals(candidateLayer, NormalizeLayer(wornArmor), StringComparison.Ordinal))
            return false;
        return SharesCover(candidate, wornArmor);
    }

    static bool SharesCover(ArmorDetailData a, ArmorDetailData b)
    {
        if (a?.covers == null || b?.covers == null)
            return false;

        for (int i = 0; i < a.covers.Count; i++)
        {
            string part = a.covers[i];
            if (string.IsNullOrEmpty(part))
                continue;
            for (int j = 0; j < b.covers.Count; j++)
            {
                if (string.Equals(part, b.covers[j], StringComparison.Ordinal))
                    return true;
            }
        }

        return false;
    }
}
