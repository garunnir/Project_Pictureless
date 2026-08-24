// ============================================================
// BodySkillModifierAggregator — 부위 효과 → 숙련 Buffed delta 합산
// ============================================================

using System;
using System.Collections.Generic;

namespace Garunnir.Runtime.Gameplay.Data
{
    public sealed class BodySkillModifierAggregator : ISkillModifierSource
    {
        readonly ICharacterBody _body;
        readonly ICharacterSkills _skills;
        readonly List<BodyPartEffect> _scratch = new(16);
        readonly Dictionary<string, int> _percentScratch = new(8);

        public BodySkillModifierAggregator(ICharacterBody body, ICharacterSkills skills)
        {
            _body = body;
            _skills = skills;
        }

        public void CollectModifiers(Dictionary<string, int> into)
        {
            if (_body == null || into == null)
                return;

            _scratch.Clear();
            _percentScratch.Clear();
            IReadOnlyList<BodyPartNode> roots = _body.Roots;
            for (int i = 0; i < roots.Count; i++)
                _body.CollectEffectsUnder(roots[i].PartId, _scratch, includeDescendants: true);

            for (int i = 0; i < _scratch.Count; i++)
            {
                BodyPartEffect effect = _scratch[i];
                if (!BodyEffectSkillModifierCatalog.TryGetDeltas(effect.EffectId, out BodySkillMod[] deltas))
                    continue;

                int intensity = effect.Intensity;
                for (int j = 0; j < deltas.Length; j++)
                {
                    BodySkillMod entry = deltas[j];
                    if (string.IsNullOrEmpty(entry.SkillId) || entry.ValuePerIntensity == 0)
                        continue;

                    int stacked = entry.ValuePerIntensity * intensity;
                    if (entry.PercentOfBase)
                    {
                        if (_percentScratch.TryGetValue(entry.SkillId, out int existingPct))
                            _percentScratch[entry.SkillId] = existingPct + stacked;
                        else
                            _percentScratch[entry.SkillId] = stacked;
                    }
                    else
                    {
                        AddDelta(into, entry.SkillId, stacked);
                    }
                }
            }

            if (_skills == null)
                return;

            foreach (KeyValuePair<string, int> pair in _percentScratch)
            {
                int baseLevel = _skills.BaseLevel(pair.Key);
                int delta = (int)Math.Round(baseLevel * (pair.Value / 100.0), MidpointRounding.AwayFromZero);
                AddDelta(into, pair.Key, delta);
            }
        }

        static void AddDelta(Dictionary<string, int> into, string skillId, int delta)
        {
            if (delta == 0)
                return;

            if (into.TryGetValue(skillId, out int existing))
                into[skillId] = existing + delta;
            else
                into[skillId] = delta;
        }
    }
}
