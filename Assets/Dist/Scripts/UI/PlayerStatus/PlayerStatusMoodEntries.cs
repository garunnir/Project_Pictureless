// ============================================================
// PlayerStatusMoodEntries ? ?? ?? HUD ?? ??? ??
// ============================================================

using System.Collections.Generic;

namespace Garunnir.Runtime.Gameplay.Data
{
    public static class PlayerStatusMoodEntries
    {
        public static void Collect(ICharacterBody body, IPlayerVitals vitals, List<MoodEntry> into)
        {
            Collect(body, vitals, PlayerEncumbranceStage.None, into);
        }

        public static void Collect(
            ICharacterBody body,
            IPlayerVitals vitals,
            PlayerEncumbranceStage encumbranceStage,
            List<MoodEntry> into)
        {
            if (into == null)
                return;

            into.Clear();

            if (vitals != null)
                CollectVitals(vitals, into);

            if (body != null)
                CollectBodyEffects(body, into);

            CollectEncumbrance(encumbranceStage, into);
            CollectCoreFeeling(PlayerGearHost.Active?.BodyTemperature, into);
        }

        static void CollectEncumbrance(PlayerEncumbranceStage stage, List<MoodEntry> into)
        {
            if (stage == PlayerEncumbranceStage.None)
                return;

            into.Add(new MoodEntry(
                MoodIconId.Overencumbered,
                MoodPolarity.Negative,
                PlayerEncumbrance.GetMoodIntensity(stage),
                PlayerStatusLabels.GetEncumbranceTooltip(stage)));
        }

        static void CollectCoreFeeling(BodyTemp bodyTemp, List<MoodEntry> into)
        {
            if (bodyTemp == null)
                return;

            float coreC = bodyTemp.BodyTempC;
            BodyTempFeeling feeling = bodyTemp.Feeling;
            MoodIconId iconId;
            MoodPolarity polarity;
            float intensity;
            if (coreC <= BodyTemp.HypothermiaBodyTempC)
            {
                iconId = MoodIconId.Hypothermia;
                polarity = MoodPolarity.Negative;
                intensity = PlayerStatusMoodVisuals.EffectDefaultIntensity;
            }
            else if (feeling == BodyTempFeeling.Cold || feeling == BodyTempFeeling.Cool)
            {
                iconId = MoodIconId.TooCold;
                polarity = MoodPolarity.Negative;
                intensity = feeling == BodyTempFeeling.Cold
                    ? PlayerStatusMoodVisuals.EffectDefaultIntensity
                    : PlayerStatusMoodVisuals.VitalLowIntensity;
            }
            else if (feeling == BodyTempFeeling.Hot)
            {
                iconId = MoodIconId.TooHot;
                polarity = MoodPolarity.Negative;
                intensity = PlayerStatusMoodVisuals.EffectDefaultIntensity;
            }
            else if (feeling == BodyTempFeeling.Warm)
            {
                iconId = MoodIconId.Warm;
                polarity = MoodPolarity.Positive;
                intensity = PlayerStatusMoodVisuals.VitalLowIntensity;
            }
            else
            {
                iconId = MoodIconId.Comfortable;
                polarity = MoodPolarity.Positive;
                intensity = PlayerStatusMoodVisuals.VitalLowIntensity;
            }

            if (ContainsIcon(into, iconId))
                return;

            into.Add(new MoodEntry(
                iconId,
                polarity,
                intensity,
                CharacterGearLabels.FormatBodyTempFeeling(feeling)));
        }

        static bool ContainsIcon(List<MoodEntry> into, MoodIconId iconId)
        {
            for (int i = 0; i < into.Count; i++)
            {
                if (into[i].IconId == iconId)
                    return true;
            }

            return false;
        }

        static void CollectVitals(IPlayerVitals vitals, List<MoodEntry> into)
        {
            for (int i = 0; i < VitalKeys.All.Length; i++)
            {
                string vitalKey = VitalKeys.All[i];
                if (!TryMapVitalIcon(vitalKey, out MoodIconId iconId))
                    continue;

                int cur = vitals.GetCurrent(vitalKey);
                int max = vitals.GetMax(vitalKey);
                PlayerStatusVitalDisplay.VitalProseBand band =
                    PlayerStatusVitalDisplay.ResolveBand(cur, max);

                if (band != PlayerStatusVitalDisplay.VitalProseBand.Low &&
                    band != PlayerStatusVitalDisplay.VitalProseBand.Critical)
                {
                    continue;
                }

                float intensity = band == PlayerStatusVitalDisplay.VitalProseBand.Critical
                    ? PlayerStatusMoodVisuals.VitalCriticalIntensity
                    : PlayerStatusMoodVisuals.VitalLowIntensity;

                string tooltip = PlayerStatusLabels.FormatVitalProse(vitalKey, cur, max);
                into.Add(new MoodEntry(iconId, MoodPolarity.Negative, intensity, tooltip));
            }
        }

        static void CollectBodyEffects(ICharacterBody body, List<MoodEntry> into)
        {
            var bestIntensity = new Dictionary<MoodIconId, float>();
            var polarities = new Dictionary<MoodIconId, MoodPolarity>();
            var tooltips = new Dictionary<MoodIconId, string>();

            IReadOnlyList<BodyPartNode> roots = body.Roots;
            for (int r = 0; r < roots.Count; r++)
                CollectEffectsRecursive(roots[r], bestIntensity, polarities, tooltips);

            foreach (KeyValuePair<MoodIconId, float> pair in bestIntensity)
            {
                MoodIconId iconId = pair.Key;
                polarities.TryGetValue(iconId, out MoodPolarity polarity);
                tooltips.TryGetValue(iconId, out string tooltip);
                into.Add(new MoodEntry(iconId, polarity, pair.Value, tooltip));
            }
        }

        static void CollectEffectsRecursive(
            BodyPartNode node,
            Dictionary<MoodIconId, float> bestIntensity,
            Dictionary<MoodIconId, MoodPolarity> polarities,
            Dictionary<MoodIconId, string> tooltips)
        {
            if (node == null)
                return;

            IReadOnlyList<BodyPartEffect> effects = node.Effects;
            for (int i = 0; i < effects.Count; i++)
            {
                BodyPartEffect effect = effects[i];
                if (!PlayerStatusMoodEffectCatalog.TryGet(
                        effect.EffectId,
                        out MoodIconId iconId,
                        out MoodPolarity polarity))
                {
                    continue;
                }

                float intensity = ResolveEffectIntensity(effect.Intensity);
                if (bestIntensity.TryGetValue(iconId, out float existing))
                    intensity = intensity > existing ? intensity : existing;
                else
                    bestIntensity[iconId] = intensity;

                polarities[iconId] = polarity;
                tooltips[iconId] = PlayerStatusLabels.GetEffectName(effect.EffectId);
            }

            IReadOnlyList<BodyPartNode> children = node.Children;
            for (int c = 0; c < children.Count; c++)
                CollectEffectsRecursive(children[c], bestIntensity, polarities, tooltips);
        }

        static float ResolveEffectIntensity(int intensity)
        {
            if (intensity <= 0)
                return PlayerStatusMoodVisuals.EffectDefaultIntensity;

            return intensity > 1f
                ? 1f
                : intensity;
        }

        static bool TryMapVitalIcon(string vitalKey, out MoodIconId iconId)
        {
            string shortKey = PlayerStatusVitalDisplay.GetVitalShortKey(vitalKey);
            switch (shortKey)
            {
                case "Hunger":
                    iconId = MoodIconId.Hunger;
                    return true;
                case "Thirst":
                    iconId = MoodIconId.Thirst;
                    return true;
                case "Stamina":
                    iconId = MoodIconId.Stamina;
                    return true;
                default:
                    iconId = default;
                    return false;
            }
        }
    }
}
