// ============================================================
// CharacterHearingDefaults — 청각 판정 상수 SSOT (3D 구형, 시야 cone과 분리)
// ============================================================

using UnityEngine;

public static class CharacterHearingDefaults
{
    public const float BaseRadius = 8f;
    public const float MovementSpeedThreshold = 0.08f;
    public const float DetectAudibilityThreshold = 0.35f;
    public const float WallAttenuation = 0.55f;
    public const float FloorAttenuationPerLevel = 0.70f;

    /// <summary>3D 구 — CharacterVisionDefaults.IsWithinConeXZ 와 다른 계약.</summary>
    public static bool IsWithinSphere(Vector3 listenerFeet, Vector3 targetFeet, float radius)
    {
        float r = Mathf.Max(0f, radius);
        return (targetFeet - listenerFeet).sqrMagnitude <= r * r;
    }
}
