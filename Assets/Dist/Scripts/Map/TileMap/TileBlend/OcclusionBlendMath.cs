// ============================================================
// OcclusionBlendMath — 오클루전 거리 곡선·프레임 간 보간
// ============================================================
using UnityEngine;

namespace IsoTilemap
{
    public static class OcclusionBlendMath
    {
        /// <summary>지수 감쇠 보간 계수. <paramref name="smoothSpeed"/>=0이면 즉시 <paramref name="target"/>.</summary>
        public static float ExpSmoothFactor(float smoothSpeed, float deltaTime)
        {
            if (smoothSpeed <= 0f || deltaTime <= 0f)
                return 1f;

            return 1f - Mathf.Exp(-smoothSpeed * deltaTime);
        }

        public static float SmoothTowards(float current, float target, float smoothFactor) =>
            Mathf.Lerp(current, target, Mathf.Clamp01(smoothFactor));

        /// <summary>거리 full~none 구간을 0~1 occlusion으로 매핑(SmoothStep).</summary>
        public static float DistanceToOcclusion01(float distance, float fullWithin, float noneBeyond)
        {
            float full = Mathf.Max(0f, fullWithin);
            float none = Mathf.Max(full + 1e-3f, noneBeyond);
            float clamped = Mathf.Clamp(distance, full, none);
            float t = Mathf.InverseLerp(none, full, clamped);
            return t * t * (3f - 2f * t);
        }

        /// <summary>셰이더·알파용 지각 보간(저강도 구간 완만).</summary>
        public static float PerceptualOcclusion01(float occlusion01)
        {
            float x = Mathf.Clamp01(occlusion01);
            return x * x * (3f - 2f * x);
        }
    }
}
