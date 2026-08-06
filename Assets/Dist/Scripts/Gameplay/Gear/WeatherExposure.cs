// ============================================================
// WeatherExposure — 날씨/바람이 BodyTemp ambient·wetness 압력에 반영되는 SSOT (Phase G)
// ============================================================

/// <summary>
/// Clear / Rain / Wind → ambient °C and wetness gain for BodyTemp / WearEnvExposure.
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
    /// <summary>Clear 환경 온도 — BodyTemp.BaseAmbientTempC와 동일 기준.</summary>
    public const float ClearAmbientTempC = BodyTemp.BaseAmbientTempC;

    /// <summary>Rain 환경 온도 (°C).</summary>
    public const float RainAmbientTempC = 10f;

    /// <summary>Wind 시 Clear ambient에서 차감하는 풍랭 (°C).</summary>
    public const float WindChillDegreesC = 4f;

    /// <summary>Clear: 습기 압력 없음 (World초당).</summary>
    public const float ClearWetnessGainPerSecond = 0f;

    /// <summary>Rain: 습기 압력 (World초당, env_prot=0 기준).</summary>
    public const float RainWetnessGainPerSecond = 0.02f;

    /// <summary>Wind: 약한 습기 압력 (먼/미스트 스탠드인).</summary>
    public const float WindWetnessGainPerSecond = 0.002f;

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

    public void Resolve()
    {
        AmbientTempC = ResolveAmbientTempC(_kind);
        AmbientWetnessGainPerSecond = ResolveWetnessGainPerSecond(_kind);
    }

    public static float ResolveAmbientTempC(WeatherKind kind)
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

    public static float ResolveWetnessGainPerSecond(WeatherKind kind)
    {
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
}
