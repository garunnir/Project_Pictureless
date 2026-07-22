// ============================================================
// PlayerStatusMoodVisuals — 요약 HUD 뒷장 틴트 색 SSOT
// ============================================================

using UnityEngine;

namespace Garunnir.Runtime.Gameplay.Data
{
    public static class PlayerStatusMoodVisuals
    {
        public static readonly Color NeutralWhite = Color.white;
        public static readonly Color NegativeRed = new(1f, 0.18f, 0.18f, 1f);
        public static readonly Color PositiveGreen = new(0.22f, 1f, 0.28f, 1f);

        public const float VitalLowIntensity = 0.5f;
        public const float VitalCriticalIntensity = 1f;
        public const float EffectDefaultIntensity = 1f;

        public const float AttentionShakeInitialAmplitude = 10f;
        public const float AttentionShakeDecay = 0.55f;
        public const int AttentionShakeOscillations = 5;
        public const float AttentionShakeStepDuration = 0.055f;

        public static Color ResolveBackColor(MoodPolarity polarity, float intensity)
        {
            float t = Mathf.Clamp01(intensity);
            return polarity switch
            {
                MoodPolarity.Positive => Color.Lerp(NeutralWhite, PositiveGreen, t),
                MoodPolarity.Negative => Color.Lerp(NeutralWhite, NegativeRed, t),
                _ => NeutralWhite
            };
        }
    }
}
