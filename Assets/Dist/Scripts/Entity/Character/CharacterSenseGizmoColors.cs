// ============================================================
// CharacterSenseGizmoColors — 가시·가청 범위 Scene 기즈모 색·드로우 SSOT
// ============================================================

using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public static class CharacterSenseGizmoColors
{
    public static readonly Color VisionLoseWire = new(0.55f, 0.45f, 1f, 0.75f);
    public static readonly Color HearingWire = new(0.45f, 0.85f, 1f, 0.85f);
    public static readonly Color HearingFill = new(0.45f, 0.85f, 1f, 0.06f);

#if UNITY_EDITOR
    /// <summary>XZ 부채꼴 외곽선만 (lose 반경 등).</summary>
    public static void DrawVisionConeWireXZ(
        Vector3 centerFeet,
        Vector3 forwardXZ,
        float radius,
        float spotAngleDegrees,
        Color color)
    {
        float outer = Mathf.Max(0f, radius);
        if (outer <= 0f)
            return;

        forwardXZ.y = 0f;
        if (forwardXZ.sqrMagnitude < 1e-6f)
            forwardXZ = Vector3.forward;
        forwardXZ.Normalize();

        float halfAngle = Mathf.Clamp(spotAngleDegrees * 0.5f, 0f, 180f);
        float yaw = Mathf.Atan2(forwardXZ.x, forwardXZ.z) * Mathf.Rad2Deg;
        Vector3 up = Vector3.up;
        Vector3 arcStart = Quaternion.Euler(0f, yaw - halfAngle, 0f) * Vector3.forward;

        Color prev = Handles.color;
        Handles.color = color;
        Handles.DrawWireArc(centerFeet, up, arcStart, halfAngle * 2f, outer);
        DrawConeEdges(centerFeet, yaw, halfAngle, outer);
        Handles.color = prev;
    }

    static void DrawConeEdges(Vector3 center, float yawDegrees, float halfAngleDegrees, float radius)
    {
        Vector3 left = Quaternion.Euler(0f, yawDegrees - halfAngleDegrees, 0f) * Vector3.forward;
        Vector3 right = Quaternion.Euler(0f, yawDegrees + halfAngleDegrees, 0f) * Vector3.forward;
        Handles.DrawLine(center, center + left * radius);
        Handles.DrawLine(center, center + right * radius);
        Handles.DrawLine(center, center + Quaternion.Euler(0f, yawDegrees, 0f) * Vector3.forward * radius);
    }

    public static void DrawHearingSphereGizmos(Vector3 centerFeet, float radius)
    {
        float r = Mathf.Max(0f, radius);
        if (r <= 0f)
            return;

        Color prev = Gizmos.color;
        Gizmos.color = HearingFill;
        Gizmos.DrawSphere(centerFeet, r);
        Gizmos.color = HearingWire;
        Gizmos.DrawWireSphere(centerFeet, r);
        Gizmos.color = prev;
    }
#endif
}
