// ============================================================
// CharacterSightFadeGizmoColors — 시야 페이드 Scene 기즈모 색·크기 SSOT
// ============================================================

using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public static class CharacterSightFadeGizmoColors
{
    public const float DefaultMarkerRadius = 0.25f;

    public static readonly Color FullyVisible = new Color(0.25f, 0.9f, 0.35f, 0.95f);
    public static readonly Color Fading = new Color(0.95f, 0.85f, 0.2f, 0.95f);
    public static readonly Color Hidden = new Color(0.85f, 0.25f, 0.25f, 0.85f);
    public static readonly Color PossessedSkip = new Color(0.35f, 0.65f, 1f, 0.9f);
    public static readonly Color VisionOuter = new Color(0.35f, 0.8f, 1f, 0.95f);
    public static readonly Color VisionInner = new Color(0.45f, 1f, 0.55f, 0.9f);
    public static readonly Color VisionOuterFill = new Color(0.35f, 0.8f, 1f, 0.12f);

    public static Color ForVisibility(float display01, float hideEpsilon, bool possessedSkip)
    {
        if (possessedSkip)
            return PossessedSkip;

        float e = Mathf.Max(0f, hideEpsilon);
        if (display01 <= e)
            return Hidden;
        if (display01 >= 1f - e)
            return FullyVisible;
        return Fading;
    }

#if UNITY_EDITOR
    /// <summary>XZ 부채꼴 — Spot.spotAngle / innerSpotAngle과 동일.</summary>
    public static void DrawVisionSectorXZ(
        Vector3 centerFeet,
        Vector3 forwardXZ,
        float outerRadius,
        float innerRadius,
        float spotAngleDegrees,
        float innerSpotAngleDegrees)
    {
        float outer = Mathf.Max(0f, outerRadius);
        if (outer <= 0f)
            return;

        forwardXZ.y = 0f;
        if (forwardXZ.sqrMagnitude < 1e-6f)
            forwardXZ = Vector3.forward;
        forwardXZ.Normalize();

        float halfOuter = Mathf.Clamp(spotAngleDegrees * 0.5f, 0f, 180f);
        float halfInner = Mathf.Clamp(innerSpotAngleDegrees * 0.5f, 0f, halfOuter);
        float yaw = Mathf.Atan2(forwardXZ.x, forwardXZ.z) * Mathf.Rad2Deg;

        Color prev = Handles.color;
        Vector3 up = Vector3.up;

        Handles.color = VisionOuterFill;
        Handles.DrawSolidArc(centerFeet, up, Quaternion.Euler(0f, yaw - halfOuter, 0f) * Vector3.forward, halfOuter * 2f, outer);

        Handles.color = VisionOuter;
        Handles.DrawWireArc(centerFeet, up, Quaternion.Euler(0f, yaw - halfOuter, 0f) * Vector3.forward, halfOuter * 2f, outer);
        DrawSectorEdges(centerFeet, yaw, halfOuter, outer);

        float inner = Mathf.Clamp(innerRadius, 0f, outer);
        if (inner > 0f && !Mathf.Approximately(inner, outer))
        {
            Handles.color = VisionInner;
            Handles.DrawWireArc(centerFeet, up, Quaternion.Euler(0f, yaw - halfOuter, 0f) * Vector3.forward, halfOuter * 2f, inner);
        }

        if (halfInner > 0f && !Mathf.Approximately(halfInner, halfOuter))
        {
            Handles.color = VisionInner;
            Handles.DrawWireArc(centerFeet, up, Quaternion.Euler(0f, yaw - halfInner, 0f) * Vector3.forward, halfInner * 2f, outer);
            DrawSectorEdges(centerFeet, yaw, halfInner, outer);
        }

        Handles.color = prev;
    }

    static void DrawSectorEdges(Vector3 center, float yawDegrees, float halfAngleDegrees, float radius)
    {
        Vector3 left = Quaternion.Euler(0f, yawDegrees - halfAngleDegrees, 0f) * Vector3.forward;
        Vector3 right = Quaternion.Euler(0f, yawDegrees + halfAngleDegrees, 0f) * Vector3.forward;
        Handles.DrawLine(center, center + left * radius);
        Handles.DrawLine(center, center + right * radius);
        Handles.DrawLine(center, center + Quaternion.Euler(0f, yawDegrees, 0f) * Vector3.forward * radius);
    }
#endif
}
