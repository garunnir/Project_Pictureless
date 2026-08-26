// ============================================================
// BodyDamageService — 부위 Condition 타격·오버킬 절단·장기 파괴 출혈
// ============================================================

using UnityEngine;

namespace Garunnir.Runtime.Gameplay.Data
{
    /// <summary>ApplyHit 1채널 결과. 연출은 조직 부상·절단을 이 값으로 구분한다.</summary>
    public readonly struct BodyHitApplyResult
    {
        public static readonly BodyHitApplyResult None = default;

        public readonly bool Applied;
        public readonly bool Severed;
        public readonly string TissueId;
        public readonly int HpLost;

        public BodyHitApplyResult(bool applied, bool severed, string tissueId, int hpLost)
        {
            Applied = applied;
            Severed = severed;
            TissueId = tissueId ?? string.Empty;
            HpLost = hpLost < 0 ? 0 : hpLost;
        }

        public static BodyHitApplyResult Hit(string tissueId, int hpLost) =>
            new BodyHitApplyResult(true, false, tissueId, hpLost);

        public static BodyHitApplyResult Sever(int hpLost) =>
            new BodyHitApplyResult(true, true, string.Empty, hpLost);
    }

    public static class BodyDamageService
    {
        public static BodyHitApplyResult ApplyHit(
            ICharacterBody body,
            string partId,
            int amount,
            BodyPartEffect[] effectSeeds = null,
            string hitTag = null)
        {
            if (body == null || amount <= 0)
                return BodyHitApplyResult.None;

            string main = BodyPartIds.GetMainConditionPart(partId) ?? partId;
            if (string.IsNullOrEmpty(main) || !body.Has(main))
                return BodyHitApplyResult.None;

            int cur = body.GetConditionCur(main);
            int max = body.GetConditionMax(main);
            if (max <= 0)
                return BodyHitApplyResult.None;

            bool organic = body.TryGet(main, out BodyPartNode part) &&
                           part != null &&
                           part.Kind == BodyPartKind.Organic;
            if (organic)
                BodyInjury.Reconcile(body, main);

            cur = body.GetConditionCur(main);

            int next = cur - amount;
            if (next < 0)
                next = 0;

            if (next == 0 &&
                BodyPartIds.IsSeverable(main) &&
                !BodySeverOverkill.RollDestroy(cur, max, amount, hitTag))
            {
                next = 1;
            }

            if (next == 0 && BodyPartIds.IsSeverable(main))
            {
                body.RemovePart(main);
                ApplySeverStumpBleed(body, main);
                return BodyHitApplyResult.Sever(cur);
            }

            int hpLost = cur - next;
            string tissueId = string.Empty;
            if (organic)
            {
                if (hpLost > 0)
                    tissueId = BodyInjury.IdForHitTag(hitTag);
                BodyInjury.AddTissue(body, main, BodyInjury.IdForHitTag(hitTag), hpLost);
                ApplySeeds(body, main, effectSeeds);
                BodyInjury.SyncPart(body, main);
            }
            else
            {
                body.SetCondition(main, next, max);
                ApplySeeds(body, main, effectSeeds);
            }

            if (body.GetConditionCur(main) == 0)
                ApplyDestroyedBleed(body, main);

            return BodyHitApplyResult.Hit(tissueId, hpLost);
        }

        /// <summary>절단된 부위는 트리에서 사라지므로, 남는 소켓에 Bleed.</summary>
        public static void ApplySeverStumpBleed(ICharacterBody body, string severedPartId)
        {
            if (body == null || string.IsNullOrEmpty(severedPartId))
                return;

            string id = BodyPartIds.ResolveNodeId(severedPartId);
            string stump = BodyPartIds.GetSocketParentId(id) ?? BodyPartIds.Chest;
            EnsureBleed(body, stump, SeverStumpBleedIntensity(id));
        }

        static int SeverStumpBleedIntensity(string severedId)
        {
            if (BodyPartIds.IsFinger(severedId))
                return BodyIllness.SeverStumpBleedFinger;
            if (severedId == BodyPartIds.HandL || severedId == BodyPartIds.HandR ||
                severedId == BodyPartIds.FootL || severedId == BodyPartIds.FootR)
                return BodyIllness.SeverStumpBleedHandFoot;
            if (severedId == BodyPartIds.LowerArmL || severedId == BodyPartIds.LowerArmR ||
                severedId == BodyPartIds.CalfL || severedId == BodyPartIds.CalfR)
                return BodyIllness.SeverStumpBleedMidLimb;
            return BodyIllness.SeverStumpBleedRootLimb;
        }

        static void ApplySeeds(ICharacterBody body, string partId, BodyPartEffect[] effectSeeds)
        {
            if (effectSeeds == null)
                return;

            for (int i = 0; i < effectSeeds.Length; i++)
            {
                BodyPartEffect seed = effectSeeds[i];
                if (string.IsNullOrEmpty(seed.EffectId))
                    continue;
                if (BodyInjury.IsTissue(seed.EffectId) &&
                    BodyInjury.IsOrganicCondition(body, partId, out _))
                    BodyInjury.AddTissue(body, partId, seed.EffectId, seed.Intensity);
                else
                    body.AddEffect(partId, seed);
            }
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
                EnsureOrganBleed(body, id, BodyCapacity.DestroyedBleedIntensity(id));
                return;
            }

            if (id == BodyPartIds.Chest)
            {
                EnsureOrganBleed(body, BodyPartIds.Heart, BodyIllness.OrganDestroyedBleedHeart);
                EnsureOrganBleed(body, BodyPartIds.LungL, BodyIllness.OrganDestroyedBleedLung);
                EnsureOrganBleed(body, BodyPartIds.LungR, BodyIllness.OrganDestroyedBleedLung);
                ApplyDestroyedBleed(body, BodyPartIds.Belly);
                return;
            }

            if (id == BodyPartIds.Belly)
            {
                EnsureOrganBleed(body, BodyPartIds.Liver, BodyIllness.OrganDestroyedBleedLiver);
                EnsureOrganBleed(body, BodyPartIds.Stomach, BodyIllness.OrganDestroyedBleedStomach);
                EnsureOrganBleed(body, BodyPartIds.KidneyL, BodyIllness.OrganDestroyedBleedKidney);
                EnsureOrganBleed(body, BodyPartIds.KidneyR, BodyIllness.OrganDestroyedBleedKidney);
                return;
            }

            if (id == BodyPartIds.Neck || id == BodyPartIds.Head)
                EnsureBleed(body, id, BodyIllness.OrganDestroyedBleedDefault);
        }

        static void EnsureOrganBleed(ICharacterBody body, string partId, int intensity)
        {
            if (!body.Has(partId))
                return;
            body.EnsureEffectMinIntensity(
                partId,
                BodyPartEffectIds.OrganBleed,
                intensity,
                remainingSeconds: -1f);
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

    /// <summary>
    /// 초과분/최대HP = 오버킬%. 타입 구간에 inverse lerp → 파괴 확률.
    /// 실패 시 1 HP. 머리/가슴 즉사 없음.
    /// </summary>
    public static class BodySeverOverkill
    {
        public const float CutMin = 0f;
        public const float CutMax = 0.1f;
        public const float BulletMin = 0f;
        public const float BulletMax = 0.7f;
        public const float BashMin = 0.4f;
        public const float BashMax = 1f;

        public static bool RollDestroy(int cur, int max, int amount, string hitTag)
        {
            if (cur <= 0 || max <= 0 || amount < cur)
                return false;

            float overkillPct = (amount - cur) / (float)max;
            ResolveRange(hitTag, out float lo, out float hi);
            float span = hi - lo;
            float t = span <= 0f ? (overkillPct >= lo ? 1f : 0f) : (overkillPct - lo) / span;
            if (t <= 0f)
                return false;
            if (t >= 1f)
                return true;
            return Random.value < t;
        }

        // AttackDamageTags 키와 동일. Dist.Gameplay.Data는 Combat asm을 참조하지 않음.
        public const string TagBash = "bash";
        public const string TagCut = "cut";
        public const string TagBullet = "bullet";

        static void ResolveRange(string hitTag, out float lo, out float hi)
        {
            if (string.Equals(hitTag, TagCut, System.StringComparison.Ordinal))
            {
                lo = CutMin;
                hi = CutMax;
                return;
            }

            if (string.Equals(hitTag, TagBullet, System.StringComparison.Ordinal))
            {
                lo = BulletMin;
                hi = BulletMax;
                return;
            }

            lo = BashMin;
            hi = BashMax;
        }
    }
}
