// ============================================================
// BodyEffectSkillModifierCatalog — 부위 효과 → 숙련 delta 매핑 SSOT
// ============================================================

using System;
using System.Collections.Generic;

namespace Garunnir.Runtime.Gameplay.Data
{
    public readonly struct BodySkillMod
    {
        public readonly string SkillId;
        public readonly int ValuePerIntensity;
        public readonly bool PercentOfBase;

        public BodySkillMod(string skillId, int valuePerIntensity, bool percentOfBase = false)
        {
            SkillId = skillId;
            ValuePerIntensity = valuePerIntensity;
            PercentOfBase = percentOfBase;
        }
    }

    /// <summary>
    /// 밸런스 수치는 스캐폴딩. intensity와 곱해 합산된다.
    /// 퍼센트 항목은 Base 대비이며, 여러 부위를 합산한 뒤 한 번 반올림한다.
    /// </summary>
    public static class BodyEffectSkillModifierCatalog
    {
        /// <summary>동상 1부위당 DEX Base 퍼센트. FrostbiteParts 5부 = −50%.</summary>
        public const int FrostbiteDexPercentPerIntensity = -10;

        static readonly Dictionary<string, BodySkillMod[]> ByEffectId =
            new(StringComparer.Ordinal)
            {
                [BodyPartEffectIds.Adrenaline] = new[]
                {
                    new BodySkillMod(AttributeIds.Str, 1),
                    new BodySkillMod(AttributeIds.Dex, 1)
                },
                [BodyPartEffectIds.Fracture] = new[]
                {
                    new BodySkillMod(AttributeIds.Dex, -2),
                    new BodySkillMod(AttributeIds.Str, -1)
                },
                [BodyPartEffectIds.Bleed] = new[]
                {
                    new BodySkillMod(AttributeIds.Con, -1)
                },
                [BodyPartEffectIds.Infected] = new[]
                {
                    new BodySkillMod(AttributeIds.Con, -1),
                    new BodySkillMod(AttributeIds.Cha, -1)
                },
                [BodyPartEffectIds.Regenerating] = new[]
                {
                    new BodySkillMod(AttributeIds.Con, 1)
                },
                [BodyPartEffectIds.Frostbite] = new[]
                {
                    new BodySkillMod(AttributeIds.Dex, FrostbiteDexPercentPerIntensity, percentOfBase: true)
                },
                [BodyPartEffectIds.Heat] = new[]
                {
                    new BodySkillMod(AttributeIds.Con, -1),
                    new BodySkillMod(AttributeIds.Wis, -1)
                }
            };

        public static bool TryGetDeltas(string effectId, out BodySkillMod[] deltas)
        {
            deltas = null;
            if (string.IsNullOrEmpty(effectId))
                return false;
            return ByEffectId.TryGetValue(effectId, out deltas);
        }
    }
}
