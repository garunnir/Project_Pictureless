// ============================================================
// BodyInjury — 조직 부상 SSOT (남은 HP = max − 부상 합)
// ============================================================

using System.Collections.Generic;

namespace Garunnir.Runtime.Gameplay.Data
{
    /// <summary>
    /// 림월드 Hediff_Injury: 심각도(HP 점수)가 본체. ConditionCur는 파생.
    /// </summary>
    /// <remarks>
    /// flowchart LR
    ///   Hit[BodyDamageService.ApplyHit]
    ///   Inj[AddTissue bruise/cut/gunshot]
    ///   Bleed[SyncBleedFromCut]
    ///   Sync[SyncPart]
    ///   Tend[BodyInjuryTend]
    ///   Pain[BodyPain]
    ///   Hit --> Inj --> Bleed --> Sync
    ///   Tend --> Inj
    ///   Sync --> Pain
    /// </remarks>
    public static class BodyInjury
    {
        public static bool IsTissue(string effectId)
        {
            if (string.IsNullOrEmpty(effectId))
                return false;
            return effectId == BodyPartEffectIds.Bruise ||
                   effectId == BodyPartEffectIds.Cut ||
                   effectId == BodyPartEffectIds.Gunshot ||
                   effectId == BodyPartEffectIds.Fracture;
        }

        public static string IdForHitTag(string hitTag)
        {
            if (string.Equals(hitTag, BodySeverOverkill.TagCut, System.StringComparison.Ordinal))
                return BodyPartEffectIds.Cut;
            if (string.Equals(hitTag, BodySeverOverkill.TagBullet, System.StringComparison.Ordinal))
                return BodyPartEffectIds.Gunshot;
            return BodyPartEffectIds.Bruise;
        }

        /// <summary>한 히트에 여러 채널이면 더 눈에 띄는 조직 부상을 남긴다 (cut &gt; gunshot &gt; bruise).</summary>
        public static int OverlayRank(string tissueId)
        {
            if (tissueId == BodyPartEffectIds.Cut)
                return 3;
            if (tissueId == BodyPartEffectIds.Gunshot)
                return 2;
            if (tissueId == BodyPartEffectIds.Bruise)
                return 1;
            return 0;
        }

        public static float PainPerIntensity(string effectId)
        {
            if (effectId == BodyPartEffectIds.Fracture)
                return BodyPain.FracturePainPerIntensity;
            if (IsTissue(effectId))
                return BodyPain.InjuryPainPerHp;
            return 0f;
        }

        public static float TendSecondsPerHp(string effectId)
        {
            if (effectId == BodyPartEffectIds.Cut)
                return BodyIllness.CutTendSecondsPerHp;
            if (effectId == BodyPartEffectIds.Gunshot)
                return BodyIllness.GunshotTendSecondsPerHp;
            if (effectId == BodyPartEffectIds.Fracture)
                return BodyIllness.FractureTendSecondsPerHp;
            return BodyIllness.InjuryHealSecondsPerHp;
        }

        public static int SumTissue(BodyPartNode node)
        {
            if (node == null)
                return 0;

            int sum = 0;
            IReadOnlyList<BodyPartEffect> effects = node.Effects;
            for (int i = 0; i < effects.Count; i++)
            {
                if (!IsTissue(effects[i].EffectId))
                    continue;
                int intensity = effects[i].Intensity;
                sum += intensity < 0 ? 0 : intensity;
            }

            return sum;
        }

        public static bool IsOrganicCondition(ICharacterBody body, string partId, out BodyPartNode node)
        {
            node = null;
            if (body == null || string.IsNullOrEmpty(partId) || !body.TryGet(partId, out node) || node == null)
                return false;
            return node.Kind == BodyPartKind.Organic && node.HasCondition && node.ConditionMax > 0;
        }

        public static void Reconcile(ICharacterBody body, string partId) =>
            SyncPart(body, partId);

        public static void SyncPart(ICharacterBody body, string partId)
        {
            if (!IsOrganicCondition(body, partId, out BodyPartNode node))
                return;

            int max = node.ConditionMax;
            int sum = SumTissue(node);
            if (sum > max)
                sum = max;
            body.SetCondition(partId, max - sum, max);
        }

        public static void AddTissue(ICharacterBody body, string partId, string effectId, int amount)
        {
            if (body == null || amount <= 0 || !IsTissue(effectId))
                return;
            if (!IsOrganicCondition(body, partId, out BodyPartNode node))
                return;

            int max = node.ConditionMax;
            int sum = SumTissue(node);
            int room = max - sum;
            if (room <= 0)
                return;

            int add = amount < room ? amount : room;
            int previousCut = effectId == BodyPartEffectIds.Cut
                ? CurrentIntensity(node, BodyPartEffectIds.Cut)
                : 0;
            int next = CurrentIntensity(node, effectId) + add;
            body.EnsureEffectMinIntensity(partId, effectId, next, -1f);
            if (effectId == BodyPartEffectIds.Cut)
            {
                ClearHemostatic(body, partId);
                SyncBleedFromCut(body, partId, previousCut);
            }
        }

        static void ClearHemostatic(ICharacterBody body, string partId)
        {
            if (body == null || string.IsNullOrEmpty(partId))
                return;
            if (!body.TryGet(partId, out BodyPartNode node) || node == null)
                return;

            int intensity = CurrentIntensity(node, BodyPartEffectIds.Hemostatic);
            if (intensity < 1)
                return;
            body.ReduceEffectIntensity(partId, BodyPartEffectIds.Hemostatic, intensity);
        }

        public static bool ReduceTissue(ICharacterBody body, string partId, int points)
        {
            if (body == null || points <= 0 || !IsOrganicCondition(body, partId, out BodyPartNode node))
                return false;

            int left = points;
            left -= ReduceOne(body, partId, node, BodyPartEffectIds.Bruise, left);
            left -= ReduceOne(body, partId, node, BodyPartEffectIds.Cut, left);
            left -= ReduceOne(body, partId, node, BodyPartEffectIds.Gunshot, left);
            left -= ReduceOne(body, partId, node, BodyPartEffectIds.Fracture, left);
            return left < points;
        }

        public static void SetCur(ICharacterBody body, string partId, int newCur)
        {
            if (body == null || string.IsNullOrEmpty(partId) || !body.TryGet(partId, out BodyPartNode node))
                return;

            int max = node.ConditionMax;
            if (max <= 0)
                return;

            int clamped = newCur < 0 ? 0 : (newCur > max ? max : newCur);
            if (node.Kind != BodyPartKind.Organic)
            {
                body.SetCondition(partId, clamped, max);
                return;
            }

            Reconcile(body, partId);
            int cur = body.GetConditionCur(partId);
            if (clamped > cur)
                ReduceTissue(body, partId, clamped - cur);
            else if (clamped < cur)
                AddTissue(body, partId, BodyPartEffectIds.Bruise, cur - clamped);

            SyncPart(body, partId);
        }

        static int ReduceOne(
            ICharacterBody body,
            string partId,
            BodyPartNode node,
            string effectId,
            int points)
        {
            if (points <= 0)
                return 0;

            int have = CurrentIntensity(node, effectId);
            if (have <= 0)
                return 0;

            int take = have < points ? have : points;
            body.ReduceEffectIntensity(partId, effectId, take);
            if (effectId == BodyPartEffectIds.Cut)
                SyncBleedFromCut(body, partId, have);
            return take;
        }

        /// <summary>
        /// 베임 변화 후 파생 Bleed를 맞춘다. 줄어든 기여분만 제거하고,
        /// 남은 베임이 있으면 영구 Bleed를 바닥값으로 유지한다.
        /// </summary>
        public static void SyncBleedFromCut(ICharacterBody body, string partId, int previousCutHp)
        {
            if (!IsOrganicCondition(body, partId, out BodyPartNode node))
                return;

            int cut = CurrentIntensity(node, BodyPartEffectIds.Cut);
            int oldDerived = BodyIllness.BleedIntensityForCut(previousCutHp);
            int newDerived = BodyIllness.BleedIntensityForCut(cut);
            int drop = oldDerived - newDerived;
            if (drop > 0)
            {
                int currentBleed = CurrentIntensity(node, BodyPartEffectIds.Bleed);
                if (currentBleed <= oldDerived)
                    body.ReduceEffectIntensity(partId, BodyPartEffectIds.Bleed, drop);
            }
            EnsureBleedFromOpenCut(body, partId);
        }

        /// <summary>베임이 남은 부위에 파생 Bleed가 없으면(또는 유한 만료면) 영구로 채운다.</summary>
        public static void EnsureBleedFromOpenCut(ICharacterBody body, string partId)
        {
            if (!IsOrganicCondition(body, partId, out BodyPartNode node))
                return;

            if (CurrentIntensity(node, BodyPartEffectIds.Hemostatic) > 0)
                return;

            int derived = BodyIllness.BleedIntensityForCut(
                CurrentIntensity(node, BodyPartEffectIds.Cut));
            if (derived <= 0)
                return;

            body.EnsureEffectMinIntensity(
                partId,
                BodyPartEffectIds.Bleed,
                derived,
                remainingSeconds: -1f);
        }

        static int CurrentIntensity(BodyPartNode node, string effectId)
        {
            IReadOnlyList<BodyPartEffect> effects = node.Effects;
            for (int i = 0; i < effects.Count; i++)
            {
                if (effects[i].EffectId == effectId)
                    return effects[i].Intensity;
            }

            return 0;
        }
    }
}
