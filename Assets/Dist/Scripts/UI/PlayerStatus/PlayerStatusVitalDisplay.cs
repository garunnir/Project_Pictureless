// ============================================================
// PlayerStatusVitalDisplay — 바이탈 수치 노출 스킬 게이트 + 비율·일수 밴드 SSOT
// ============================================================
using Garunnir.Runtime.Gameplay.Data;

public static class PlayerStatusVitalDisplay
{
    public const string NumericVitalSkillId = SkillIds.Survival;
    public const int NumericVitalMinSkillLevel = 2;

    public const float BandFullRatio = 0.75f;
    public const float BandOkRatio = 0.45f;
    public const float BandLowRatio = 0.2f;

    public enum VitalProseBand
    {
        Full,
        Ok,
        Low,
        Critical
    }

    public enum HungerDaysBand
    {
        Engorged,
        Sated,
        Hungry,
        VeryHungry,
        Famished,
        Starving
    }

    public enum ThirstNeedsBand
    {
        Quenched,
        NotThirsty,
        Thirsty,
        VeryThirsty,
        Parched
    }

    public static bool CanShowNumericVitals(IPlayerStats stats)
    {
        if (stats == null)
            return false;

        return stats.GetSkillLevel(NumericVitalSkillId) >= NumericVitalMinSkillLevel;
    }

    public static VitalProseBand ResolveBand(int current, int max)
    {
        if (max <= 0)
            return VitalProseBand.Critical;

        float ratio = (float)current / max;
        if (ratio >= BandFullRatio)
            return VitalProseBand.Full;
        if (ratio >= BandOkRatio)
            return VitalProseBand.Ok;
        if (ratio >= BandLowRatio)
            return VitalProseBand.Low;
        return VitalProseBand.Critical;
    }

    public static float RemainingFoodDays(int storedKcal, float stomachKcal, PlayerNeedsSettings settings)
    {
        int burn = settings != null
            ? settings.DailyKcalBurn
            : PlayerNeedsSettings.DefaultDailyKcalBurn;
        if (burn <= 0)
            return 0f;

        float kcal = storedKcal + stomachKcal;
        if (kcal < 0f)
            kcal = 0f;
        return kcal / burn;
    }

    public static HungerDaysBand ResolveHungerDaysBand(float remainingDays, PlayerNeedsSettings settings)
    {
        float overate = settings != null
            ? settings.MoodOverateRatio
            : PlayerNeedsSettings.DefaultMoodOverateRatio;
        float proseFull = settings != null
            ? settings.ProseFullRatio
            : PlayerNeedsSettings.DefaultProseFullRatio;
        float proseOk = settings != null
            ? settings.ProseOkRatio
            : PlayerNeedsSettings.DefaultProseOkRatio;
        float proseLow = settings != null
            ? settings.ProseLowRatio
            : PlayerNeedsSettings.DefaultProseLowRatio;
        float empty = settings != null
            ? settings.MoodStomachEmptyRatio
            : PlayerNeedsSettings.DefaultMoodStomachEmptyRatio;
        int maxStored = settings != null
            ? settings.MaxStoredKcal
            : PlayerNeedsSettings.DefaultMaxStoredKcal;
        int burn = settings != null
            ? settings.DailyKcalBurn
            : PlayerNeedsSettings.DefaultDailyKcalBurn;
        float maxDays = burn > 0 ? maxStored / (float)burn : 0f;

        if (remainingDays >= maxDays * overate)
            return HungerDaysBand.Engorged;
        if (remainingDays >= proseFull)
            return HungerDaysBand.Sated;
        if (remainingDays >= proseOk)
            return HungerDaysBand.Hungry;
        if (remainingDays >= proseLow)
            return HungerDaysBand.VeryHungry;
        if (remainingDays >= empty)
            return HungerDaysBand.Famished;
        return HungerDaysBand.Starving;
    }

    public static ThirstNeedsBand ResolveThirstNeedsBand(int current, int max, PlayerNeedsSettings settings)
    {
        if (max <= 0)
            return ThirstNeedsBand.Parched;

        float ratio = (float)current / max;
        float quenched = settings != null
            ? settings.MoodThirstQuenchedRatio
            : PlayerNeedsSettings.DefaultMoodThirstQuenchedRatio;
        float thirsty = settings != null
            ? settings.MoodThirstyRatio
            : PlayerNeedsSettings.DefaultMoodThirstyRatio;
        float veryThirsty = settings != null
            ? settings.MoodVeryThirstyRatio
            : PlayerNeedsSettings.DefaultMoodVeryThirstyRatio;

        if (ratio >= quenched)
            return ThirstNeedsBand.Quenched;
        if (ratio >= thirsty)
            return ThirstNeedsBand.NotThirsty;
        if (ratio >= veryThirsty)
            return ThirstNeedsBand.Thirsty;
        if (current > 0)
            return ThirstNeedsBand.VeryThirsty;
        return ThirstNeedsBand.Parched;
    }

    public static string GetVitalShortKey(string vitalKey)
    {
        if (string.IsNullOrEmpty(vitalKey))
            return string.Empty;

        return vitalKey.StartsWith("Vital.", System.StringComparison.Ordinal)
            ? vitalKey.Substring("Vital.".Length)
            : vitalKey;
    }
}
