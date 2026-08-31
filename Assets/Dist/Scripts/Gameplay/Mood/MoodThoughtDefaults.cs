// ============================================================
// MoodThoughtDefaults — 사고 표 기본 행 SSOT (MoodSettings 병합·에디터 Ensure)
// ============================================================

using System;
using System.Collections.Generic;

public static class MoodThoughtDefaults
{
    public readonly struct Row
    {
        public readonly ThoughtId Id;
        public readonly MoodThoughtKind Kind;
        public readonly int Offset;
        public readonly int DurationMinutes;
        public readonly int StackLimit;

        public Row(
            ThoughtId id,
            MoodThoughtKind kind,
            int offset,
            int durationMinutes = 0,
            int stackLimit = MoodSettings.DefaultMemoryStackLimit)
        {
            Id = id;
            Kind = kind;
            Offset = offset;
            DurationMinutes = durationMinutes;
            StackLimit = stackLimit;
        }

        public MoodSettings.ThoughtRow ToThoughtRow()
        {
            return new MoodSettings.ThoughtRow
            {
                id = Id,
                kind = Kind,
                offset = Offset,
                durationMinutes = DurationMinutes,
                stackLimit = StackLimit
            };
        }
    }

    public static readonly Row[] Catalog = new[]
    {
        Situational(ThoughtId.Pain, MoodSettings.DefaultPainOffset),
        Situational(ThoughtId.SeverePain, MoodSettings.DefaultSeverePainOffset),
        Situational(ThoughtId.Hungry, MoodSettings.DefaultHungryOffset),
        Situational(ThoughtId.VeryHungry, MoodSettings.DefaultVeryHungryOffset),
        Situational(ThoughtId.Thirsty, MoodSettings.DefaultThirstyOffset),
        Situational(ThoughtId.VeryThirsty, MoodSettings.DefaultVeryThirstyOffset),
        Situational(ThoughtId.TooCold, MoodSettings.DefaultTooColdOffset),
        Situational(ThoughtId.TooHot, MoodSettings.DefaultTooHotOffset),
        Situational(ThoughtId.Hypothermia, MoodSettings.DefaultHypothermiaOffset),
        Situational(ThoughtId.Bleed, MoodSettings.DefaultBleedOffset),
        Situational(ThoughtId.Overencumbered, MoodSettings.DefaultOverencumberedOffset),
        Situational(ThoughtId.LowOxygen, DefaultLowOxygenOffset),
        Situational(ThoughtId.PainShock, DefaultPainShockOffset),
        Situational(ThoughtId.CapacityDown, DefaultCapacityDownOffset),
        Situational(ThoughtId.Dirty, DefaultDirtyOffset),
        Situational(ThoughtId.VeryDirty, DefaultVeryDirtyOffset),
        Situational(ThoughtId.Lonely, DefaultLonelyOffset),
        Situational(ThoughtId.Bored, DefaultBoredOffset),
        Situational(ThoughtId.Uncomfortable, DefaultUncomfortableOffset),
        Situational(ThoughtId.Cramped, DefaultCrampedOffset),
        Situational(ThoughtId.Dark, DefaultDarkOffset),
        Situational(ThoughtId.Stressed, DefaultStressedOffset),
        Situational(ThoughtId.SeverelyStressed, DefaultSeverelyStressedOffset),
        Situational(ThoughtId.SeverelySick, DefaultSeverelySickOffset),
        Memory(ThoughtId.AteMeal, MoodSettings.DefaultAteMealOffset, MoodSettings.DefaultAteMealMinutes),
        Memory(ThoughtId.Vomited, MoodSettings.DefaultVomitedOffset, MoodSettings.DefaultVomitedMinutes),
        Memory(ThoughtId.AteRotten, MoodSettings.DefaultAteRottenOffset, MoodSettings.DefaultAteRottenMinutes),
        Memory(ThoughtId.Catharsis, MoodSettings.DefaultCatharsisOffset, MoodSettings.DefaultCatharsisMinutes),
        Memory(ThoughtId.Crafted, MoodSettings.DefaultCraftedOffset, MoodSettings.DefaultCraftedMinutes),
        Memory(ThoughtId.AteHotMeal, MoodSettings.DefaultAteHotMealOffset, MoodSettings.DefaultAteHotMealMinutes),
        Memory(ThoughtId.Recovering, DefaultRecoveringOffset, DefaultRecoveringMinutes),
        Memory(ThoughtId.NeedShower, DefaultNeedShowerOffset, DefaultNeedShowerMinutes),
        Memory(ThoughtId.FreshlyBathed, DefaultFreshlyBathedOffset, DefaultFreshlyBathedMinutes),
        Memory(ThoughtId.Attractive, DefaultAttractiveOffset, DefaultAttractiveMinutes),
        Memory(ThoughtId.PleasantConversation, DefaultPleasantConversationOffset, DefaultPleasantConversationMinutes),
        Memory(ThoughtId.RestArea, DefaultRestAreaOffset, DefaultRestAreaMinutes),
        Memory(ThoughtId.SuitableEnvironment, DefaultSuitableEnvironmentOffset, DefaultSuitableEnvironmentMinutes),
        Memory(ThoughtId.NatureFriendly, DefaultNatureFriendlyOffset, DefaultNatureFriendlyMinutes),
        Memory(ThoughtId.Inspired, DefaultInspiredOffset, DefaultInspiredMinutes),
        Memory(ThoughtId.Motivated, DefaultMotivatedOffset, DefaultMotivatedMinutes),
        Memory(ThoughtId.SkillUp, DefaultSkillUpOffset, DefaultSkillUpMinutes),
        Memory(ThoughtId.RelationshipImproved, DefaultRelationshipImprovedOffset, DefaultRelationshipImprovedMinutes),
        Memory(ThoughtId.Loved, DefaultLovedOffset, DefaultLovedMinutes),
        Memory(ThoughtId.MarriedEngaged, DefaultMarriedEngagedOffset, DefaultMarriedEngagedMinutes),
        Memory(ThoughtId.Trust, DefaultTrustOffset, DefaultTrustMinutes),
        Memory(ThoughtId.Respect, DefaultRespectOffset, DefaultRespectMinutes)
    };

    public const int DefaultLowOxygenOffset = -12;
    public const int DefaultPainShockOffset = -20;
    public const int DefaultCapacityDownOffset = -14;
    public const int DefaultDirtyOffset = -4;
    public const int DefaultVeryDirtyOffset = -10;
    public const int DefaultLonelyOffset = -8;
    public const int DefaultBoredOffset = -6;
    public const int DefaultUncomfortableOffset = -6;
    public const int DefaultCrampedOffset = -8;
    public const int DefaultDarkOffset = -4;
    public const int DefaultStressedOffset = -8;
    public const int DefaultSeverelyStressedOffset = -16;
    public const int DefaultSeverelySickOffset = -12;
    public const int DefaultRecoveringOffset = 4;
    public const int DefaultRecoveringMinutes = 240;
    public const int DefaultNeedShowerOffset = -6;
    public const int DefaultNeedShowerMinutes = 180;
    public const int DefaultFreshlyBathedOffset = 4;
    public const int DefaultFreshlyBathedMinutes = 180;
    public const int DefaultAttractiveOffset = 3;
    public const int DefaultAttractiveMinutes = 120;
    public const int DefaultPleasantConversationOffset = 5;
    public const int DefaultPleasantConversationMinutes = 120;
    public const int DefaultRestAreaOffset = 4;
    public const int DefaultRestAreaMinutes = 180;
    public const int DefaultSuitableEnvironmentOffset = 4;
    public const int DefaultSuitableEnvironmentMinutes = 180;
    public const int DefaultNatureFriendlyOffset = 3;
    public const int DefaultNatureFriendlyMinutes = 120;
    public const int DefaultInspiredOffset = 8;
    public const int DefaultInspiredMinutes = 240;
    public const int DefaultMotivatedOffset = 5;
    public const int DefaultMotivatedMinutes = 180;
    public const int DefaultSkillUpOffset = 4;
    public const int DefaultSkillUpMinutes = 60;
    public const int DefaultRelationshipImprovedOffset = 6;
    public const int DefaultRelationshipImprovedMinutes = 240;
    public const int DefaultLovedOffset = 10;
    public const int DefaultLovedMinutes = 360;
    public const int DefaultMarriedEngagedOffset = 8;
    public const int DefaultMarriedEngagedMinutes = 360;
    public const int DefaultTrustOffset = 4;
    public const int DefaultTrustMinutes = 240;
    public const int DefaultRespectOffset = 4;
    public const int DefaultRespectMinutes = 240;

    public static MoodSettings.ThoughtRow[] CreateDefaultThoughtRows()
    {
        var rows = new MoodSettings.ThoughtRow[Catalog.Length];
        for (int i = 0; i < Catalog.Length; i++)
            rows[i] = Catalog[i].ToThoughtRow();
        return rows;
    }

    static Row Situational(ThoughtId id, int offset) =>
        new(id, MoodThoughtKind.Situational, offset);

    static Row Memory(ThoughtId id, int offset, int minutes) =>
        new(id, MoodThoughtKind.Memory, offset, minutes);
}
