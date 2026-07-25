// ============================================================
// BodySkillModifierAggregator — 부위 효과 전수 합산 → Refresh 소스 1개
// ============================================================

using System;
using System.Collections.Generic;

namespace Garunnir.Runtime.Gameplay.Data
{
    /// <summary>
    /// 부위마다 효과를 보유하고, Elona식 버프/Refresh에 넣기 전 신체에서 한 번 합산한다.
    /// 동일 effectId는 intensity 합산 후 카탈로그 delta에 곱한다.
    /// </summary>
    public sealed class BodySkillModifierAggregator : ISkillModifierSource
    {
        readonly IPlayerBody _body;
        readonly List<BodyPartEffect> _scratch = new(32);
        readonly Dictionary<string, int> _intensityByEffect = new(StringComparer.Ordinal);

        public BodySkillModifierAggregator(IPlayerBody body)
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
