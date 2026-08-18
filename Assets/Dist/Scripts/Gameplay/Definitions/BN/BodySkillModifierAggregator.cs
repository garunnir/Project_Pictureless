// ============================================================
// BodySkillModifierAggregator ? ?? ?? ? ?? Buffed delta ??
// ============================================================

using System.Collections.Generic;

namespace Garunnir.Runtime.Gameplay.Data
{
    public sealed class BodySkillModifierAggregator : ISkillModifierSource
    {
        readonly ICharacterBody _body;
        readonly List<BodyPartEffect> _scratch = new(16);

        public BodySkillModifierAggregator(ICharacterBody body)
        {
            _body = body;
        }

        public void CollectModifiers(Dictionary<string, int> into)
        {
            if (_body == null || into == null)
                return;

            _scratch.Clear();
            IReadOnlyList<BodyPartNode> roots = _body.Roots;
            for (int i = 0; i < roots.Count; i++)
                _body.CollectEffectsUnder(roots[i].PartId, _scratch, includeDescendants: true);

            for (int i = 0; i < _scratch.Count; i++)
            {
                BodyPartEffect effect = _scratch[i];
                if (!BodyEffectSkillModifierCatalog.TryGetDeltas(
                        effect.EffectId,
                        out (string skillId, int deltaPerIntensity)[] deltas))
                {
                    continue;
                }

                int intensity = effect.Intensity;
                for (int j = 0; j < deltas.Length; j++)
                {
                    (string skillId, int deltaPerIntensity) entry = deltas[j];
                    if (string.IsNullOrEmpty(entry.skillId) || entry.deltaPerIntensity == 0)
                        continue;

                    int delta = entry.deltaPerIntensity * intensity;
                    if (into.TryGetValue(entry.skillId, out int existing))
                        into[entry.skillId] = existing + delta;
                    else
                        into[entry.skillId] = delta;
                }
            }
        }
    }
}
