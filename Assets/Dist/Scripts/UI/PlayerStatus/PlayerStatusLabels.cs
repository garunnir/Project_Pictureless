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
    const string KeyProsthetic = "PlayerStatus.Kind.Prosthetic";
    const string KeyConditionFormat = "PlayerStatus.ConditionFormat";
    const string KeyVitalFormat = "PlayerStatus.VitalFormat";
    const string KeySkillFormat = "PlayerStatus.SkillFormat";
    const string KeyPartPrefix = "PlayerStatus.Part.";
    const string KeyVitalPrefix = "PlayerStatus.Vital.";
    const string KeyVitalProsePrefix = "PlayerStatus.VitalProse.";
    const string KeySkillPrefix = "PlayerStatus.Skill.";
    const string KeyEffectPrefix = "PlayerStatus.Effect.";
    const string KeyBandageDirtyFormat = "PlayerStatus.BandageDirtyFormat";
    const string KeyDebugSeverArmL = "PlayerStatus.DebugSeverArmL";
    const string KeyBleedDrainRateFormat = "PlayerStatus.Bleed.DrainRateFormat";
    const string KeyBleedEtaFormat = "PlayerStatus.Bleed.EtaFormat";
    const string KeyBleedBandagedBlock = "PlayerStatus.Bleed.BandagedBlock";
    const string KeyBleedProsePrefix = "PlayerStatus.Bleed.Prose.";
    const string KeyBleedVitalsNumeric = "PlayerStatus.Bleed.VitalsNumeric";
    const string KeyBleedDurationMinutes = "PlayerStatus.Bleed.DurationMinutes";
    const string KeyBleedDurationSeconds = "PlayerStatus.Bleed.DurationSeconds";

    public static string Title => Loc.Get(KeyTitle);
    public static string VitalsSection => Loc.Get(KeyVitalsSection);
    public static string SkillsSection => Loc.Get(KeySkillsSection);
    public static string DetailHeader => Loc.Get(KeyDetailHeader);
    public static string DetailSubparts => Loc.Get(KeyDetailSubparts);
    public static string DetailEffects => Loc.Get(KeyDetailEffects);
    public static string NoEffects => Loc.Get(KeyNoEffects);
    public static string Lost => Loc.Get(KeyLost);
    public static string Prosthetic =>
        Loc.TryGet(KeyProsthetic, out string text) ? text : "의체";
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

    public static string FormatHungerDaysProse(int storedKcal, float stomachKcal, PlayerNeedsSettings settings)
    {
        float days = PlayerStatusVitalDisplay.RemainingFoodDays(storedKcal, stomachKcal, settings);
        PlayerStatusVitalDisplay.HungerDaysBand band =
            PlayerStatusVitalDisplay.ResolveHungerDaysBand(days, settings);
        string key = KeyVitalProsePrefix + "Hunger." + band;
        return Loc.TryGet(key, out string prose) ? prose : string.Empty;
    }

    public static string FormatThirstNeedsProse(int cur, int max, PlayerNeedsSettings settings)
    {
        PlayerStatusVitalDisplay.ThirstNeedsBand band =
            PlayerStatusVitalDisplay.ResolveThirstNeedsBand(cur, max, settings);
        string key = KeyVitalProsePrefix + "Thirst." + band;
        return Loc.TryGet(key, out string prose) ? prose : string.Empty;
    }

    public static string GetMoodTooltip(Garunnir.Runtime.Gameplay.Data.MoodIconId iconId)
    {
        string key = "PlayerStatus.Mood." + iconId;
        if (Loc.TryGet(key, out string text))
            return text;
        return iconId.ToString();
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

    public static string FormatBandageDirty(float dirty01)
    {
        int percent = dirty01 <= 0f
            ? 0
            : dirty01 >= 1f
                ? 100
                : (int)(dirty01 * 100f + 0.5f);
        return Loc.Format(KeyBandageDirtyFormat, percent);
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

    public static string GetOffBalanceTooltip(bool fallen)
    {
        string key = fallen ? "PlayerStatus.Mood.OffBalance.Fallen" : "PlayerStatus.Mood.OffBalance";
        if (Loc.TryGet(key, out string text))
            return text;
        return fallen ? "중심을 잃고 쓰러졌다" : "중심이 흔들린다";
    }

    public static string GetBloodTooltip(bool critical, PlayerStatusBleedSnapshot bleed, bool showNumeric)
    {
        string headline = GetBloodTooltip(critical);
        return AppendBleedDetails(headline, bleed, showNumeric);
    }

    public static string FormatBleedTooltip(PlayerStatusBleedSnapshot bleed, bool showNumeric)
    {
        string headline = GetEffectName(Garunnir.Runtime.Gameplay.Data.BodyPartEffectIds.Bleed);
        return AppendBleedDetails(headline, bleed, showNumeric);
    }

    public static string FormatBleedVitalsLine(PlayerStatusBleedSnapshot bleed, bool showNumeric)
    {
        if (!bleed.HasAnyBleed)
            return string.Empty;

        if (showNumeric)
        {
            if (!bleed.HasOpenDrain)
                return Loc.TryGet(KeyBleedBandagedBlock, out string blocked) ? blocked : "붕대로 출혈이 막혀 있다";

            float percentPerSec = bleed.OpenDrainPerSecond * 100f;
            return Loc.Format(
                KeyBleedVitalsNumeric,
                percentPerSec.ToString("0.##"),
                FormatBleedDuration(bleed.SecondsToEmpty));
        }

        return FormatBleedProse(bleed);
    }

    public static string FormatBleedDuration(float seconds)
    {
        if (seconds <= 0f || float.IsInfinity(seconds) || float.IsNaN(seconds))
            return "-";

        if (seconds >= 60f)
        {
            int minutes = (int)(seconds / 60f);
            int secs = (int)(seconds % 60f + 0.5f);
            if (secs >= 60)
            {
                minutes++;
                secs = 0;
            }

            return Loc.Format(KeyBleedDurationMinutes, minutes, secs);
        }

        int wholeSeconds = seconds < 1f ? 1 : (int)(seconds + 0.5f);
        return Loc.Format(KeyBleedDurationSeconds, wholeSeconds);
    }

    static string AppendBleedDetails(string headline, PlayerStatusBleedSnapshot bleed, bool showNumeric)
    {
        if (!bleed.HasAnyBleed)
            return headline;

        if (showNumeric)
            return AppendNumericBleedDetails(headline, bleed);

        string prose = FormatBleedProse(bleed);
        if (string.IsNullOrEmpty(prose))
            return headline;

        return string.IsNullOrEmpty(headline) ? prose : headline + "\n" + prose;
    }

    static string AppendNumericBleedDetails(string headline, PlayerStatusBleedSnapshot bleed)
    {
        if (!bleed.HasOpenDrain)
        {
            string blocked = Loc.TryGet(KeyBleedBandagedBlock, out string text)
                ? text
                : "붕대로 출혈이 막혀 있다";
            return string.IsNullOrEmpty(headline) ? blocked : headline + "\n" + blocked;
        }

        float percentPerSec = bleed.OpenDrainPerSecond * 100f;
        string drainLine = Loc.Format(KeyBleedDrainRateFormat, percentPerSec.ToString("0.##"));
        string etaLine = Loc.Format(KeyBleedEtaFormat, FormatBleedDuration(bleed.SecondsToEmpty));
        string details = drainLine + "\n" + etaLine;
        return string.IsNullOrEmpty(headline) ? details : headline + "\n" + details;
    }

    static string FormatBleedProse(PlayerStatusBleedSnapshot bleed)
    {
        PlayerStatusBleedDisplay.ProseBand band =
            PlayerStatusBleedDisplay.ResolveProseBand(bleed.OpenDrainPerSecond, bleed.HasOpenDrain);
        string key = KeyBleedProsePrefix + band;
        if (Loc.TryGet(key, out string prose))
            return prose;

        return band switch
        {
            PlayerStatusBleedDisplay.ProseBand.Bandaged => "붕대로 출혈이 막혀 있다",
            PlayerStatusBleedDisplay.ProseBand.Severe => "피가 빠르게 줄어든다",
            PlayerStatusBleedDisplay.ProseBand.Moderate => "피가 계속 빠진다",
            _ => "피가 서서히 빠진다",
        };
    }

    public static string GetBloodTooltip(bool critical)
    {
        string key = critical ? "PlayerStatus.Mood.Pale.Critical" : "PlayerStatus.Mood.Pale";
        if (Loc.TryGet(key, out string text))
            return text;
        return critical ? "과다출혈로 쓰러질 것 같다" : "핏기가 없다";
    }

    public static string GetConsciousnessTooltip(bool downed, bool fatal)
    {
        string key = fatal
            ? "PlayerStatus.Mood.Fading.Fatal"
            : downed
                ? "PlayerStatus.Mood.Fading.Downed"
                : "PlayerStatus.Mood.Fading";
        if (Loc.TryGet(key, out string text))
            return text;
        if (fatal)
            return "의식이 끊겼다";
        if (downed)
            return "의식이 가물거린다";
        return "의식이 흐릿하다";
    }

    public static string GetStatCollapseTooltip()
    {
        const string key = "PlayerStatus.Mood.StatCollapse";
        if (Loc.TryGet(key, out string text))
            return text;
        return "정신이 무너졌다";
    }
}
