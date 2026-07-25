// ============================================================
// BodySkillModifierAggregator ??Î∂Ä???®Í≥º ?ÑÏàò ?©ÏÇ∞ ??Refresh ?åÏä§ 1Í∞?
// ============================================================

using System;
using System.Collections.Generic;

namespace Garunnir.Runtime.Gameplay.Data
{
    /// <summary>
    /// Î∂Ä?ÑÎßà???®Í≥ºÎ•?Î≥¥Ïú†?òÍ≥†, Elona??Î≤ÑÌîÑ/Refresh???£Í∏∞ ???†Ï≤¥?êÏÑú ??Î≤??©ÏÇ∞?úÎã§.
    /// ?ôÏùº effectId??intensity ?©ÏÇ∞ ??Ïπ¥ÌÉàÎ°úÍ∑∏ delta??Í≥±Ìïú??
    /// </summary>
    public sealed class BodySkillModifierAggregator : ISkillModifierSource
    {
        readonly ICharacterBody _body;
        readonly List<BodyPartEffect> _scratch = new(32);
        readonly Dictionary<string, int> _intensityByEffect = new(StringComparer.Ordinal);

        public BodySkillModifierAggregator(ICharacterBody body)
        {
            _body = body ?? throw new ArgumentNullException(nameof(body));
        }

        public void CollectModifiers(Dictionary<string, int> into)
        {
            if (into == null || _body == null)
                return;

            _scratch.Clear();
            _intensityByEffect.Clear();

            IReadOnlyList<BodyPartNode> roots = _body.Roots;
            for (int i = 0; i < roots.Count; i++)
            {
                BodyPartNode root = roots[i];
                if (root == null || string.IsNullOrEmpty(root.PartId))
                    continue;
                _body.CollectEffectsUnder(root.PartId, _scratch, includeDescendants: true);
            }

            for (int i = 0; i < _scratch.Count; i++)
            {
                BodyPartEffect effect = _scratch[i];
                if (string.IsNullOrEmpty(effect.EffectId))
                    continue;

                int intensity = Math.Max(1, effect.Intensity);
                if (_intensityByEffect.TryGetValue(effect.EffectId, out int sum))
                    _intensityByEffect[effect.EffectId] = sum + intensity;
                else
                    _intensityByEffect[effect.EffectId] = intensity;
            }

            foreach (KeyValuePair<string, int> pair in _intensityByEffect)
            {
                if (!BodyEffectSkillModifierCatalog.TryGetDeltas(pair.Key, out var deltas))
                    continue;

                for (int d = 0; d < deltas.Length; d++)
                {
                    (string skillId, int deltaPerIntensity) = deltas[d];
                    if (string.IsNullOrEmpty(skillId) || deltaPerIntensity == 0)
                        continue;

                    int add = deltaPerIntensity * pair.Value;
                    if (into.TryGetValue(skillId, out int current))
                        into[skillId] = current + add;
                    else
                        into[skillId] = add;
                }
            }
        }
    }
}
