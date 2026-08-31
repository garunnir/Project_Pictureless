// ============================================================
// PlayerStatusMoodTypes — 상태 요약 HUD 아이콘·극성·엔트리 SSOT
// ============================================================

namespace Garunnir.Runtime.Gameplay.Data
{
    public enum MoodIconId
    {
        Hunger,
        Thirst,
        Stamina,
        Bleed,
        Fracture,
        Infected,
        Regenerating,
        Adrenaline,
        GoodMood,
        Happy,
        VeryHappy,
        Stable,
        SlightlyHappy,
        Neutral,
        SlightlySad,
        Sad,
        VerySad,
        Depressed,
        Stressed,
        SeverelyStressed,
        Fear,
        ExtremeFear,
        Angry,
        Furious,
        Tired,
        VeryTired,
        NeedRest,
        WellRested,
        Hungry,
        VeryHungry,
        Fed,
        Full,
        Thirsty,
        VeryThirsty,
        ThirstQuenched,
        Discomfort,
        Pain,
        SeverePain,
        Injured,
        SeverelyInjured,
        Sick,
        SeverelySick,
        LowImmunity,
        Recovering,
        Pale,
        Overheated,
        Hypothermia,
        Comfortable,
        Dirty,
        VeryDirty,
        NeedShower,
        Attractive,
        Warm,
        TooHot,
        TooCold,
        Dark,
        Lonely,
        Bored,
        Idle,
        PleasantConversation,
        GoodMeal,
        RestArea,
        SuitableEnvironment,
        NatureFriendly,
        Inspired,
        Motivated,
        SkillUp,
        RelationshipImproved,
        Loved,
        MarriedEngaged,
        Trust,
        Respect,
        Overencumbered,
        OffBalance,
        Fading,
        StatCollapse,
    // ── Reserved HUD chips (PlayerStatusMoodChipSlots Pending) ──
        Suffocating,
        PainShocked,
        CapacityDown,
        Dying,
        Defeated
    }

    public enum MoodPolarity
    {
        Neutral,
        Positive,
        Negative
    }

    public readonly struct MoodEntry
    {
        public readonly MoodIconId IconId;
        public readonly MoodPolarity Polarity;
        public readonly float Intensity;
        public readonly string TooltipText;

        public MoodEntry(MoodIconId iconId, MoodPolarity polarity, float intensity, string tooltipText)
        {
            IconId = iconId;
            Polarity = polarity;
            Intensity = intensity;
            TooltipText = tooltipText ?? string.Empty;
        }
    }
}
