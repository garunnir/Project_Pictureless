// ============================================================
// CharacterEmoteSettings — 월드 이모트 튜닝 SSOT
// ============================================================

using System;
using UnityEngine;

[Serializable]
public struct CharacterEmoteSettings
{
    [Tooltip("CharacterSightFadeHost.DisplayVisibility 이 값 이하이면 NPC 이모트 숨김.")]
    [Min(0f)]
    public float HiddenThreshold;

    public static CharacterEmoteSettings DefaultUnity => new CharacterEmoteSettings
    {
        HiddenThreshold = CharacterHearingPingSettings.DefaultUnity.HiddenThreshold,
    };
}
