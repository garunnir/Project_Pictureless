// ============================================================
// BodyPain — PainTotal / EffectivePain SSOT (Gameplay.Data)
// ============================================================
// LLM 인덱스: docs/body/TUNING.md

using System.Collections.Generic;

namespace Garunnir.Runtime.Gameplay.Data
{
    /// <summary>부상 심각도 × painFactor. 의식 공식·다운이 공유.</summary>
    public static class BodyPain
    {
        public const float PainShockThreshold = 0.8f;
        /// <summary>쇼크 래치 해제. 진입은 <see cref="PainShockThreshold"/>, 기상은 이 값 아래.</summary>
        public const float PainWakeThreshold = 0.5f;
        const float PainShockEpsilon = 0.0001f;
        public const float PainHudMin = 0.2f;
        public const float SeverePainHudMin = 0.55f;
        public const float AdrenalinePainFactor = 0.5f;

        /// <summary>조직 부상(타박·베임·총상) 1 HP당 고통. 한 부위 ~80이면 쇼크 문턱.</summary>
        public const float InjuryPainPerHp = 0.01f;
        public const float BleedPainPerIntensity = 0.025f;
        public const float FracturePainPerIntensity = 0.04f;

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
        public const float WeightBrain = 0.2f;
        public const float WeightHeart = 0.15f;
        public const float WeightLiver = 0.12f;
        public const float WeightLung = 0.08f;
        public const float WeightStomach = 0.08f;
        public const float WeightKidney = 0.05f;
        public const float WeightOther = 0.02f;

        public static bool IsPainShocked(float effectivePain01) =>
            effectivePain01 >= PainShockThreshold - PainShockEpsilon;

        public static bool IsPainDown(float effectivePain01, bool latched)
        {
            if (latched)
                return effectivePain01 >= PainWakeThreshold - PainShockEpsilon;
            return IsPainShocked(effectivePain01);
        }

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
            if (partId == BodyPartIds.Brain)
                return WeightBrain;
            if (partId == BodyPartIds.Heart)
                return WeightHeart;
            if (partId == BodyPartIds.Liver)
                return WeightLiver;
            if (partId == BodyPartIds.LungL || partId == BodyPartIds.LungR)
                return WeightLung;
            if (partId == BodyPartIds.Stomach)
                return WeightStomach;
            if (partId == BodyPartIds.KidneyL || partId == BodyPartIds.KidneyR)
                return WeightKidney;
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
            sum += SumPartPain(body, BodyPartIds.MainConditionParts);
            sum += SumPartPain(body, BodyPartIds.VitalOrgans);
            if (sum < 0f)
                return 0f;
            if (sum > 1f)
                return 1f;
            return sum;
        }

        /// <summary>단일 부위 고통 기여(0–1). 디버그·HUD 분해용.</summary>
        public static float PartPain01(ICharacterBody body, string partId) => PartPain(body, partId);

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
            float value = PainTotal01(body) * PainFactor(body, scratch);
            if (value < 0f)
                return 0f;
            if (value > 1f)
                return 1f;
            return value;
        }

        /// <summary>scratch 없이 의식 판정용. adrenaline 있으면 과대평가될 수 있음 — 틱은 scratch 경로 권장.</summary>
        public static float EffectivePain01NoScratch(ICharacterBody body)
        {
            if (body == null)
                return 0f;
            float total = PainTotal01(body);
            if (!HasAdrenalineAllocating(body))
                return total;
            float value = total * AdrenalinePainFactor;
            if (value > 1f)
                return 1f;
            return value;
        }

        static float SumPartPain(ICharacterBody body, string[] partIds)
        {
            float sum = 0f;
            for (int i = 0; i < partIds.Length; i++)
                sum += PartPain(body, partIds[i]);
            return sum;
        }

        static float PartPain(ICharacterBody body, string id)
        {
            if (!body.TryGet(id, out BodyPartNode node) || node == null)
                return 0f;

            float pain = 0f;
            var effects = node.Effects;
            for (int i = 0; i < effects.Count; i++)
            {
                BodyPartEffect e = effects[i];
                if (e.EffectId == BodyPartEffectIds.Bleed)
                    pain += e.Intensity * BleedPainPerIntensity;
                else
                    pain += e.Intensity * BodyInjury.PainPerIntensity(e.EffectId);
            }

            return pain;
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

        static bool HasAdrenalineAllocating(ICharacterBody body)
        {
            var scratch = new List<BodyPartEffect>(8);
            return HasAdrenaline(body, scratch);
        }
    }
}
