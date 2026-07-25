// ============================================================
// BodyEffectSkillModifierCatalog — 부위 효과 → 숙련 delta 매핑 SSOT
// ============================================================

using System;
using System.Collections.Generic;

namespace Garunnir.Runtime.Gameplay.Data
{
    /// <summary>
    /// 밸런스 수치는 스캐폴딩. intensity와 곱해 합산된다.
    /// </summary>
    public static class BodyEffectSkillModifierCatalog
    {
        static readonly Dictionary<string, (string skillId, int deltaPerIntensity)[]> ByEffectId =
            new(StringComparer.Ordinal)
            {
                [BodyPartEffectIds.Adrenaline] = new[]
                {
                    (AttributeIds.Str, 1),
                    (AttributeIds.Dex, 1)
                },
                [BodyPartEffectIds.Fracture] = new[]
                {
                    (AttributeIds.Dex, -2),
                    (AttributeIds.Str, -1)
                },
                [BodyPartEffectIds.Bleed] = new[]
                {
                    (AttributeIds.Con, -1)
                },
                [BodyPartEffectIds.Infected] = new[]
                {
                    (AttributeIds.Con, -1),
                    (AttributeIds.Cha, -1)
                },
                [BodyPartEffectIds.Regenerating] = new[]
                {
                    (AttributeIds.Con, 1)
                }
            };

        public static bool TryGetDeltas(
            string effectId,
            out (string skillId, int deltaPerIntensity)[] deltas)
        {
            deltas = null;
            if (string.IsNullOrEmpty(effectId))
                return false;
            return ByEffectId.TryGetValue(effectId, out deltas);
        }
    }
}
