// ============================================================
// GearEnvPenalties — BodyTemp / wetness → move·combat 배율 SSOT
// ============================================================

using UnityEngine;

/// <summary>
/// Feeling bands + Wetness01 → movement / HitChance factors.
/// Wired by PlayerGearHost (move) and CharacterAttacker (accuracy).
/// Docs: docs/equipment/GEAR.md Phase H.
/// </summary>
public static class GearEnvPenalties
{
    /// <summary>Cold feeling 이동 배율.</summary>
    public const float ColdMoveFactor = 0.88f;

    /// <summary>Cool feeling 이동 배율.</summary>
    public const float CoolMoveFactor = 0.95f;

    /// <summary>Comfortable feeling 이동 배율.</summary>
    public const float ComfortableMoveFactor = 1f;

    /// <summary>Warm feeling 이동 배율.</summary>
    public const float WarmMoveFactor = 0.97f;

    /// <summary>Hot feeling 이동 배율.</summary>
    public const float HotMoveFactor = 0.9f;

    /// <summary>Wetness01=1일 때 이동 추가 감소 (× (1 − this)).</summary>
    public const float WetnessMovePenaltyPerUnit = 0.15f;

    /// <summary>Cold feeling HitChance 배율.</summary>
    public const float ColdHitFactor = 0.9f;

    /// <summary>Cool feeling HitChance 배율.</summary>
    public const float CoolHitFactor = 0.96f;

    /// <summary>Comfortable feeling HitChance 배율.</summary>
    public const float ComfortableHitFactor = 1f;

    /// <summary>Warm feeling HitChance 배율.</summary>
    public const float WarmHitFactor = 0.97f;

    /// <summary>Hot feeling HitChance 배율.</summary>
    public const float HotHitFactor = 0.92f;

    /// <summary>Wetness01=1일 때 HitChance 추가 감소 (× (1 − this)).</summary>
    public const float WetnessHitPenaltyPerUnit = 0.1f;

    /// <summary>
    /// MoveSpeedFactor = FeelingMove × (1 − Wetness01 × WetnessMovePenaltyPerUnit).
    /// </summary>
    public static float MoveSpeedFactor(BodyTempFeeling feeling, float wetness01)
    {
        float wet = Mathf.Clamp01(wetness01);
        float feelingFactor = FeelingMoveFactor(feeling);
        float wetFactor = 1f - wet * WetnessMovePenaltyPerUnit;
        return Mathf.Clamp01(feelingFactor * wetFactor);
    }

    /// <summary>
    /// HitAccuracyFactor = FeelingHit × (1 − Wetness01 × WetnessHitPenaltyPerUnit).
    /// </summary>
    public static float HitAccuracyFactor(BodyTempFeeling feeling, float wetness01)
    {
        float wet = Mathf.Clamp01(wetness01);
        float feelingFactor = FeelingHitFactor(feeling);
        float wetFactor = 1f - wet * WetnessHitPenaltyPerUnit;
        return Mathf.Clamp01(feelingFactor * wetFactor);
    }

    public static float FeelingMoveFactor(BodyTempFeeling feeling)
    {
        switch (feeling)
        {
            case BodyTempFeeling.Cold:
                return ColdMoveFactor;
            case BodyTempFeeling.Cool:
                return CoolMoveFactor;
            case BodyTempFeeling.Warm:
                return WarmMoveFactor;
            case BodyTempFeeling.Hot:
                return HotMoveFactor;
            default:
                return ComfortableMoveFactor;
        }
    }

    public static float FeelingHitFactor(BodyTempFeeling feeling)
    {
        switch (feeling)
        {
            case BodyTempFeeling.Cold:
                return ColdHitFactor;
            case BodyTempFeeling.Cool:
                return CoolHitFactor;
            case BodyTempFeeling.Warm:
                return WarmHitFactor;
            case BodyTempFeeling.Hot:
                return HotHitFactor;
            default:
                return ComfortableHitFactor;
        }
    }
}
