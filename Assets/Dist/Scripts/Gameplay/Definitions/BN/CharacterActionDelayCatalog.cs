// ============================================================
// CharacterActionDelayCatalog — 부위 효과 → 행동 틱 배율 SSOT
// ============================================================

using System;
using System.Collections.Generic;

namespace Garunnir.Runtime.Gameplay.Data
{
    /// <summary>
    /// 밸런스 수치는 스캐폴딩. intensity만큼 곱해 쌓인다. 미등록 효과는 1.
    /// </summary>
    public static class CharacterActionDelayCatalog
    {
        public const float MinTickScale = 0.05f;

        static readonly Dictionary<string, float> ScalePerIntensity =
            new(StringComparer.Ordinal)
            {
                [BodyPartEffectIds.Fracture] = 0.8f,
                [BodyPartEffectIds.Bleed] = 0.9f,
                [BodyPartEffectIds.Infected] = 0.9f,
                [BodyPartEffectIds.Frostbite] = 0.85f,
                [BodyPartEffectIds.Heat] = 0.85f,
                [BodyPartEffectIds.Adrenaline] = 1.15f
            };

        public static bool TryGetScalePerIntensity(string effectId, out float scale)
        {
            scale = 1f;
            if (string.IsNullOrEmpty(effectId))
                return false;
            return ScalePerIntensity.TryGetValue(effectId, out scale);
        }
    }
}
