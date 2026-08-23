// ============================================================
// BodyDamageService — 부위 Condition 타격·효과 부여·장기 파괴 출혈
// ============================================================

namespace Garunnir.Runtime.Gameplay.Data
{
    public static class BodyDamageService
    {
        public static bool ApplyHit(
            ICharacterBody body,
            string partId,
            int amount,
            BodyPartEffect[] effectSeeds = null)
        {
            if (body == null || amount <= 0)
                return false;

            string main = BodyPartIds.GetMainConditionPart(partId) ?? partId;
            if (string.IsNullOrEmpty(main) || !body.Has(main))
                return false;

            int cur = body.GetConditionCur(main);
            int max = body.GetConditionMax(main);
            if (max <= 0)
                return false;

            int next = cur - amount;
            if (next < 0)
                next = 0;
            body.SetCondition(main, next, max);

            if (effectSeeds != null)
            {
                for (int i = 0; i < effectSeeds.Length; i++)
                {
                    BodyPartEffect seed = effectSeeds[i];
                    if (string.IsNullOrEmpty(seed.EffectId))
                        continue;
                    body.AddEffect(main, seed);
                }
            }

            if (next == 0)
            {
                if (BodyPartIds.IsSeverable(main))
                    body.RemovePart(main);
                else
                    ApplyDestroyedBleed(body, main);
            }

            return true;
        }

        /// <summary>뇌 제외 장기/몸통 HP0 → 강한 Bleed. 즉사 아님.</summary>
        public static void ApplyDestroyedBleed(ICharacterBody body, string destroyedPartId)
        {
            if (body == null || string.IsNullOrEmpty(destroyedPartId))
                return;

            string id = BodyPartIds.ResolveNodeId(destroyedPartId);
            if (id == BodyPartIds.Brain)
                return;

            if (BodyPartIds.IsVitalOrgan(id))
            {
                EnsureBleed(body, id, BodyCapacity.DestroyedBleedIntensity(id));
                return;
            }

            if (id == BodyPartIds.Chest)
            {
                EnsureBleed(body, BodyPartIds.Heart, BodyIllness.OrganDestroyedBleedHeart);
                EnsureBleed(body, BodyPartIds.LungL, BodyIllness.OrganDestroyedBleedLung);
                EnsureBleed(body, BodyPartIds.LungR, BodyIllness.OrganDestroyedBleedLung);
                ApplyDestroyedBleed(body, BodyPartIds.Belly);
                return;
            }

            if (id == BodyPartIds.Belly)
            {
                EnsureBleed(body, BodyPartIds.Liver, BodyIllness.OrganDestroyedBleedLiver);
                EnsureBleed(body, BodyPartIds.Stomach, BodyIllness.OrganDestroyedBleedStomach);
                EnsureBleed(body, BodyPartIds.KidneyL, BodyIllness.OrganDestroyedBleedKidney);
                EnsureBleed(body, BodyPartIds.KidneyR, BodyIllness.OrganDestroyedBleedKidney);
                return;
            }

            if (id == BodyPartIds.Neck || id == BodyPartIds.Head)
                EnsureBleed(body, id, BodyIllness.OrganDestroyedBleedDefault);
        }

        static void EnsureBleed(ICharacterBody body, string partId, int intensity)
        {
            if (!body.Has(partId))
                return;
            body.EnsureEffectMinIntensity(
                partId,
                BodyPartEffectIds.Bleed,
                intensity,
                remainingSeconds: -1f);
        }
    }
}
