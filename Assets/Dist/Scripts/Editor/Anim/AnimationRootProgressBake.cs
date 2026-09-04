#if UNITY_EDITOR
// ============================================================
// AnimationRootProgressBake — 클립 루트 이동 → 0..1 progress 커브 bake
// ============================================================

using UnityEditor;
using UnityEngine;

/// <summary>
/// Humanoid/generic 클립의 RootT.* 커브로 누적 이동 거리 progress를 만든다.
/// Bake Into Pose(루트 고정) 클립은 null → 런타임 선형 폴백.
/// </summary>
public static class AnimationRootProgressBake
{
    const string RootX = "RootT.x";
    const string RootY = "RootT.y";
    const string RootZ = "RootT.z";
    const int SampleCount = 32;

    public static AnimationCurve TryBakeProgressCurve(AnimationClip clip)
    {
        if (clip == null || clip.length <= 0f)
            return null;

        AnimationCurve curveX = FindCurve(clip, RootX);
        AnimationCurve curveY = FindCurve(clip, RootY);
        AnimationCurve curveZ = FindCurve(clip, RootZ);
        if (curveX == null && curveY == null && curveZ == null)
            return null;

        float Eval(AnimationCurve c, float t) => c != null ? c.Evaluate(t) : 0f;

        Vector3 prev = new Vector3(Eval(curveX, 0f), Eval(curveY, 0f), Eval(curveZ, 0f));
        float cumulative = 0f;
        var keys = new Keyframe[SampleCount + 1];

        for (int i = 0; i <= SampleCount; i++)
        {
            float nt = i / (float)SampleCount;
            float time = nt * clip.length;
            Vector3 pos = new Vector3(Eval(curveX, time), Eval(curveY, time), Eval(curveZ, time));
            if (i > 0)
                cumulative += Vector3.Distance(prev, pos);
            prev = pos;
            keys[i] = new Keyframe(nt, 0f);
        }

        if (cumulative <= 1e-5f)
            return null;

        float walked = 0f;
        prev = new Vector3(Eval(curveX, 0f), Eval(curveY, 0f), Eval(curveZ, 0f));
        keys[0] = new Keyframe(0f, 0f);
        for (int i = 1; i <= SampleCount; i++)
        {
            float nt = i / (float)SampleCount;
            float time = nt * clip.length;
            Vector3 pos = new Vector3(Eval(curveX, time), Eval(curveY, time), Eval(curveZ, time));
            walked += Vector3.Distance(prev, pos);
            prev = pos;
            keys[i] = new Keyframe(nt, Mathf.Clamp01(walked / cumulative));
        }

        var curve = new AnimationCurve(keys);
        for (int i = 0; i < curve.length; i++)
        {
            AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
            AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
        }

        return curve;
    }

    /// <summary>
    /// RootT 축별 누적 이동 progress: Y는 |dy|, XZ는 sqrt(dx²+dz²). 축별 이동 없으면 해당 커브 null.
    /// </summary>
    public static bool TryBakeAxisProgressCurves(
        AnimationClip clip,
        out AnimationCurve yCurve,
        out AnimationCurve xzCurve)
    {
        yCurve = null;
        xzCurve = null;
        if (clip == null || clip.length <= 0f)
            return false;

        AnimationCurve curveX = FindCurve(clip, RootX);
        AnimationCurve curveY = FindCurve(clip, RootY);
        AnimationCurve curveZ = FindCurve(clip, RootZ);

        yCurve = BakeAbsoluteDeltaProgress(clip, t => curveY.Evaluate(t), curveY != null);
        xzCurve = BakeHorizontalXzProgress(clip, curveX, curveZ);

        return yCurve != null || xzCurve != null;
    }

    static AnimationCurve BakeAbsoluteDeltaProgress(
        AnimationClip clip,
        System.Func<float, float> eval,
        bool hasCurve)
    {
        if (!hasCurve)
            return null;

        return BuildNormalizedProgressCurve(clip, nt =>
        {
            float time = nt * clip.length;
            return eval(time);
        }, (prevValue, value) => Mathf.Abs(value - prevValue));
    }

    static AnimationCurve BakeHorizontalXzProgress(
        AnimationClip clip,
        AnimationCurve curveX,
        AnimationCurve curveZ)
    {
        if (curveX == null && curveZ == null)
            return null;

        float EvalX(float t) => curveX != null ? curveX.Evaluate(t) : 0f;
        float EvalZ(float t) => curveZ != null ? curveZ.Evaluate(t) : 0f;

        return BuildNormalizedProgressCurve(clip, nt =>
        {
            float time = nt * clip.length;
            return new Vector2(EvalX(time), EvalZ(time));
        }, (prevPos, pos) =>
        {
            Vector2 p = (Vector2)prevPos;
            Vector2 c = (Vector2)pos;
            float dx = c.x - p.x;
            float dz = c.y - p.y;
            return Mathf.Sqrt(dx * dx + dz * dz);
        });
    }

    static AnimationCurve BuildNormalizedProgressCurve<T>(
        AnimationClip clip,
        System.Func<float, T> sampleAtNormalizedTime,
        System.Func<T, T, float> segmentLength)
    {
        T prev = sampleAtNormalizedTime(0f);
        float walked = 0f;
        float cumulative = 0f;

        for (int i = 1; i <= SampleCount; i++)
        {
            float nt = i / (float)SampleCount;
            T pos = sampleAtNormalizedTime(nt);
            cumulative += segmentLength(prev, pos);
            prev = pos;
        }

        if (cumulative <= 1e-5f)
            return null;

        var keys = new Keyframe[SampleCount + 1];
        prev = sampleAtNormalizedTime(0f);
        keys[0] = new Keyframe(0f, 0f);

        for (int i = 1; i <= SampleCount; i++)
        {
            float nt = i / (float)SampleCount;
            T pos = sampleAtNormalizedTime(nt);
            walked += segmentLength(prev, pos);
            prev = pos;
            keys[i] = new Keyframe(nt, Mathf.Clamp01(walked / cumulative));
        }

        var curve = new AnimationCurve(keys);
        for (int i = 0; i < curve.length; i++)
        {
            AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
            AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
        }

        return curve;
    }

    static AnimationCurve FindCurve(AnimationClip clip, string propertyName)
    {
        EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(clip);
        for (int i = 0; i < bindings.Length; i++)
        {
            if (bindings[i].propertyName != propertyName)
                continue;

            return AnimationUtility.GetEditorCurve(clip, bindings[i]);
        }

        return null;
    }
}
#endif
