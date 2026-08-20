// ============================================================
// PlayerStatusLabels — 상태창·부위·효과·상세 박스 문구 SSOT
// ============================================================

public static class PlayerStatusLabels
{
    const string KeyTitle = "PlayerStatus.Title";
    const string KeyVitalsSection = "PlayerStatus.VitalsSection";
    const string KeySkillsSection = "PlayerStatus.SkillsSection";
    const string KeyDetailHeader = "PlayerStatus.DetailHeader";
    const string KeyDetailSubparts = "PlayerStatus.DetailSubparts";
    const string KeyDetailEffects = "PlayerStatus.DetailEffects";
    const string KeyNoEffects = "PlayerStatus.NoEffects";
    const string KeyLost = "PlayerStatus.Lost";
    const string KeyConditionFormat = "PlayerStatus.ConditionFormat";
    const string KeyVitalFormat = "PlayerStatus.VitalFormat";
    const string KeySkillFormat = "PlayerStatus.SkillFormat";
    const string KeyPartPrefix = "PlayerStatus.Part.";
    const string KeyVitalPrefix = "PlayerStatus.Vital.";
    const string KeyVitalProsePrefix = "PlayerStatus.VitalProse.";
    const string KeySkillPrefix = "PlayerStatus.Skill.";
    const string KeyEffectPrefix = "PlayerStatus.Effect.";
    const string KeyDebugSeverArmL = "PlayerStatus.DebugSeverArmL";

    public static string Title => Loc.Get(KeyTitle);
    public static string VitalsSection => Loc.Get(KeyVitalsSection);
    public static string SkillsSection => Loc.Get(KeySkillsSection);
    public static string DetailHeader => Loc.Get(KeyDetailHeader);
    public static string DetailSubparts => Loc.Get(KeyDetailSubparts);
    public static string DetailEffects => Loc.Get(KeyDetailEffects);
    public static string NoEffects => Loc.Get(KeyNoEffects);
    public static string Lost => Loc.Get(KeyLost);
    public static string DebugSeverArmL => Loc.Get(KeyDebugSeverArmL);

    public static string FormatCondition(int cur, int max) =>
        Loc.Format(KeyConditionFormat, cur, max);

    public static string FormatVital(int cur, int max) => Loc.Format(KeyVitalFormat, cur, max);

    public static string FormatVitalProse(string vitalKey, int cur, int max)
    {
        string shortKey = PlayerStatusVitalDisplay.GetVitalShortKey(vitalKey);
        if (string.IsNullOrEmpty(shortKey))
            return string.Empty;

        PlayerStatusVitalDisplay.VitalProseBand band =
            PlayerStatusVitalDisplay.ResolveBand(cur, max);
        string key = $"{KeyVitalProsePrefix}{shortKey}.{band}";
        return Loc.TryGet(key, out string prose) ? prose : string.Empty;
    }

    public static string FormatSkill(string skillId, int level) =>
        Loc.Format(KeySkillFormat, GetSkillName(skillId), level);

    public static string GetSkillName(string skillId)
    {
        if (string.IsNullOrEmpty(skillId))
            return string.Empty;

        return Loc.TryGet(KeySkillPrefix + skillId, out string name) ? name : skillId;
    }

    public static string GetPartName(string partId)
    {
        if (string.IsNullOrEmpty(partId))
            return string.Empty;

        return Loc.TryGet(KeyPartPrefix + partId, out string name) ? name : partId;
    }

    public static string GetVitalName(string vitalKey)
    {
        if (string.IsNullOrEmpty(vitalKey))
            return string.Empty;

        string shortKey = PlayerStatusVitalDisplay.GetVitalShortKey(vitalKey);
        return Loc.TryGet(KeyVitalPrefix + shortKey, out string name) ? name : vitalKey;
    }

    public static string GetEffectName(string effectId)
    {
        if (string.IsNullOrEmpty(effectId))
            return string.Empty;

        return Loc.TryGet(KeyEffectPrefix + effectId, out string name) ? name : effectId;
    }

    public static string GetEncumbranceTooltip(PlayerEncumbranceStage stage)
    {
        if (stage == PlayerEncumbranceStage.None)
            return string.Empty;

        string key = "PlayerStatus.Mood.Overencumbered." + stage;
        if (Loc.TryGet(key, out string text))
            return text;

        return Loc.TryGet("PlayerStatus.Mood.Overencumbered", out string fallback)
            ? fallback
            : string.Empty;
    }

    public static string GetPainTooltip(bool severe)
    {
        string key = severe ? "PlayerStatus.Mood.SeverePain" : "PlayerStatus.Mood.Pain";
        if (Loc.TryGet(key, out string text))
            return text;
        return severe ? "극심한 고통" : "고통";
    }
}
