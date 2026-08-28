// ============================================================
// PlayerEncumbrance — 플레이어 몸통 과적 단계·페널티 SSOT
// ============================================================

using Garunnir.Runtime.Gameplay.Data;

public enum PlayerEncumbranceStage
{
    None = 0,
    Light = 1,
    Medium = 2,
    Heavy = 3,
    Extreme = 4
}

public static class PlayerEncumbrance
{
    public const float LightRatioMax = 1.25f;
    public const float MediumRatioMax = 1.5f;
    public const float HeavyRatioMax = 2f;

    public const float NoneSpeedMult = 1f;
    public const float LightSpeedMult = 0.85f;
    public const float MediumSpeedMult = 0.7f;
    public const float HeavySpeedMult = 0.5f;
    public const float ExtremeSpeedMult = 0f;

    public const float LightMoodIntensity = 0.35f;
    public const float MediumMoodIntensity = 0.55f;
    public const float HeavyMoodIntensity = 0.75f;
    public const float ExtremeMoodIntensity = 1f;

    public static PlayerEncumbranceStage ResolveStage(float usedWeight, float maxWeight)
    {
        if (maxWeight <= 0f)
            return PlayerEncumbranceStage.Extreme;

        float ratio = usedWeight / maxWeight;
        if (ratio <= 1f)
            return PlayerEncumbranceStage.None;
        if (ratio <= LightRatioMax)
            return PlayerEncumbranceStage.Light;
        if (ratio <= MediumRatioMax)
            return PlayerEncumbranceStage.Medium;
        if (ratio <= HeavyRatioMax)
            return PlayerEncumbranceStage.Heavy;
        return PlayerEncumbranceStage.Extreme;
    }

    public static float GetMoveSpeedMultiplier(PlayerEncumbranceStage stage) =>
        stage switch
        {
            PlayerEncumbranceStage.Light => LightSpeedMult,
            PlayerEncumbranceStage.Medium => MediumSpeedMult,
            PlayerEncumbranceStage.Heavy => HeavySpeedMult,
            PlayerEncumbranceStage.Extreme => ExtremeSpeedMult,
            _ => NoneSpeedMult
        };

    public static bool BlocksSprint(PlayerEncumbranceStage stage) =>
        stage >= PlayerEncumbranceStage.Heavy;

    public static bool BlocksMovement(PlayerEncumbranceStage stage) =>
        stage == PlayerEncumbranceStage.Extreme;

    public static float GetMoodIntensity(PlayerEncumbranceStage stage) =>
        stage switch
        {
            PlayerEncumbranceStage.Light => LightMoodIntensity,
            PlayerEncumbranceStage.Medium => MediumMoodIntensity,
            PlayerEncumbranceStage.Heavy => HeavyMoodIntensity,
            PlayerEncumbranceStage.Extreme => ExtremeMoodIntensity,
            _ => 0f
        };

    public static void CollectSkillModifiers(
        PlayerEncumbranceStage stage,
        System.Collections.Generic.Dictionary<string, int> into)
    {
        if (into == null || stage == PlayerEncumbranceStage.None)
            return;

        int strDelta = 0;
        int dexDelta = 0;
        switch (stage)
        {
            case PlayerEncumbranceStage.Light:
                dexDelta = -1;
                break;
            case PlayerEncumbranceStage.Medium:
                strDelta = -1;
                dexDelta = -1;
                break;
            case PlayerEncumbranceStage.Heavy:
                strDelta = -2;
                dexDelta = -2;
                break;
            case PlayerEncumbranceStage.Extreme:
                strDelta = -3;
                dexDelta = -3;
                break;
        }

        SkillModifierCollect.AddDelta(into, AttributeIds.Str, strDelta);
        SkillModifierCollect.AddDelta(into, AttributeIds.Dex, dexDelta);
    }
}
