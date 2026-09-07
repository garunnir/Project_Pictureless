// ============================================================
// CharacterSenseGizmo — 캐릭터 가시·가청 범위 Scene/Play 디버그 기즈모
// ============================================================

using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterVision))]
public sealed class CharacterSenseGizmo : MonoBehaviour
{
    [SerializeField] bool _drawGizmos = true;
    [SerializeField] bool _onlyWhenSelected;
    [SerializeField] bool _drawVisionDetect = true;
    [SerializeField] bool _drawVisionLose = true;
    [SerializeField] bool _drawHearing = true;

    CharacterVision _vision;
    CharacterHearing _hearing;
    CharacterState _state;
    CharacterDefinitionBinder _definitionBinder;

    void OnValidate() => EnsureRefs();

    void Awake() => EnsureRefs();

    void EnsureRefs()
    {
        TryGetComponent(out _vision);
        TryGetComponent(out _hearing);
        _state = CharacterBodyResolve.GetInBody<CharacterState>(this);
        _definitionBinder = CharacterBodyResolve.GetInBody<CharacterDefinitionBinder>(this);
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (!_onlyWhenSelected)
            DrawSenseGizmos();
    }

    void OnDrawGizmosSelected()
    {
        if (_onlyWhenSelected)
            DrawSenseGizmos();
    }

    void DrawSenseGizmos()
    {
        if (!_drawGizmos || _vision == null)
            return;

        EnsureRefs();
        ResolveRadii(
            out float detectRadius,
            out float loseRadius,
            out float hearingRadius,
            out float spotAngle,
            out float innerSpotAngle);

        Vector3 center = CharacterFeetPose.GetFeetWorld(transform);
        Vector3 forward = CharacterSightForward.ResolveXZ(_state, transform);

        if (_drawVisionDetect && detectRadius > 0f)
        {
            CharacterSightFadeGizmoColors.DrawVisionSectorXZ(
                center,
                forward,
                detectRadius,
                0f,
                spotAngle,
                innerSpotAngle);
        }

        if (_drawVisionLose && loseRadius > 0f && loseRadius > detectRadius + 0.01f)
        {
            CharacterSenseGizmoColors.DrawVisionConeWireXZ(
                center,
                forward,
                loseRadius,
                spotAngle,
                CharacterSenseGizmoColors.VisionLoseWire);
        }

        if (_drawHearing && _hearing != null && hearingRadius > 0f)
            CharacterSenseGizmoColors.DrawHearingSphereGizmos(center, hearingRadius);
    }

    void ResolveRadii(
        out float detectRadius,
        out float loseRadius,
        out float hearingRadius,
        out float spotAngle,
        out float innerSpotAngle)
    {
        if (Application.isPlaying)
        {
            detectRadius = _vision.EffectiveDetectRadius;
            loseRadius = _vision.EffectiveLoseRadius;
            spotAngle = _vision.EffectiveSpotAngleDegrees;
            innerSpotAngle = _vision.EffectiveInnerSpotAngleDegrees;
            hearingRadius = _hearing != null ? _hearing.EffectiveHearingRadius : 0f;
            return;
        }

        CharacterDefinition definition = _definitionBinder != null ? _definitionBinder.Definition : null;
        CharacterSenseBlock senses = definition != null ? definition.Senses : CharacterSenseBlock.Default;
        detectRadius = senses.sightDetectMeters;
        loseRadius = senses.sightLoseMeters;
        hearingRadius = senses.hearingRadiusMeters;
        spotAngle = CharacterVisionDefaults.ClampSpotAngle(
            definition != null
                ? definition.SpotAngleDegrees
                : CharacterVisionDefaults.SpotAngleDegrees);
        innerSpotAngle = spotAngle * CharacterVisionDefaults.InnerSpotAngleRatio;
    }

#endif
}
