// ============================================================
// CombatImbalance — 불균형 0..1 / 이속·자빠짐·HUD SSOT (Pain·밀침 J와 분리)
// ============================================================

/// <summary>
/// 피격 Δv로 쌓이고 시간에 회복. 이속·HitChance × (1 − Imbalance). 1 + 능동 속도면 자빠짐.
/// 문서: docs/locomotion/LOCOMOTION.md.
/// </summary>
public static class CombatImbalance
{
    /// <summary>초당 불균형 회복량.</summary>
    public const float RecoverPerSecond = 0.4f;

    /// <summary>자빠짐: 능동 |CurrentSpeed| 하한 (m/s). 넉백만은 제외.</summary>
    public const float FallSpeedMin = 2f;

    /// <summary>HUD OffBalance 아이콘 하한.</summary>
    public const float HudMin = 0.15f;

    /// <summary>무드 intensity 버킷 (ViewModel 리빌드 스로틀).</summary>
    public const float HudIntensityBucket = 0.05f;

    /// <summary>이속·히트 공통 배율. imbalance 1 → 0. 필드 읽기만, 재계산 없음.</summary>
    public static float MoveSpeedFactor(float imbalance01) =>
        1f - UnityEngine.Mathf.Clamp01(imbalance01);

    /// <summary>HitChance 배율. 이속과 동일식 (1 − Imbalance).</summary>
    public static float HitAccuracyFactor(float imbalance01) =>
        MoveSpeedFactor(imbalance01);

    /// <summary>피격 Δv → 불균형 증가분. StaggerDeltaV에서 풀 게이지.</summary>
    public static float DrainFromDeltaV(float deltaV)
    {
        float threshold = CombatImpulse.StaggerDeltaV;
        if (threshold <= 0f || deltaV <= 0f)
            return 0f;
        return UnityEngine.Mathf.Clamp01(deltaV / threshold);
    }

    public static float BucketIntensity(float imbalance01)
    {
        float v = UnityEngine.Mathf.Clamp01(imbalance01);
        if (v <= 0f)
            return 0f;
        float bucket = CombatImbalance.HudIntensityBucket;
        if (bucket <= 0f)
            return v;
        return UnityEngine.Mathf.Floor(v / bucket) * bucket;
    }
}
