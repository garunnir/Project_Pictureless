// ============================================================
// BodyTemp — 착용 warmth가 체온 목표/수렴에 반영되는 SSOT (Phase F)
// ============================================================

using UnityEngine;

/// <summary>
/// Worn TotalWarmth → body temperature toward ambient+insulation target.
/// Ambient °C from WeatherExposure (Phase G); BaseAmbientTempC = Clear fallback.
/// Optional wetness cool. Docs: docs/equipment/GEAR.md Phase F/G · dt = World.
/// </summary>
public sealed class BodyTemp
{
    /// <summary>편안 체온 (°C).</summary>
    public const float ComfortBodyTempC = 37f;

    /// <summary>체온 하한 (°C).</summary>
    public const float BodyTempMinC = 27f;

    /// <summary>체온 상한 (°C).</summary>
    public const float BodyTempMaxC = 43f;

    /// <summary>Clear 날씨 기본 환경 온도 (°C). WeatherExposure.ClearAmbientTempC와 동일.</summary>
    public const float BaseAmbientTempC = 18f;

    /// <summary>TotalWarmth 1포인트당 목표 온도 상승 (°C).</summary>
    public const float DegreesPerWarmthPoint = 0.5f;

    /// <summary>Wetness01=1일 때 목표 온도 감소 (°C). 전투/습윤 연동 아님.</summary>
    public const float WetnessCoolDegreesC = 2f;

    /// <summary>현재→목표 수렴 속도 (World초당 비율).</summary>
    public const float ConvergencePerSecond = 0.08f;

    /// <summary>편안 밴드 하한 (°C) — Comfort ± ComfortBandHalfWidth.</summary>
    public const float ComfortBandHalfWidthC = 1f;

    float _bodyTempC = ComfortBodyTempC;

    /// <summary>현재 체온 (°C).</summary>
    public float BodyTempC => _bodyTempC;

    /// <summary>직전 Tick의 목표 체온 (°C).</summary>
    public float TargetTempC { get; private set; } = ComfortBodyTempC;

    /// <summary>직전 Tick에 사용한 TotalWarmth.</summary>
    public int LastTotalWarmth { get; private set; }

    public void Reset(float bodyTempC = ComfortBodyTempC)
    {
        _bodyTempC = Mathf.Clamp(bodyTempC, BodyTempMinC, BodyTempMaxC);
        TargetTempC = _bodyTempC;
        LastTotalWarmth = 0;
    }

    /// <summary>
    /// Target = ambient + warmth×DegreesPerWarmth − wetness×WetnessCool.
    /// BodyTemp += (Target − BodyTemp) × Convergence × dt.
    /// </summary>
    public void Tick(
        float deltaSeconds,
        int totalWarmth,
        float wetness01 = 0f,
        float ambientTempC = BaseAmbientTempC)
    {
        if (deltaSeconds <= 0f)
            return;

        LastTotalWarmth = Mathf.Max(0, totalWarmth);
        float wet = Mathf.Clamp01(wetness01);
        TargetTempC = ComputeTargetTempC(LastTotalWarmth, wet, ambientTempC);

        float next = _bodyTempC + (TargetTempC - _bodyTempC) * ConvergencePerSecond * deltaSeconds;
        _bodyTempC = Mathf.Clamp(next, BodyTempMinC, BodyTempMaxC);
    }

    public static float ComputeTargetTempC(
        int totalWarmth,
        float wetness01,
        float ambientTempC = BaseAmbientTempC)
    {
        float warmth = Mathf.Max(0, totalWarmth);
        float wet = Mathf.Clamp01(wetness01);
        float target = ambientTempC
                       + warmth * DegreesPerWarmthPoint
                       - wet * WetnessCoolDegreesC;
        return Mathf.Clamp(target, BodyTempMinC, BodyTempMaxC);
    }

    /// <summary>표시용 소수 1자리 체온.</summary>
    public float BodyTempDisplayC => Mathf.Round(_bodyTempC * 10f) * 0.1f;

    /// <summary>표시용 소수 1자리 목표.</summary>
    public float TargetTempDisplayC => Mathf.Round(TargetTempC * 10f) * 0.1f;

    /// <summary>0.1°C 단위 정수 (Changed 스로틀용).</summary>
    public int BodyTempTenths => Mathf.RoundToInt(_bodyTempC * 10f);

    public BodyTempFeeling Feeling => ClassifyFeeling(_bodyTempC);

    public static BodyTempFeeling ClassifyFeeling(float bodyTempC)
    {
        float comfortLo = ComfortBodyTempC - ComfortBandHalfWidthC;
        float comfortHi = ComfortBodyTempC + ComfortBandHalfWidthC;
        if (bodyTempC < comfortLo - ComfortBandHalfWidthC * 2f)
            return BodyTempFeeling.Cold;
        if (bodyTempC < comfortLo)
            return BodyTempFeeling.Cool;
        if (bodyTempC <= comfortHi)
            return BodyTempFeeling.Comfortable;
        if (bodyTempC <= comfortHi + ComfortBandHalfWidthC * 2f)
            return BodyTempFeeling.Warm;
        return BodyTempFeeling.Hot;
    }
}

public enum BodyTempFeeling
{
    Cold = 0,
    Cool = 1,
    Comfortable = 2,
    Warm = 3,
    Hot = 4
}
