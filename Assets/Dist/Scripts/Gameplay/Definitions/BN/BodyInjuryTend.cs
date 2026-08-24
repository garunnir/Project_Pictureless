// ============================================================
// BodyInjuryTend — 조직 부상 심각도 tend (HP는 SyncPart 파생)
// ============================================================

using System.Collections.Generic;

namespace Garunnir.Runtime.Gameplay.Data
{
    /// <summary>부상 intensity를 줄이면 PainTotal이 내려가고 PainHost가 기상을 푼다.</summary>
    /// <remarks>
    /// flowchart LR
    ///   Ticker[BodyEffectTicker World dt]
    ///   Tend[BodyInjuryTend]
    ///   Bleed[SyncBleedFromCut]
    ///   Inj[ReduceEffectIntensity tissue]
    ///   Sync[BodyInjury.SyncPart]
    ///   Pain[CharacterPainHost]
    ///   Ticker --> Tend --> Inj --> Bleed --> Sync --> Pain
    /// </remarks>
    public static class BodyInjuryTend
    {
        public sealed class Accum
        {
            public float Bruise;
            public float Cut;
            public float Gunshot;
            public float Fracture;
        }

        public static void Tick(
            ICharacterBody body,
            float deltaSeconds,
            Dictionary<string, Accum> healAccumByPart)
        {
            if (body == null || healAccumByPart == null || deltaSeconds <= 0f)
                return;
            if (body.IsDeadState)
                return;

            IReadOnlyList<BodyPartNode> roots = body.Roots;
            for (int i = 0; i < roots.Count; i++)
                TickNode(body, roots[i], deltaSeconds, healAccumByPart);
        }

        static void TickNode(
            ICharacterBody body,
            BodyPartNode node,
            float deltaSeconds,
            Dictionary<string, Accum> healAccumByPart)
        {
            if (node == null)
                return;

            if (node.Kind == BodyPartKind.Organic && node.HasCondition)
                TendPart(body, node, deltaSeconds, healAccumByPart);

            IReadOnlyList<BodyPartNode> children = node.Children;
            for (int i = 0; i < children.Count; i++)
                TickNode(body, children[i], deltaSeconds, healAccumByPart);
        }

        static void TendPart(
            ICharacterBody body,
            BodyPartNode node,
            float deltaSeconds,
            Dictionary<string, Accum> healAccumByPart)
        {
            BodyInjury.Reconcile(body, node.PartId);
            if (!body.TryGet(node.PartId, out node) || node == null)
                return;

            string partId = node.PartId;
            int tissue = BodyInjury.SumTissue(node);
            if (tissue <= 0)
            {
                healAccumByPart.Remove(partId);
                return;
            }

            if (!healAccumByPart.TryGetValue(partId, out Accum accum) || accum == null)
            {
                accum = new Accum();
                healAccumByPart[partId] = accum;
            }

            float tendMul = HasEffect(node, BodyPartEffectIds.Bandaged)
                ? BodyIllness.BandageTendMul
                : 1f;

            bool changed = false;
            changed |= Spend(body, partId, node, BodyPartEffectIds.Bruise, deltaSeconds, tendMul, ref accum.Bruise);
            changed |= Spend(body, partId, node, BodyPartEffectIds.Cut, deltaSeconds, tendMul, ref accum.Cut);
            changed |= Spend(body, partId, node, BodyPartEffectIds.Gunshot, deltaSeconds, tendMul, ref accum.Gunshot);
            changed |= Spend(body, partId, node, BodyPartEffectIds.Fracture, deltaSeconds, tendMul, ref accum.Fracture);

            if (changed)
                BodyInjury.SyncPart(body, partId);

            if (!body.TryGet(partId, out node) || node == null || BodyInjury.SumTissue(node) <= 0)
                healAccumByPart.Remove(partId);
        }

        static bool HasEffect(BodyPartNode node, string effectId)
        {
            IReadOnlyList<BodyPartEffect> effects = node.Effects;
            for (int i = 0; i < effects.Count; i++)
            {
                if (effects[i].EffectId == effectId)
                    return true;
            }

            return false;
        }

        static bool Spend(
            ICharacterBody body,
            string partId,
            BodyPartNode node,
            string effectId,
            float deltaSeconds,
            float tendMul,
            ref float accum)
        {
            int have = 0;
            IReadOnlyList<BodyPartEffect> effects = node.Effects;
            for (int i = 0; i < effects.Count; i++)
            {
                if (effects[i].EffectId != effectId)
                    continue;
                have = effects[i].Intensity;
                break;
            }

            if (have < 1)
            {
                accum = 0f;
                return false;
            }

            float seconds = BodyInjury.TendSecondsPerHp(effectId);
            if (seconds <= 0f)
                return false;

            float mul = tendMul > 0f ? tendMul : 1f;
            accum += deltaSeconds * mul / seconds;
            int points = (int)accum;
            accum -= points;
            if (points < 1)
                return false;

            if (points > have)
                points = have;
            bool reduced = body.ReduceEffectIntensity(partId, effectId, points);
            if (reduced && effectId == BodyPartEffectIds.Cut)
                BodyInjury.SyncBleedFromCut(body, partId, have);
            return reduced;
        }
    }
}
