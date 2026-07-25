// ============================================================
// BodyDamageService — 부위 Condition 타격·효과 부여
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

            return true;
        }
    }
}
