// ============================================================
// PlayerStatusMoodEntries — 상태 요약 HUD 무드 슬롯 수집
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
            Collect(body, vitals, encumbranceStage, PlayerNeedsHost.Active, into);
        }

        public static void Collect(
            ICharacterBody body,
            IPlayerVitals vitals,
            PlayerEncumbranceStage encumbranceStage,
            PlayerNeedsHost needs,
            List<MoodEntry> into)
        {
            if (into == null)
                return;

            into.Clear();

            CollectMoodNeed(into);

            if (body != null)
            {
                CollectActiveBleed(body, into);
                CollectBodyEffects(body, into);
                CollectIllness(body, into);
            }

            if (vitals != null)
                CollectVitals(vitals, into);

            CollectNeeds(needs, vitals, into);
            CollectEncumbrance(encumbranceStage, into);
            CollectPain(body, into);
            CollectBlood(body, into);
            CollectConsciousness(body, into);
            CollectDefeat(into);
            CollectImbalance(into);
            CollectCoreFeeling(PlayerGearHost.Active?.BodyTemperature, into);
        }

        static void CollectMoodNeed(List<MoodEntry> into)
        {
            CharacterMoodHost mood = CharacterMoodHost.Active;
            if (mood == null)
                return;

            float value = mood.Mood;
            into.Add(new MoodEntry(
                MoodThoughtLabels.ResolveMoodIcon(value),
                MoodThoughtLabels.ResolveMoodPolarity(value),
                value / 100f,
                MoodThoughtLabels.FormatHudTooltip(value, mood.Thoughts, mood.BreakKind)));
        }

        static void CollectImbalance(List<MoodEntry> into)
        {
            CharacterImbalanceHost host = CharacterImbalanceHost.Active;
            if (host == null)
                return;

            float imbalance = host.Imbalance01;
            if (imbalance < CombatImbalance.HudMin)
                return;

            bool fallen = host.IsFullyUnbalanced;
            into.Add(new MoodEntry(
                MoodIconId.OffBalance,
                MoodPolarity.Negative,
                CombatImbalance.BucketIntensity(imbalance),
                PlayerStatusLabels.GetOffBalanceTooltip(fallen)));
        }

        static readonly List<BodyPartEffect> PainEffectScratch = new(16);

        static void CollectActiveBleed(ICharacterBody body, List<MoodEntry> into)
        {
            if (!PlayerStatusBleedDisplay.TrySnapshot(body, out PlayerStatusBleedSnapshot bleed))
                return;

            bool showNumeric = PlayerStatusVitalDisplay.CanShowNumericVitals(GameplayData.Stats);
            into.Add(new MoodEntry(
                MoodIconId.Bleed,
                MoodPolarity.Negative,
                ResolveEffectIntensity(bleed.TotalBleedIntensity),
                PlayerStatusLabels.FormatBleedTooltip(bleed, showNumeric)));
        }

        static int SumOrganicBleed(BodyPartNode node)
        {
            if (node == null || node.Kind == BodyPartKind.Prosthetic)
                return 0;

            int sum = 0;
            IReadOnlyList<BodyPartEffect> effects = node.Effects;
            for (int i = 0; i < effects.Count; i++)
            {
                if (effects[i].EffectId != BodyPartEffectIds.Bleed
                    && effects[i].EffectId != BodyPartEffectIds.OrganBleed)
                    continue;
                int intensity = effects[i].Intensity;
                sum += intensity < 1 ? 1 : intensity;
            }

            IReadOnlyList<BodyPartNode> children = node.Children;
            for (int c = 0; c < children.Count; c++)
                sum += SumOrganicBleed(children[c]);
            return sum;
        }

        static void CollectBlood(ICharacterBody body, List<MoodEntry> into)
        {
            if (body == null)
                return;

            float blood = body.Blood01;
            if (blood >= BodyCapacity.BloodHudMin)
                return;

            bool critical = blood <= BodyCapacity.BloodHudCritical;
            bool showNumeric = PlayerStatusVitalDisplay.CanShowNumericVitals(GameplayData.Stats);
            string tooltip = PlayerStatusBleedDisplay.TrySnapshot(body, out PlayerStatusBleedSnapshot bleed)
                ? PlayerStatusLabels.GetBloodTooltip(critical, bleed, showNumeric)
                : PlayerStatusLabels.GetBloodTooltip(critical);
            into.Add(new MoodEntry(
                MoodIconId.Pale,
                MoodPolarity.Negative,
                BodyCapacity.MoodBucket01(1f - blood),
                tooltip));
        }

        static void CollectConsciousness(ICharacterBody body, List<MoodEntry> into)
        {
            if (body == null)
                return;

            float con = BodyCapacity.Consciousness(body);
            if (con >= BodyCapacity.ConsciousnessHudMin)
                return;

            bool fatal = con <= 0f;
            bool downed = !fatal && con < BodyCapacity.ConsciousnessDownedThreshold;
            into.Add(new MoodEntry(
                MoodIconId.Fading,
                MoodPolarity.Negative,
                BodyCapacity.MoodBucket01(1f - con),
                PlayerStatusLabels.GetConsciousnessTooltip(downed, fatal)));
        }

        static void CollectDefeat(List<MoodEntry> into)
        {
            ICharacterDefeat defeat = GameplayData.Defeat;
            if (defeat == null || !defeat.IsDefeated)
                return;
            if (defeat.Cause != DefeatCause.StatCollapse)
                return;
            if (ContainsIcon(into, MoodIconId.StatCollapse))
                return;

            into.Add(new MoodEntry(
                MoodIconId.StatCollapse,
                MoodPolarity.Negative,
                1f,
                PlayerStatusLabels.GetStatCollapseTooltip()));
        }

        static void CollectPain(ICharacterBody body, List<MoodEntry> into)
        {
            if (body == null)
                return;

            float pain = CombatPain.EffectivePain01(body, PainEffectScratch);
            if (pain < CombatPain.PainHudMin)
                return;

            bool severe = pain >= CombatPain.SeverePainHudMin;
            into.Add(new MoodEntry(
                severe ? MoodIconId.SeverePain : MoodIconId.Pain,
                MoodPolarity.Negative,
                pain,
                PlayerStatusLabels.GetPainTooltip(severe)));
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

        static void CollectNeeds(PlayerNeedsHost needs, IPlayerVitals vitals, List<MoodEntry> into)
        {
            if (needs == null)
                return;

            PlayerNeedsSettings settings = needs.Settings;
            CollectFoodMood(needs, vitals, settings, into);
            CollectThirstMood(vitals, settings, into);
            CollectSleepMood(needs, settings, into);
            CollectMetaboliteMoods(needs, settings, into);
        }

        static void CollectFoodMood(
            PlayerNeedsHost needs,
            IPlayerVitals vitals,
            PlayerNeedsSettings settings,
            List<MoodEntry> into)
        {
            float cap = settings != null
                ? settings.StomachCapacityMl
                : PlayerNeedsSettings.DefaultStomachCapacityMl;
            float stomachRatio = cap > 0f ? needs.StomachUsedMl / cap : 0f;
            int stored = vitals != null ? vitals.GetCurrent(VitalKeys.Hunger) : 0;
            int storedMax = settings != null
                ? settings.MaxStoredKcal
                : PlayerNeedsSettings.DefaultMaxStoredKcal;
            if (storedMax <= 0 && vitals != null)
                storedMax = vitals.GetMax(VitalKeys.Hunger);
            float storedRatio = storedMax > 0 ? stored / (float)storedMax : 0f;

            float overate = settings != null
                ? settings.MoodOverateRatio
                : PlayerNeedsSettings.DefaultMoodOverateRatio;
            float fed = settings != null
                ? settings.MoodFedRatio
                : PlayerNeedsSettings.DefaultMoodFedRatio;
            float hungryStored = settings != null
                ? settings.MoodHungryStoredRatio
                : PlayerNeedsSettings.DefaultMoodHungryStoredRatio;
            float veryHungryStored = settings != null
                ? settings.MoodVeryHungryStoredRatio
                : PlayerNeedsSettings.DefaultMoodVeryHungryStoredRatio;

            MoodIconId iconId;
            MoodPolarity polarity;
            float intensity;
            if (stomachRatio >= overate)
            {
                iconId = MoodIconId.Full;
                polarity = MoodPolarity.Positive;
                intensity = stomachRatio;
            }
            else if (stomachRatio >= fed)
            {
                iconId = MoodIconId.Fed;
                polarity = MoodPolarity.Positive;
                intensity = PlayerStatusMoodVisuals.VitalLowIntensity;
            }
            else if (storedRatio <= veryHungryStored)
            {
                iconId = MoodIconId.VeryHungry;
                polarity = MoodPolarity.Negative;
                intensity = PlayerStatusMoodVisuals.VitalCriticalIntensity;
            }
            else if (storedRatio <= hungryStored)
            {
                iconId = MoodIconId.Hungry;
                polarity = MoodPolarity.Negative;
                intensity = PlayerStatusMoodVisuals.VitalLowIntensity;
            }
            else
            {
                iconId = MoodIconId.Fed;
                polarity = MoodPolarity.Positive;
                intensity = PlayerStatusMoodVisuals.VitalLowIntensity;
            }

            if (ContainsIcon(into, iconId))
                return;

            into.Add(new MoodEntry(iconId, polarity, intensity, PlayerStatusLabels.GetMoodTooltip(iconId)));
        }

        static void CollectThirstMood(IPlayerVitals vitals, PlayerNeedsSettings settings, List<MoodEntry> into)
        {
            if (vitals == null)
                return;

            int cur = vitals.GetCurrent(VitalKeys.Thirst);
            int max = vitals.GetMax(VitalKeys.Thirst);
            float quenched = settings != null
                ? settings.MoodThirstQuenchedRatio
                : PlayerNeedsSettings.DefaultMoodThirstQuenchedRatio;
            float thirsty = settings != null
                ? settings.MoodThirstyRatio
                : PlayerNeedsSettings.DefaultMoodThirstyRatio;
            float veryThirsty = settings != null
                ? settings.MoodVeryThirstyRatio
                : PlayerNeedsSettings.DefaultMoodVeryThirstyRatio;

            float ratio = max > 0 ? cur / (float)max : 0f;
            MoodIconId iconId;
            MoodPolarity polarity;
            float intensity;
            if (max > 0 && ratio >= quenched)
            {
                iconId = MoodIconId.ThirstQuenched;
                polarity = MoodPolarity.Positive;
                intensity = PlayerStatusMoodVisuals.VitalLowIntensity;
            }
            else if (max <= 0 || ratio <= veryThirsty)
            {
                iconId = MoodIconId.VeryThirsty;
                polarity = MoodPolarity.Negative;
                intensity = PlayerStatusMoodVisuals.VitalCriticalIntensity;
            }
            else if (ratio <= thirsty)
            {
                iconId = MoodIconId.Thirsty;
                polarity = MoodPolarity.Negative;
                intensity = PlayerStatusMoodVisuals.VitalLowIntensity;
            }
            else
                return;

            if (ContainsIcon(into, iconId))
                return;

            into.Add(new MoodEntry(iconId, polarity, intensity, PlayerStatusLabels.GetMoodTooltip(iconId)));
        }

        static void CollectSleepMood(
            PlayerNeedsHost needs,
            PlayerNeedsSettings settings,
            List<MoodEntry> into)
        {
            float tired = settings != null
                ? settings.MoodTiredRatio
                : PlayerNeedsSettings.DefaultMoodTiredRatio;
            float veryTired = settings != null
                ? settings.MoodVeryTiredRatio
                : PlayerNeedsSettings.DefaultMoodVeryTiredRatio;
            float needRest = settings != null
                ? settings.MoodNeedRestRatio
                : PlayerNeedsSettings.DefaultMoodNeedRestRatio;

            float display = needs.SleepDisplay01;
            MoodIconId iconId;
            float intensity;
            if (display >= needRest)
            {
                iconId = MoodIconId.NeedRest;
                intensity = PlayerStatusMoodVisuals.VitalCriticalIntensity;
            }
            else if (display >= veryTired)
            {
                iconId = MoodIconId.VeryTired;
                intensity = PlayerStatusMoodVisuals.VitalCriticalIntensity;
            }
            else if (display >= tired)
            {
                iconId = MoodIconId.Tired;
                intensity = PlayerStatusMoodVisuals.VitalLowIntensity;
            }
            else
                return;

            if (ContainsIcon(into, iconId))
                return;

            into.Add(new MoodEntry(
                iconId,
                MoodPolarity.Negative,
                intensity,
                PlayerStatusLabels.GetMoodTooltip(iconId)));
        }

        static void CollectMetaboliteMoods(
            PlayerNeedsHost needs,
            PlayerNeedsSettings settings,
            List<MoodEntry> into)
        {
            int funGate = settings != null
                ? AbsThreshold(settings.RotFunPenalty)
                : AbsThreshold(PlayerNeedsSettings.DefaultRotFunPenalty);
            int healthyGate = settings != null
                ? AbsThreshold(settings.RotHealthyPenalty)
                : AbsThreshold(PlayerNeedsSettings.DefaultRotHealthyPenalty);

            if (needs.Fun >= funGate)
                TryAddNeedsMood(into, MoodIconId.GoodMood, MoodPolarity.Positive);
            else if (needs.Fun <= -funGate)
                TryAddNeedsMood(into, MoodIconId.Sad, MoodPolarity.Negative);

            if (needs.Healthy <= -healthyGate)
                TryAddNeedsMood(into, MoodIconId.Sick, MoodPolarity.Negative);

            if (needs.Stim >= funGate)
                TryAddNeedsMood(into, MoodIconId.Adrenaline, MoodPolarity.Positive);
        }

        static void TryAddNeedsMood(List<MoodEntry> into, MoodIconId iconId, MoodPolarity polarity)
        {
            if (ContainsIcon(into, iconId))
                return;

            into.Add(new MoodEntry(
                iconId,
                polarity,
                PlayerStatusMoodVisuals.EffectDefaultIntensity,
                PlayerStatusLabels.GetMoodTooltip(iconId)));
        }

        static int AbsThreshold(int value) => value < 0 ? -value : value;

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

        static void CollectIllness(ICharacterBody body, List<MoodEntry> into)
        {
            if (body.Toxin01 >= BodyIllness.ToxinMoodMin && !ContainsIcon(into, MoodIconId.Infected))
            {
                into.Add(new MoodEntry(
                    MoodIconId.Infected,
                    MoodPolarity.Negative,
                    body.Toxin01,
                    PlayerStatusLabels.GetEffectName(BodyPartEffectIds.Toxin)));
            }

            float filtration = BodyCapacity.BloodFiltration(body);
            if (filtration < BodyIllness.LowImmunityFiltration
                && !ContainsIcon(into, MoodIconId.LowImmunity))
            {
                into.Add(new MoodEntry(
                    MoodIconId.LowImmunity,
                    MoodPolarity.Negative,
                    1f - filtration,
                    PlayerStatusLabels.GetMoodTooltip(MoodIconId.LowImmunity)));
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
                if (effect.EffectId == BodyPartEffectIds.Bleed
                    || effect.EffectId == BodyPartEffectIds.OrganBleed)
                    continue;

                if (!PlayerStatusMoodEffectCatalog.TryGet(
                        effect.EffectId,
                        out MoodIconId iconId,
                        out MoodPolarity polarity))
                {
                    continue;
                }

                float intensity = ResolveEffectIntensity(effect.Intensity);
                if (bestIntensity.TryGetValue(iconId, out float existing) && existing > intensity)
                    intensity = existing;
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
