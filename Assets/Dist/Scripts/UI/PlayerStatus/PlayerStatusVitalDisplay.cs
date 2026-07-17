// ============================================================
// PlayerStatusVitalDisplay — 바이탈 수치 노출 스킬 게이트 + 비율 밴드 SSOT
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

    public static string GetVitalShortKey(string vitalKey)
    {
        if (string.IsNullOrEmpty(vitalKey))
            return string.Empty;

        return vitalKey.StartsWith("Vital.", System.StringComparison.Ordinal)
            ? vitalKey.Substring("Vital.".Length)
            : vitalKey;
    }
}
