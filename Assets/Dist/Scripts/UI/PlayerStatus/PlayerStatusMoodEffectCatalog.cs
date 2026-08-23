// ============================================================
// PlayerStatusMoodEffectCatalog — 부위 효과 → 아이콘·극성 SSOT
// ============================================================

using System;
using System.Collections.Generic;

namespace Garunnir.Runtime.Gameplay.Data
{
    public static class PlayerStatusMoodEffectCatalog
    {
        readonly struct EffectSpec
        {
            public readonly MoodIconId IconId;
            public readonly MoodPolarity Polarity;

            public EffectSpec(MoodIconId iconId, MoodPolarity polarity)
            {
                IconId = iconId;
                Polarity = polarity;
            }
        }

        static readonly Dictionary<string, EffectSpec> ByEffectId =
            new(StringComparer.Ordinal)
            {
                [BodyPartEffectIds.Bleed] = new(MoodIconId.Bleed, MoodPolarity.Negative),
                [BodyPartEffectIds.Fracture] = new(MoodIconId.Fracture, MoodPolarity.Negative),
                [BodyPartEffectIds.Infected] = new(MoodIconId.Infected, MoodPolarity.Negative),
                [BodyPartEffectIds.Regenerating] = new(MoodIconId.Regenerating, MoodPolarity.Positive),
                [BodyPartEffectIds.Adrenaline] = new(MoodIconId.Adrenaline, MoodPolarity.Positive),
                [BodyPartEffectIds.Frostbite] = new(MoodIconId.Hypothermia, MoodPolarity.Negative),
                [BodyPartEffectIds.Heat] = new(MoodIconId.Overheated, MoodPolarity.Negative),
                [BodyPartEffectIds.Bloated] = new(MoodIconId.Discomfort, MoodPolarity.Negative),
                [BodyPartEffectIds.Toxin] = new(MoodIconId.Infected, MoodPolarity.Negative),
            };

        public static bool TryGet(string effectId, out MoodIconId iconId, out MoodPolarity polarity)
        {
            iconId = default;
            polarity = MoodPolarity.Neutral;
            if (string.IsNullOrEmpty(effectId) ||
                !ByEffectId.TryGetValue(effectId, out EffectSpec spec))
            {
                return false;
            }

            iconId = spec.IconId;
            polarity = spec.Polarity;
            return true;
        }
    }
}
