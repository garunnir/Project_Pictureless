// ============================================================
// CombatPain — BodyPain 래퍼 (밀침 J와 분리, DistScript 소비처용)
// ============================================================

using System.Collections.Generic;
using Garunnir.Runtime.Gameplay.Data;

/// <summary>문서: docs/body/BODY.md · LOCOMOTION 상수 표. 공식 SSOT는 BodyPain.</summary>
public static class CombatPain
{
    public const float PainShockThreshold = BodyPain.PainShockThreshold;
    public const float PainWakeThreshold = BodyPain.PainWakeThreshold;
    public const float PainHudMin = BodyPain.PainHudMin;
    public const float SeverePainHudMin = BodyPain.SeverePainHudMin;
    public const float AdrenalinePainFactor = BodyPain.AdrenalinePainFactor;

    public const float WeightHead = BodyPain.WeightHead;
    public const float WeightNeck = BodyPain.WeightNeck;
    public const float WeightChest = BodyPain.WeightChest;
    public const float WeightBelly = BodyPain.WeightBelly;
    public const float WeightPelvis = BodyPain.WeightPelvis;
    public const float WeightUpperArm = BodyPain.WeightUpperArm;
    public const float WeightLowerArm = BodyPain.WeightLowerArm;
    public const float WeightHand = BodyPain.WeightHand;
    public const float WeightThigh = BodyPain.WeightThigh;
    public const float WeightCalf = BodyPain.WeightCalf;
    public const float WeightFoot = BodyPain.WeightFoot;
    public const float WeightOther = BodyPain.WeightOther;

    public static float PartWeight(string partId) => BodyPain.PartWeight(partId);

    public static float PainTotal01(ICharacterBody body) => BodyPain.PainTotal01(body);

    public static float PartPain01(ICharacterBody body, string partId) => BodyPain.PartPain01(body, partId);

    public static float PainFactor(ICharacterBody body, List<BodyPartEffect> scratch) =>
        BodyPain.PainFactor(body, scratch);

    public static float EffectivePain01(ICharacterBody body, List<BodyPartEffect> scratch) =>
        BodyPain.EffectivePain01(body, scratch);

    public static bool IsPainShocked(float effectivePain01) =>
        BodyPain.IsPainShocked(effectivePain01);

    public static bool IsPainDown(float effectivePain01, bool latched) =>
        BodyPain.IsPainDown(effectivePain01, latched);
}
