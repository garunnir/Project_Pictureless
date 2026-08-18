// ============================================================
// WeatherExposure — 날씨/바람/기간/실내외가 BodyTemp ambient·wetness 압력에 반영되는 SSOT (Phase G)
// ============================================================

/// <summary>
/// Clear / Rain / Wind → ambient °C and wetness gain for BodyTemp / WearEnvExposure.
/// Outdoor day Clear stays <see cref="ClearAmbientTempC"/> (~18°C). Indoor ignores rain/wind.
/// Docs: docs/equipment/GEAR.md Phase G.
/// </summary>
public enum WeatherKind
{
    Clear = 0,
    Rain = 1,
    Wind = 2
}

public sealed class WeatherExposure
{
    /// <summary>Clear 야외 Day 환경 온도 — BodyTemp.BaseAmbientTempC와 동일 기준.</summary>
    public const float ClearAmbientTempC = BodyTemp.BaseAmbientTempC;

    /// <summary>Rain 야외 환경 온도 (°C).</summary>
    public const float RainAmbientTempC = 10f;

    /// <summary>Wind 시 Clear ambient에서 차감하는 풍랭 (°C).</summary>
    public const float WindChillDegreesC = 4f;

    /// <summary>야외 Night: kind ambient에 더하는 오프셋 (°C).</summary>
    public const float NightAmbientOffsetC = -6f;

    /// <summary>야외 Dawn: kind ambient에 더하는 오프셋 (°C).</summary>
    public const float DawnAmbientOffsetC = -3f;

    /// <summary>실내 환경 온도 (°C). 비/바람·기간 오프셋 무시.</summary>
    public const float IndoorAmbientTempC = ClearAmbientTempC;

    /// <summary>Clear: 습기 압력 없음 (World초당).</summary>
    public const float ClearWetnessGainPerSecond = 0f;

    /// <summary>Rain: 습기 압력 (World초당, env_prot=0 기준).</summary>
    public const float RainWetnessGainPerSecond = 0.02f;

    /// <summary>Wind: 약한 습기 압력 (먼/미스트 스탠드인).</summary>
    public const float WindWetnessGainPerSecond = 0.002f;

    /// <summary>실내 습기 압력 (World초당). 비/바람 무시.</summary>
    public const float IndoorWetnessGainPerSecond = 0f;

    WeatherKind _kind = WeatherKind.Clear;

    public WeatherKind Kind
    {
        get => _kind;
        set => _kind = value;
    }

    /// <summary>직전 Resolve의 ambient °C.</summary>
    public float AmbientTempC { get; private set; } = ClearAmbientTempC;

    /// <summary>직전 Resolve의 wetness gain/s.</summary>
    public float AmbientWetnessGainPerSecond { get; private set; } = ClearWetnessGainPerSecond;

    public void SetKind(WeatherKind kind)
    {
        _kind = kind;
        Resolve();
    }

    /// <summary>레거시: kind만. Day + outdoor true (Clear ≈ 18°C).</summary>
    public void Resolve()
    {
        Resolve(_kind, DayPeriod.Day, outdoor: true);
    }

    public void Resolve(WeatherKind kind, DayPeriod period, bool outdoor)
    {
        _kind = kind;
        AmbientTempC = ResolveAmbientTempC(kind, period, outdoor);
        AmbientWetnessGainPerSecond = ResolveWetnessGainPerSecond(kind, outdoor);
    }

    /// <summary>레거시: kind만. Day + outdoor true.</summary>
    public static float ResolveAmbientTempC(WeatherKind kind) =>
        ResolveAmbientTempC(kind, DayPeriod.Day, outdoor: true);

    public static float ResolveAmbientTempC(WeatherKind kind, DayPeriod period, bool outdoor)
    {
        if (!outdoor)
            return IndoorAmbientTempC;

        return ResolveKindAmbientTempC(kind) + ResolvePeriodOffsetC(period);
    }

    public static float ResolvePeriodOffsetC(DayPeriod period)
    {
        switch (period)
        {
            case DayPeriod.Night:
                return NightAmbientOffsetC;
            case DayPeriod.Dawn:
                return DawnAmbientOffsetC;
            default:
                return 0f;
        }
    }

    /// <summary>레거시: kind만. outdoor true.</summary>
    public static float ResolveWetnessGainPerSecond(WeatherKind kind) =>
        ResolveWetnessGainPerSecond(kind, outdoor: true);

    public static float ResolveWetnessGainPerSecond(WeatherKind kind, bool outdoor)
    {
        if (!outdoor)
            return IndoorWetnessGainPerSecond;

        switch (kind)
        {
            case WeatherKind.Rain:
                return RainWetnessGainPerSecond;
            case WeatherKind.Wind:
                return WindWetnessGainPerSecond;
            default:
                return ClearWetnessGainPerSecond;
        }
    }

    public static string KindLabel(WeatherKind kind)
    {
        switch (kind)
        {
            case WeatherKind.Rain:
                return "Rain";
            case WeatherKind.Wind:
                return "Wind";
            default:
                return "Clear";
        }
    }

    static float ResolveKindAmbientTempC(WeatherKind kind)
    {
        switch (kind)
        {
            case WeatherKind.Rain:
                return RainAmbientTempC;
            case WeatherKind.Wind:
                return ClearAmbientTempC - WindChillDegreesC;
            default:
                return ClearAmbientTempC;
        }
    }
}
