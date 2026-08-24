// ============================================================
// MoodSituationalCollector — 몸·니즈·체온·과적에서 상황 사고 수집
// ============================================================

using System.Collections.Generic;
using Garunnir.Runtime.Gameplay.Data;
using UnityEngine;

public static class MoodSituationalCollector
{
    static readonly List<BodyPartEffect> PainScratch = new(16);

    public static void Collect(
        ICharacterBody body,
        IPlayerVitals vitals,
        PlayerNeedsHost needs,
        PlayerEncumbranceStage encumbrance,
        MoodSettings settings,
        List<MoodThought> into)
    {
        if (into == null || settings == null)
            return;

        CollectPain(body, settings, into);
        CollectBleed(body, settings, into);
        CollectNeeds(needs, vitals, settings, into);
        CollectFeeling(settings, into);
        CollectEncumbrance(encumbrance, settings, into);
    }

    static void CollectPain(ICharacterBody body, MoodSettings settings, List<MoodThought> into)
    {
        if (body == null)
            return;

        float pain = CombatPain.EffectivePain01(body, PainScratch);
        if (pain < CombatPain.PainHudMin)
            return;

        ThoughtId id = pain >= CombatPain.SeverePainHudMin ? ThoughtId.SeverePain : ThoughtId.Pain;
        TryAdd(settings, id, into);
    }

    static void CollectBleed(ICharacterBody body, MoodSettings settings, List<MoodThought> into)
    {
        if (body == null)
            return;

        IReadOnlyList<BodyPartNode> roots = body.Roots;
        for (int r = 0; r < roots.Count; r++)
        {
            if (SumOrganicBleed(roots[r]) <= 0)
                continue;

            TryAdd(settings, ThoughtId.Bleed, into);
            return;
        }
    }

    static int SumOrganicBleed(BodyPartNode node)
    {
        if (node == null || node.Kind == BodyPartKind.Prosthetic)
            return 0;

        int sum = 0;
        IReadOnlyList<BodyPartEffect> effects = node.Effects;
        for (int i = 0; i < effects.Count; i++)
        {
            if (effects[i].EffectId != BodyPartEffectIds.Bleed)
                continue;
            int intensity = effects[i].Intensity;
            sum += intensity < 1 ? 1 : intensity;
        }

        IReadOnlyList<BodyPartNode> children = node.Children;
        for (int c = 0; c < children.Count; c++)
            sum += SumOrganicBleed(children[c]);
        return sum;
    }

    static void CollectNeeds(
        PlayerNeedsHost needs,
        IPlayerVitals vitals,
        MoodSettings settings,
        List<MoodThought> into)
    {
        PlayerNeedsSettings needSettings = needs != null ? needs.Settings : null;
        CollectFood(needs, vitals, needSettings, settings, into);
        CollectThirst(vitals, needSettings, settings, into);
    }

    static void CollectFood(
        PlayerNeedsHost needs,
        IPlayerVitals vitals,
        PlayerNeedsSettings needSettings,
        MoodSettings settings,
        List<MoodThought> into)
    {
        if (needs == null && vitals == null)
            return;

        int stored = vitals != null ? vitals.GetCurrent(VitalKeys.Hunger) : 0;
        int storedMax = needSettings != null
            ? needSettings.MaxStoredKcal
            : PlayerNeedsSettings.DefaultMaxStoredKcal;
        if (storedMax <= 0 && vitals != null)
            storedMax = vitals.GetMax(VitalKeys.Hunger);
        float storedRatio = storedMax > 0 ? stored / (float)storedMax : 0f;

        float hungry = needSettings != null
            ? needSettings.MoodHungryStoredRatio
            : PlayerNeedsSettings.DefaultMoodHungryStoredRatio;
        float veryHungry = needSettings != null
            ? needSettings.MoodVeryHungryStoredRatio
            : PlayerNeedsSettings.DefaultMoodVeryHungryStoredRatio;

        if (storedRatio <= veryHungry)
            TryAdd(settings, ThoughtId.VeryHungry, into);
        else if (storedRatio <= hungry)
            TryAdd(settings, ThoughtId.Hungry, into);
    }

    static void CollectThirst(
        IPlayerVitals vitals,
        PlayerNeedsSettings needSettings,
        MoodSettings settings,
        List<MoodThought> into)
    {
        if (vitals == null)
            return;

        int cur = vitals.GetCurrent(VitalKeys.Thirst);
        int max = vitals.GetMax(VitalKeys.Thirst);
        float thirsty = needSettings != null
            ? needSettings.MoodThirstyRatio
            : PlayerNeedsSettings.DefaultMoodThirstyRatio;
        float veryThirsty = needSettings != null
            ? needSettings.MoodVeryThirstyRatio
            : PlayerNeedsSettings.DefaultMoodVeryThirstyRatio;

        float ratio = max > 0 ? cur / (float)max : 0f;
        if (max <= 0 || ratio <= veryThirsty)
            TryAdd(settings, ThoughtId.VeryThirsty, into);
        else if (ratio <= thirsty)
            TryAdd(settings, ThoughtId.Thirsty, into);
    }

    static void CollectFeeling(MoodSettings settings, List<MoodThought> into)
    {
        BodyTemp bodyTemp = PlayerGearHost.Active?.BodyTemperature;
        if (bodyTemp == null)
            return;

        if (bodyTemp.BodyTempC <= BodyTemp.HypothermiaBodyTempC)
        {
            TryAdd(settings, ThoughtId.Hypothermia, into);
            return;
        }

        BodyTempFeeling feeling = bodyTemp.Feeling;
        if (feeling == BodyTempFeeling.Cold || feeling == BodyTempFeeling.Cool)
            TryAdd(settings, ThoughtId.TooCold, into);
        else if (feeling == BodyTempFeeling.Hot)
            TryAdd(settings, ThoughtId.TooHot, into);
    }

    static void CollectEncumbrance(
        PlayerEncumbranceStage stage,
        MoodSettings settings,
        List<MoodThought> into)
    {
        if (stage == PlayerEncumbranceStage.None)
            return;

        TryAdd(settings, ThoughtId.Overencumbered, into);
    }

    static void TryAdd(MoodSettings settings, ThoughtId id, List<MoodThought> into)
    {
        if (!settings.TryGetThought(id, out MoodSettings.ThoughtRow row) || row == null)
            return;

        into.Add(new MoodThought(id, MoodThoughtKind.Situational, row.offset, 0));
    }
}
