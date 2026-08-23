// ============================================================
// BodyCapacity — 의식·펌프·호흡·여과·소화·이동·조작 용량 SSOT
// ============================================================

namespace Garunnir.Runtime.Gameplay.Data
{
    /// <summary>
    /// IsFatal = 의식 ≤ 0 만. 펌프/호흡/여과/소화/이동/조작 0은 사망 아님.
    /// </summary>
    public static class BodyCapacity
    {
        public const float ConsciousnessDownedThreshold = 0.3f;
        public const float MovingDownedThreshold = 0.15f;
        public const float MissingFootAsMove = 0.5f;
        public const float MissingHandAsManip = 0.35f;
        public const float ManipTickMin = 0.15f;

        public static bool IsFatal(ICharacterBody body) =>
            Consciousness(body) <= 0f;

        public static bool IsCapacityDowned(ICharacterBody body)
        {
            if (body == null)
                return false;

            float con = Consciousness(body);
            if (con <= 0f)
                return false;
            if (con < ConsciousnessDownedThreshold)
                return true;
            if (Moving(body) < MovingDownedThreshold)
                return true;
            if (Breathing(body) <= 0f)
                return true;
            return false;
        }

        public static float Consciousness(ICharacterBody body)
        {
            if (body == null)
                return 0f;

            float brain = OrganEff(body, BodyPartIds.Brain);
            float blood = BloodFactor(body.Blood01);
            float infection = 1f - BodyIllness.InfectionConsciousnessK * body.InfectionProgress01;
            float toxin = 1f - BodyIllness.ToxinConsciousnessK * body.Toxin01;
            float pain = 1f - BodyPain.EffectivePain01NoScratch(body);

            float value = brain * blood * Clamp01NonNeg(infection) * Clamp01NonNeg(toxin) * Clamp01NonNeg(pain);
            return Clamp01NonNeg(value);
        }

        public static float BloodPumping(ICharacterBody body)
        {
            if (body == null)
                return 0f;
            return Clamp01NonNeg(OrganEff(body, BodyPartIds.Heart) * BloodFactor(body.Blood01));
        }

        public static float Breathing(ICharacterBody body)
        {
            if (body == null)
                return 0f;
            return Clamp01NonNeg(LungEff(body));
        }

        public static float BloodFiltration(ICharacterBody body)
        {
            if (body == null)
                return 0f;

            float organs = OrganEff(body, BodyPartIds.Liver) * KidneyEff(body);
            float toxin = 1f - BodyIllness.ToxinFiltrationK * body.Toxin01;
            return Clamp01NonNeg(organs * Clamp01NonNeg(toxin));
        }

        public static float Digestion(ICharacterBody body)
        {
            if (body == null)
                return 0f;
            return Clamp01NonNeg(OrganEff(body, BodyPartIds.Stomach));
        }

        public static float Moving(ICharacterBody body)
        {
            if (body == null)
                return 0f;

            float legs = (LegSideEff(body, BodyPartIds.ThighL, BodyPartIds.FootL)
                          + LegSideEff(body, BodyPartIds.ThighR, BodyPartIds.FootR)) * 0.5f;
            float pelvis = PartPresentEff(body, BodyPartIds.Pelvis);
            return Clamp01NonNeg(legs * Consciousness(body) * pelvis);
        }

        public static float Manipulation(ICharacterBody body)
        {
            if (body == null)
                return 0f;

            float arms = (ArmSideEff(body, BodyPartIds.UpperArmL, BodyPartIds.LowerArmL, BodyPartIds.HandL)
                          + ArmSideEff(body, BodyPartIds.UpperArmR, BodyPartIds.LowerArmR, BodyPartIds.HandR))
                         * 0.5f;
            return Clamp01NonNeg(arms * Consciousness(body));
        }

        public static float OrganEff(ICharacterBody body, string organId)
        {
            if (body == null || string.IsNullOrEmpty(organId))
                return 0f;

            string parentId = BodyPartIds.GetOrganParentId(organId);
            if (!string.IsNullOrEmpty(parentId) && !IsUsableMain(body, parentId))
                return 0f;

            if (!body.TryGet(organId, out BodyPartNode node) || !node.HasCondition)
                return 0f;
            if (node.ConditionMax <= 0 || node.ConditionCur <= 0)
                return 0f;

            return node.ConditionCur / (float)node.ConditionMax;
        }

        public static float LungEff(ICharacterBody body) =>
            (OrganEff(body, BodyPartIds.LungL) + OrganEff(body, BodyPartIds.LungR)) * 0.5f;

        public static float KidneyEff(ICharacterBody body) =>
            (OrganEff(body, BodyPartIds.KidneyL) + OrganEff(body, BodyPartIds.KidneyR)) * 0.5f;

        public static float BloodFactor(float blood01)
        {
            if (blood01 <= 0f)
                return 0f;
            if (blood01 >= 1f)
                return 1f;
            return blood01;
        }

        public static float ManipulationTickScale(ICharacterBody body)
        {
            float manip = Manipulation(body);
            return ManipTickMin + (1f - ManipTickMin) * Clamp01NonNeg(manip);
        }

        public static int DestroyedBleedIntensity(string organId)
        {
            if (organId == BodyPartIds.Heart)
                return BodyIllness.OrganDestroyedBleedHeart;
            if (organId == BodyPartIds.Liver)
                return BodyIllness.OrganDestroyedBleedLiver;
            if (organId == BodyPartIds.LungL || organId == BodyPartIds.LungR)
                return BodyIllness.OrganDestroyedBleedLung;
            if (organId == BodyPartIds.KidneyL || organId == BodyPartIds.KidneyR)
                return BodyIllness.OrganDestroyedBleedKidney;
            if (organId == BodyPartIds.Stomach)
                return BodyIllness.OrganDestroyedBleedStomach;
            return BodyIllness.OrganDestroyedBleedDefault;
        }

        static float LegSideEff(ICharacterBody body, string thighId, string footId)
        {
            if (!body.Has(thighId))
                return 0f;
            float thigh = PartConditionEff(body, thighId);
            if (!body.Has(footId))
                return thigh * MissingFootAsMove;
            return thigh * PartConditionEff(body, footId);
        }

        static float ArmSideEff(
            ICharacterBody body,
            string upperId,
            string lowerId,
            string handId)
        {
            if (!body.Has(upperId))
                return 0f;
            float upper = PartConditionEff(body, upperId);
            float lower = body.Has(lowerId) ? PartConditionEff(body, lowerId) : MissingHandAsManip;
            float hand = body.Has(handId) ? PartConditionEff(body, handId) : MissingHandAsManip;
            return upper * lower * hand;
        }

        static float PartConditionEff(ICharacterBody body, string partId)
        {
            int max = body.GetConditionMax(partId);
            if (max <= 0)
                return body.Has(partId) ? 1f : 0f;
            int cur = body.GetConditionCur(partId);
            if (cur <= 0)
                return 0f;
            return cur / (float)max;
        }

        static float PartPresentEff(ICharacterBody body, string partId)
        {
            if (!IsUsableMain(body, partId))
                return 0f;
            return PartConditionEff(body, partId);
        }

        static bool IsUsableMain(ICharacterBody body, string partId)
        {
            if (!body.TryGet(partId, out BodyPartNode node) || !node.HasCondition)
                return false;
            return node.ConditionCur > 0;
        }

        static float Clamp01NonNeg(float value)
        {
            if (value < 0f)
                return 0f;
            if (value > 1f)
                return 1f;
            return value;
        }
    }
}
