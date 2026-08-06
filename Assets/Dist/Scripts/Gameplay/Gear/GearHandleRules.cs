// ============================================================
// GearHandleRules — CanLift / RequiredStr / LiftStrain (힘 게이트)
// ============================================================

using System;
using Garunnir.Runtime.Gameplay.Data;
using UnityEngine;

public static class GearHandleRules
{
    public static int RequiredStr(int weightG, bool twoHand)
    {
        int grams = Mathf.Max(0, weightG);
        int divisor = GearConstants.GramsPerStr;
        if (twoHand)
            divisor *= GearConstants.TwoHandWeightFactor;
        if (divisor <= 0)
            return 0;
        return Mathf.CeilToInt(grams / (float)divisor);
    }

    public static int RequiredStr(ItemData item, bool twoHand) =>
        RequiredStr(item != null ? item.weight_g : 0, twoHand);

    /// <summary>착용(Wear)은 양손 배율 없음 — 한손 공식만.</summary>
    public static int RequiredStrForWear(ItemData item) =>
        RequiredStr(item, twoHand: false);

    public static bool CanLift(int strength, int weightG, bool twoHand) =>
        strength >= RequiredStr(weightG, twoHand);

    public static bool CanLift(int strength, ItemData item, bool twoHand) =>
        CanLift(strength, item != null ? item.weight_g : 0, twoHand);

    /// <summary>
    /// 하드 통과 후 여유 힘이 SoftMargin 미만이면 true.
    /// 실패(들 수 없음)면 false.
    /// </summary>
    public static bool HasLiftStrain(int strength, int weightG, bool twoHand)
    {
        int required = RequiredStr(weightG, twoHand);
        if (strength < required)
            return false;
        return (strength - required) < GearConstants.SoftMargin;
    }

    public static bool HasLiftStrain(int strength, ItemData item, bool twoHand) =>
        HasLiftStrain(strength, item != null ? item.weight_g : 0, twoHand);

    public static bool IsWearable(ItemData item) =>
        item?.armor != null
        && item.armor.covers != null
        && item.armor.covers.Count > 0;

    public static bool IsTwoHandWeapon(ItemData item)
    {
        if (item?.flags == null)
            return false;

        for (int i = 0; i < item.flags.Count; i++)
        {
            string flag = item.flags[i];
            if (string.IsNullOrEmpty(flag))
                continue;
            if (flag.Equals("TWO_HAND", StringComparison.OrdinalIgnoreCase)
                || flag.Equals("TWO-HANDED", StringComparison.OrdinalIgnoreCase)
                || flag.Equals("TWOHAND", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    public static bool CoversPart(ItemData item, string partId)
    {
        if (item?.armor?.covers == null || string.IsNullOrEmpty(partId))
            return false;

        for (int i = 0; i < item.armor.covers.Count; i++)
        {
            if (string.Equals(item.armor.covers[i], partId, StringComparison.Ordinal))
                return true;
        }

        return false;
    }
}
