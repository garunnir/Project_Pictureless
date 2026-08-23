// ============================================================
// CharacterActionDelay — BodyPartEffect 트리 → 행동 TickScale
// ============================================================

using System.Collections.Generic;

namespace Garunnir.Runtime.Gameplay.Data
{
    public static class CharacterActionDelay
    {
        public static float TickScale(ICharacterBody body, List<BodyPartEffect> scratch)
        {
            if (body == null)
                return 1f;

            if (scratch == null)
                return TickScaleAllocating(body);

            scratch.Clear();
            IReadOnlyList<BodyPartNode> roots = body.Roots;
            for (int i = 0; i < roots.Count; i++)
                body.CollectEffectsUnder(roots[i].PartId, scratch, includeDescendants: true);

            float scale = 1f;
            for (int i = 0; i < scratch.Count; i++)
            {
                BodyPartEffect effect = scratch[i];
                if (!CharacterActionDelayCatalog.TryGetScalePerIntensity(
                        effect.EffectId,
                        out float perIntensity))
                {
                    continue;
                }

                int intensity = effect.Intensity < 1 ? 1 : effect.Intensity;
                for (int n = 0; n < intensity; n++)
                    scale *= perIntensity;
            }

            scale *= BodyCapacity.ManipulationTickScale(body);

            if (scale < CharacterActionDelayCatalog.MinTickScale)
                return CharacterActionDelayCatalog.MinTickScale;
            return scale;
        }

        static float TickScaleAllocating(ICharacterBody body)
        {
            var scratch = new List<BodyPartEffect>(16);
            return TickScale(body, scratch);
        }
    }
}
