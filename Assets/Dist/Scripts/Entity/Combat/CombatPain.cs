// ============================================================
// CombatPain — PainTotal / 쇼크 문턱 SSOT (밀침 J와 분리)
// ============================================================

using System.Collections.Generic;
using Garunnir.Runtime.Gameplay.Data;
using UnityEngine;

/// <summary>손실 HP 합 × 부위 가중 × painFactor. 문서: docs/body/BODY.md · LOCOMOTION 상수 표.</summary>
public static class CombatPain
{
    public const float PainShockThreshold = 0.8f;

    /// <summary>HUD Pain 아이콘 하한 (effective).</summary>
    public const float PainHudMin = 0.2f;

    /// <summary>HUD SeverePain 아이콘 하한 (effective).</summary>
    public const float SeverePainHudMin = 0.55f;

    /// <summary>adrenaline 효과가 있으면 PainTotal에 곱함.</summary>
    public const float AdrenalinePainFactor = 0.5f;

    public const float WeightHead = 0.25f;
    public const float WeightNeck = 0.08f;
    public const float WeightChest = 0.22f;
    public const float WeightBelly = 0.12f;
    public const float WeightPelvis = 0.08f;
    public const float WeightUpperArm = 0.04f;
    public const float WeightLowerArm = 0.02f;
    public const float WeightHand = 0.015f;
    public const float WeightThigh = 0.05f;
    public const float WeightCalf = 0.02f;
    public const float WeightFoot = 0.015f;
    public const float WeightOther = 0.02f;

    public static float PartWeight(string partId)
    {
        if (string.IsNullOrEmpty(partId))
            return WeightOther;
        if (partId == BodyPartIds.Head)
            return WeightHead;
        if (partId == BodyPartIds.Neck)
            return WeightNeck;
        if (partId == BodyPartIds.Chest)
            return WeightChest;
        if (partId == BodyPartIds.Belly)
            return WeightBelly;
        if (partId == BodyPartIds.Pelvis)
            return WeightPelvis;
        if (partId == BodyPartIds.UpperArmL || partId == BodyPartIds.UpperArmR)
            return WeightUpperArm;
        if (partId == BodyPartIds.LowerArmL || partId == BodyPartIds.LowerArmR)
            return WeightLowerArm;
        if (partId == BodyPartIds.HandL || partId == BodyPartIds.HandR)
            return WeightHand;
        if (partId == BodyPartIds.ThighL || partId == BodyPartIds.ThighR)
            return WeightThigh;
        if (partId == BodyPartIds.CalfL || partId == BodyPartIds.CalfR)
            return WeightCalf;
        if (partId == BodyPartIds.FootL || partId == BodyPartIds.FootR)
            return WeightFoot;
        return WeightOther;
    }

    public static float PainTotal01(ICharacterBody body)
    {
        if (body == null)
            return 0f;

        float sum = 0f;
        string[] mains = BodyPartIds.MainConditionParts;
        for (int i = 0; i < mains.Length; i++)
        {
            string id = mains[i];
            int max = body.GetConditionMax(id);
            if (max <= 0)
                continue;
            int cur = body.GetConditionCur(id);
            if (cur < 0)
                cur = 0;
            if (cur > max)
                cur = max;
            float missing01 = (max - cur) / (float)max;
            sum += missing01 * PartWeight(id);
        }

        return Mathf.Clamp01(sum);
    }

    public static float PainFactor(ICharacterBody body, List<BodyPartEffect> scratch)
    {
        if (body == null || scratch == null)
            return 1f;
        if (!HasAdrenaline(body, scratch))
            return 1f;
        return AdrenalinePainFactor;
    }

    public static float EffectivePain01(ICharacterBody body, List<BodyPartEffect> scratch)
    {
        return Mathf.Clamp01(PainTotal01(body) * PainFactor(body, scratch));
    }

    static bool HasAdrenaline(ICharacterBody body, List<BodyPartEffect> scratch)
    {
        string[] mains = BodyPartIds.MainConditionParts;
        for (int i = 0; i < mains.Length; i++)
        {
            scratch.Clear();
            body.CollectEffectsUnder(mains[i], scratch, false);
            for (int e = 0; e < scratch.Count; e++)
            {
                if (scratch[e].EffectId == BodyPartEffectIds.Adrenaline)
                    return true;
            }
        }

        return false;
    }
}
