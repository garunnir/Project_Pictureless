// ============================================================
// BodyPain — PainTotal / EffectivePain SSOT (Gameplay.Data)
// ============================================================

using System.Collections.Generic;

namespace Garunnir.Runtime.Gameplay.Data
{
    /// <summary>손실 HP × 부위 가중 × painFactor. 의식 공식·다운이 공유.</summary>
    public static class BodyPain
    {
        public const float PainShockThreshold = 0.8f;
        public const float PainHudMin = 0.2f;
        public const float SeverePainHudMin = 0.55f;
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
        public const float WeightBrain = 0.2f;
        public const float WeightHeart = 0.15f;
        public const float WeightLiver = 0.12f;
        public const float WeightLung = 0.08f;
        public const float WeightStomach = 0.08f;
        public const float WeightKidney = 0.05f;
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
            sum += SumMissing(body, BodyPartIds.MainConditionParts);
            sum += SumMissing(body, BodyPartIds.VitalOrgans);
            if (sum < 0f)
                return 0f;
            if (sum > 1f)
                return 1f;
            return sum;
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

        static float SumMissing(ICharacterBody body, string[] partIds)
        {
            float sum = 0f;
            for (int i = 0; i < partIds.Length; i++)
            {
                string id = partIds[i];
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

            return sum;
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
