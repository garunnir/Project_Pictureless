// ============================================================
// CharacterSightFadeSettings — 플레이어 시야 반경 캐릭터 페이드 튜닝 SSOT
// ============================================================

using System;
using UnityEngine;

[Serializable]
public struct CharacterSightFadeSettings
{
    [Tooltip("EffectiveDetectRadius 끝에서 0→1로 선형 페이드되는 폭(미터).")]
    [Min(0f)]
    public float FadeWidthMeters;

    [Tooltip("display가 target을 MoveTowards로 따라가는 속도(초당).")]
    [Min(0f)]
    public float DisplayFadePerSecond;

    [Tooltip("이보다 작으면 SkinnedMeshRenderer.enabled=false.")]
    [Min(0f)]
    public float FullHideEpsilon;

    [Tooltip("켜면 눈높이 3D topology LOS(벽·위층 Floor). 층 가시성 hide와 무관.")]
    public bool LineOfSightEnabled;

    [Tooltip("발 기준 LOS 샘플 높이(미터). XZ 부채꼴은 높이 제한 없음.")]
    [Min(0f)]
    public float LosHeightOffsetMeters;

    [Tooltip("에디터 Scene 뷰 캐릭터 위치·시야 반경 기즈모.")]
    public bool DrawEditorGizmos;

    public static CharacterSightFadeSettings DefaultUnity => new CharacterSightFadeSettings
    {
        FadeWidthMeters = 2f,
        DisplayFadePerSecond = 8f,
        FullHideEpsilon = 0.02f,
        LineOfSightEnabled = true,
        LosHeightOffsetMeters = 1f,
        DrawEditorGizmos = true,
    };
}
