// ============================================================
// CharacterPresenceSettings — 존재감(가시성·소음) 복합 출력 튜닝 SSOT
// ============================================================

using System;
using UnityEngine;

[Serializable]
public struct CharacterPresenceSettings
{
    [Tooltip("은신 중 타인 시력 판정 반경 배율 (0~1).")]
    [Range(0f, 1f)]
    public float StealthVisibilityMultiplier;

    [Tooltip("은신 중 이동 소음 배율 (0~1).")]
    [Range(0f, 1f)]
    public float StealthNoiseMultiplier;

    [Tooltip("달리기 시 소음 배율 (1 = 변화 없음).")]
    [Min(0f)]
    public float SprintNoiseMultiplier;

    [Tooltip("CurrentSpeed를 Noise01로 정규화하는 기준 속도(미터/초).")]
    [Min(0.01f)]
    public float NoiseReferenceSpeed;

    public static CharacterPresenceSettings DefaultUnity => new CharacterPresenceSettings
    {
        StealthVisibilityMultiplier = 0.35f,
        StealthNoiseMultiplier = 0.4f,
        SprintNoiseMultiplier = 1.75f,
        NoiseReferenceSpeed = 6f,
    };
}
