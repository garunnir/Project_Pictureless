// ============================================================
// BodyLocomotionPenalties — 절단 절뚝 이동 배율 SSOT (GearEnv와 곱함)
// ============================================================

using Garunnir.Runtime.Gameplay.Data;

/// <summary>
/// Missing thigh/foot limp. Combined with GearEnvPenalties (core Feeling + wetness)
/// at CharacterClimateHost → CharacterMotor / PlayerMovement.SetEnvMovement.
/// Thigh missing implies that side's foot is gone — do not stack foot on the same side.
/// </summary>
public static class BodyLocomotionPenalties
{
    /// <summary>사지 온전.</summary>
    public const float IntactMoveFactor = 1f;

    /// <summary>대퇴 한쪽 없음 (그 쪽 발 페널티는 적용하지 않음).</summary>
    public const float MissingThighMoveFactor = 0.5f;

    /// <summary>발 한쪽 없음 (대퇴는 있음).</summary>
    public const float MissingFootMoveFactor = 0.8f;

    public static float MoveSpeedFactor(ICharacterBody body)
    {
        if (body == null)
            return IntactMoveFactor;

        float factor = IntactMoveFactor;
        bool missingThighL = !body.Has(BodyPartIds.ThighL);
        bool missingThighR = !body.Has(BodyPartIds.ThighR);

        if (missingThighL)
            factor *= MissingThighMoveFactor;
        else if (!body.Has(BodyPartIds.FootL))
            factor *= MissingFootMoveFactor;

        if (missingThighR)
            factor *= MissingThighMoveFactor;
        else if (!body.Has(BodyPartIds.FootR))
            factor *= MissingFootMoveFactor;

        return factor;
    }

    public static float CombinedMoveSpeedFactor(
        ICharacterBody body,
        BodyTempFeeling feeling,
        float wetness01)
    {
        return GearEnvPenalties.MoveSpeedFactor(feeling, wetness01)
               * MoveSpeedFactor(body);
    }
}
