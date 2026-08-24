// ============================================================
// BodyHealApply — heal use_action: 지정 부위 즉시 HP + 붕대/지혈
// ============================================================

using System.Collections.Generic;

namespace Garunnir.Runtime.Gameplay.Data
{
    /// <summary>
    /// BN heal_actor Dist 소비. 부위는 호출부가 고른다.
    /// <see cref="UseActionData.bandages_power"/>는 HP가 아니다.
    /// </summary>
    /// <remarks>
    /// flowchart LR
    ///   Use[heal use_action + partId] --> Instant[ResolveInstantHeal]
    ///   Use --> Wrap[bandages_power]
    ///   Use --> Hemo[bleed only]
    ///   Wrap --> Fx[bandaged permanent]
    ///   Hemo --> ClearBleed[remove bleed + hemostatic]
    /// </remarks>
    public static class BodyHealApply
    {
        public static HealRegion RegionOf(string partId)
        {
            string id = BodyPartIds.GetMainConditionPart(partId) ?? BodyPartIds.ResolveNodeId(partId);
            if (id == BodyPartIds.Head ||
                id == BodyPartIds.Eyes ||
                id == BodyPartIds.Mouth ||
                id == BodyPartIds.Brain)
                return HealRegion.Head;

            if (id == BodyPartIds.Neck ||
                id == BodyPartIds.Chest ||
                id == BodyPartIds.Belly ||
                id == BodyPartIds.Pelvis ||
                id == BodyPartIds.Torso ||
                id == BodyPartIds.Heart ||
                id == BodyPartIds.LungL ||
                id == BodyPartIds.LungR ||
                id == BodyPartIds.Liver ||
                id == BodyPartIds.Stomach ||
                id == BodyPartIds.KidneyL ||
                id == BodyPartIds.KidneyR)
                return HealRegion.Torso;

            return HealRegion.Limb;
        }

        /// <summary>후보 부위가 하나라도 있으면 true. 실행은 <see cref="TryApply"/>.</summary>
        public static bool CanApply(ICharacterBody body, UseActionData action) =>
            TryCollectEligibleParts(body, action, null);

        public static bool CanApplyTo(ICharacterBody body, UseActionData action, string partId)
        {
            if (body == null || action == null || string.IsNullOrEmpty(partId))
                return false;
            if (!body.TryGet(partId, out BodyPartNode node) || node == null)
                return false;

            int instant = action.ResolveInstantHeal(RegionOf(partId));
            if (instant > 0 && BodyPartRestoreService.CanHeal(body, partId, instant))
                return true;

            if (action.IsBandage)
                return CanBandage(node);

            if (action.IsHemostatic)
                return CanHemostatic(node);

            return false;
        }

        public static bool TryApply(ICharacterBody body, UseActionData action, string partId)
        {
            if (!CanApplyTo(body, action, partId))
                return false;

            int instant = action.ResolveInstantHeal(RegionOf(partId));
            bool healed = instant > 0 && BodyPartRestoreService.TryHeal(body, partId, instant);
            bool wrapped = action.IsBandage && TryApplyBandage(body, partId, action);
            bool hemo = !action.IsBandage && action.IsHemostatic && TryApplyHemostatic(body, partId);
            return healed || wrapped || hemo;
        }

        /// <summary>메뉴 활성용. 자동 픽으로 실행하지 않는다.</summary>
        public static bool TryCollectEligibleParts(
            ICharacterBody body,
            UseActionData action,
            List<string> dest)
        {
            dest?.Clear();
            if (body == null || action == null)
                return false;

            string[] mains = BodyPartIds.MainConditionParts;
            bool any = false;
            for (int i = 0; i < mains.Length; i++)
            {
                string id = mains[i];
                if (!CanApplyTo(body, action, id))
                    continue;

                any = true;
                dest?.Add(id);
            }

            return any;
        }

        public static bool TryUnwrap(ICharacterBody body, string partId)
        {
            if (body == null || string.IsNullOrEmpty(partId))
                return false;
            if (!body.TryGet(partId, out BodyPartNode node) || node == null)
                return false;
            if (HasEffect(node, BodyPartEffectIds.Bandaged))
            {
                RemoveEffectFully(body, partId, node, BodyPartEffectIds.Bandaged);
                if (body.TryGet(partId, out node) && node != null)
                    RemoveEffectFully(body, partId, node, BodyPartEffectIds.BandageDirty);
                return true;
            }

            return TryUnwrapDescendant(body, node);
        }

        static bool TryUnwrapDescendant(ICharacterBody body, BodyPartNode node)
        {
            IReadOnlyList<BodyPartNode> children = node.Children;
            for (int i = 0; i < children.Count; i++)
            {
                BodyPartNode child = children[i];
                if (child == null)
                    continue;
                if (TryUnwrap(body, child.PartId))
                    return true;
            }

            return false;
        }

        public static bool HasBandagedUnder(ICharacterBody body, string partId, List<BodyPartEffect> scratch)
        {
            if (body == null || string.IsNullOrEmpty(partId) || scratch == null)
                return false;

            scratch.Clear();
            body.CollectEffectsUnder(partId, scratch, includeDescendants: true);
            for (int i = 0; i < scratch.Count; i++)
            {
                if (scratch[i].EffectId == BodyPartEffectIds.Bandaged)
                    return true;
            }

            return false;
        }

        public static float BandageDirty01(BodyPartNode node)
        {
            if (node == null)
                return 0f;

            int dirty = EffectIntensity(node, BodyPartEffectIds.BandageDirty);
            if (dirty < 1)
                return 0f;

            float max = BodyIllness.BandageDirtyMax;
            if (max <= 0f)
                return 0f;
            float ratio = dirty / max;
            return ratio > 1f ? 1f : ratio;
        }

        public static float BandageDirty01Under(
            ICharacterBody body,
            string partId,
            List<BodyPartEffect> scratch)
        {
            if (body == null || string.IsNullOrEmpty(partId) || scratch == null)
                return 0f;

            scratch.Clear();
            body.CollectEffectsUnder(partId, scratch, includeDescendants: true);
            int dirty = 0;
            for (int i = 0; i < scratch.Count; i++)
            {
                if (scratch[i].EffectId != BodyPartEffectIds.BandageDirty)
                    continue;
                int intensity = scratch[i].Intensity;
                if (intensity > dirty)
                    dirty = intensity;
            }

            if (dirty < 1)
                return 0f;

            float max = BodyIllness.BandageDirtyMax;
            if (max <= 0f)
                return 0f;
            float ratio = dirty / max;
            return ratio > 1f ? 1f : ratio;
        }

        static bool CanBandage(BodyPartNode node)
        {
            if (node.Kind != BodyPartKind.Organic || BodyInjury.SumTissue(node) <= 0)
                return false;
            return !HasEffect(node, BodyPartEffectIds.Bandaged);
        }

        static bool CanHemostatic(BodyPartNode node)
        {
            if (HasEffect(node, BodyPartEffectIds.Hemostatic))
                return false;
            return EffectIntensity(node, BodyPartEffectIds.Bleed) > 0;
        }

        static bool TryApplyBandage(ICharacterBody body, string partId, UseActionData action)
        {
            if (!body.TryGet(partId, out BodyPartNode node) || node == null)
                return false;
            if (!CanBandage(node))
                return false;

            int intensity = action.bandages_power;
            if (intensity < 1)
                return false;
            if (intensity > BodyIllness.BandageMaxIntensity)
                intensity = BodyIllness.BandageMaxIntensity;

            return body.EnsureEffectMinIntensity(
                partId,
                BodyPartEffectIds.Bandaged,
                intensity,
                remainingSeconds: -1f);
        }

        static bool TryApplyHemostatic(ICharacterBody body, string partId)
        {
            if (!body.TryGet(partId, out BodyPartNode node) || node == null)
                return false;
            if (!CanHemostatic(node))
                return false;

            RemoveEffectFully(body, partId, node, BodyPartEffectIds.Bleed);
            return body.EnsureEffectMinIntensity(
                partId,
                BodyPartEffectIds.Hemostatic,
                1,
                remainingSeconds: -1f);
        }

        static void RemoveEffectFully(
            ICharacterBody body,
            string partId,
            BodyPartNode node,
            string effectId)
        {
            int intensity = EffectIntensity(node, effectId);
            if (intensity < 1)
                return;
            body.ReduceEffectIntensity(partId, effectId, intensity);
        }

        static int EffectIntensity(BodyPartNode node, string effectId)
        {
            IReadOnlyList<BodyPartEffect> effects = node.Effects;
            for (int i = 0; i < effects.Count; i++)
            {
                if (effects[i].EffectId == effectId)
                    return effects[i].Intensity;
            }

            return 0;
        }

        static bool HasEffect(BodyPartNode node, string effectId)
        {
            return EffectIntensity(node, effectId) > 0;
        }
    }
}
