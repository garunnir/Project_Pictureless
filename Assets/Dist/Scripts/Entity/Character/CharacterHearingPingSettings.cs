// ============================================================
// CharacterHearingPingSettings — 플레이어 청각 핑 오버레이 튜닝 SSOT
// ============================================================

using System;
using UnityEngine;

[Serializable]
public struct CharacterHearingPingSettings
{
    [Tooltip("CharacterSightFadeHost.DisplayVisibility 이 값 이하일 때만 바닥 핑.")]
    [Min(0f)]
    public float HiddenThreshold;

    [Tooltip("audibility × MaxAlpha → quad 알파.")]
    [Range(0f, 1f)]
    public float MaxAlpha;

    [Tooltip("셀 바닥 quad Y 오프셋(미터).")]
    [Min(0f)]
    public float YOffsetMeters;

    [Tooltip("핑 알파가 target을 MoveTowards로 따라가는 속도(초당). 0이면 즉시.")]
    [Min(0f)]
    public float DisplayFadePerSecond;

    [Tooltip("셀 중심 quad 한 변 길이(미터).")]
    [Min(0.1f)]
    public float QuadSizeMeters;

    public static CharacterHearingPingSettings DefaultUnity => new CharacterHearingPingSettings
    {
        HiddenThreshold = 0.02f,
        MaxAlpha = 0.55f,
        YOffsetMeters = 0.02f,
        DisplayFadePerSecond = 10f,
        QuadSizeMeters = 0.95f,
    };
}
