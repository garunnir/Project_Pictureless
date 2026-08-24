// ============================================================
// WearEnvExposure — env_prot가 wetness/환경 노출 증가율을 줄이는 SSOT (Phase E)
// ============================================================

using UnityEngine;

/// <summary>
/// Wear environmental_protection → wetness gain.
/// Ambient wetness rate from WeatherExposure (Phase G); BaseAmbient = Clear/legacy fallback.
/// Docs: docs/equipment/GEAR.md Phase E/G.
/// </summary>
public sealed class WearEnvExposure
{
    /// <summary>Wetness 하한 (건조).</summary>
    public const float WetnessMin = 0f;

    /// <summary>Wetness 상한 (완전 젖음).</summary>
    public const float WetnessMax = 1f;

    /// <summary>
    /// Legacy/Clear 미지정 시 기본 습기 압력 — World초당 (env_prot=0).
    /// Host는 WeatherExposure.ResolveWetnessGain을 넘기는 것이 정식 경로.
    /// </summary>
    public const float BaseAmbientWetnessGainPerSecond = 0.005f;

    /// <summary>env_prot 1포인트당 노출 배율 감소.</summary>
    public const float EnvProtWetnessReductionPerPoint = 0.05f;

    /// <summary>ExposureFactor 최대 감소 (0.95 = 최소 5% 스며듦).</summary>
    public const float EnvProtWetnessReductionCap = 0.95f;

    /// <summary>
    /// ExposureFactor가 낮을 때 World초당 건조량 (완전 방호 시 최대).
    /// </summary>
    public const float BaseDryRatePerSecond = 0.01f;

    float _wetness;

    /// <summary>0..1 wetness.</summary>
    public float Wetness01 => _wetness;

    /// <summary>직전 Tick의 노출 배율 (1=완전 노출, 0에 가까울수록 방호).</summary>
    public float ExposureFactor { get; private set; } = 1f;

    /// <summary>직전 Tick에 사용한 TotalEnvironmentalProtection.</summary>
    public int LastEnvProtection { get; private set; }

    public void Reset(float wetness01 = 0f)
    {
        _wetness = Mathf.Clamp(wetness01, WetnessMin, WetnessMax);
        ExposureFactor = 1f;
        LastEnvProtection = 0;
    }

    /// <summary>디버그/치트용. 노출 배율·env_prot는 유지하고 wetness만 설정.</summary>
    public void SetWetness01(float wetness01)
    {
        _wetness = Mathf.Clamp(wetness01, WetnessMin, WetnessMax);
    }

    /// <summary>
    /// ExposureFactor = 1 − min(env × PerPoint, Cap).
    /// Wetness += (ambientGain × ExposureFactor − BaseDry × (1 − ExposureFactor)) × dt.
    /// </summary>
    public void Tick(
        float deltaSeconds,
        int totalEnvironmentalProtection,
        float ambientWetnessGainPerSecond = BaseAmbientWetnessGainPerSecond)
    {
        if (deltaSeconds <= 0f)
            return;

        LastEnvProtection = Mathf.Max(0, totalEnvironmentalProtection);
        ExposureFactor = ComputeExposureFactor(LastEnvProtection);

        float ambientGain = Mathf.Max(0f, ambientWetnessGainPerSecond);
        float gain = ambientGain * ExposureFactor;
        float dry = BaseDryRatePerSecond * (1f - ExposureFactor);
        _wetness = Mathf.Clamp(_wetness + (gain - dry) * deltaSeconds, WetnessMin, WetnessMax);
    }

    public static float ComputeExposureFactor(int totalEnvironmentalProtection)
    {
        if (totalEnvironmentalProtection <= 0)
            return 1f;

        float reduction = totalEnvironmentalProtection * EnvProtWetnessReductionPerPoint;
        if (reduction > EnvProtWetnessReductionCap)
            reduction = EnvProtWetnessReductionCap;
        return Mathf.Clamp01(1f - reduction);
    }

    /// <summary>표시용 0..100 정수 wetness.</summary>
    public int WetnessPercent => Mathf.RoundToInt(_wetness * 100f);
}
