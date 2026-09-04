// ============================================================
// AnimationHybridTrajectory — 애니 progress 커브 × 코드 waypoints (하이브리드 SSOT)
// ============================================================

using UnityEngine;

/// <summary>
/// 위치·waypoint는 코드, 시간→진행률 형태는 <see cref="AnimationCurve"/> (클립 루트 bake).
/// 커브 없으면 normalizedTime 선형 폴백.
/// </summary>
public static class AnimationHybridTrajectory
{
    public static float ResolveProgress(float normalizedTime, AnimationCurve progressCurve)
    {
        normalizedTime = Mathf.Clamp01(normalizedTime);
        if (progressCurve == null || progressCurve.length == 0)
            return normalizedTime;

        return Mathf.Clamp01(progressCurve.Evaluate(normalizedTime));
    }

    /// <summary>2점 직선. progress = curve(t).</summary>
    public static Vector3 SampleSegment(
        Vector3 start,
        Vector3 end,
        float normalizedTime,
        AnimationCurve progressCurve)
    {
        float p = ResolveProgress(normalizedTime, progressCurve);
        return Vector3.Lerp(start, end, p);
    }

    /// <summary>3점 꺾인 경로. progress = curve(t)로 호 길이 비율 매핑.</summary>
    public static Vector3 SampleArc(
        Vector3 start,
        Vector3 peak,
        Vector3 end,
        float normalizedTime,
        AnimationCurve progressCurve)
    {
        float p = ResolveProgress(normalizedTime, progressCurve);
        float len1 = Vector3.Distance(start, peak);
        float len2 = Vector3.Distance(peak, end);
        float total = len1 + len2;
        if (total <= 1e-6f)
            return end;

        float d = p * total;
        if (d <= len1)
            return Vector3.Lerp(start, peak, len1 > 1e-6f ? d / len1 : 1f);

        return Vector3.Lerp(peak, end, len2 > 1e-6f ? (d - len1) / len2 : 1f);
    }

    /// <summary>
    /// Mantle: Y·XZ 진행률 분리. Y = curve(t); XZ = curve(t) 또는 xzStartT 선형 폴백.
    /// </summary>
    public static Vector3 SampleMantleDecoupled(
        Vector3 start,
        Vector3 end,
        float normalizedTime,
        AnimationCurve yProgress,
        AnimationCurve xzProgress,
        float xzStartT)
    {
        float t = Mathf.Clamp01(normalizedTime);
        float py = ResolveProgress(t, yProgress);
        float pxz = ResolveXzProgress(t, xzProgress, xzStartT);

        float y = Mathf.Lerp(start.y, end.y, py);
        float x = Mathf.Lerp(start.x, end.x, pxz);
        float z = Mathf.Lerp(start.z, end.z, pxz);
        return new Vector3(x, y, z);
    }

    static float ResolveXzProgress(float normalizedTime, AnimationCurve xzProgress, float xzStartT)
    {
        if (xzProgress != null && xzProgress.length > 0)
            return ResolveProgress(normalizedTime, xzProgress);

        normalizedTime = Mathf.Clamp01(normalizedTime);
        xzStartT = Mathf.Clamp01(xzStartT);
        if (normalizedTime < xzStartT)
            return 0f;

        return Mathf.InverseLerp(xzStartT, 1f, normalizedTime);
    }
}
