// ============================================================
// PlayerStatusMoodVisuals — 요약 HUD Fill 틴트/채움 SSOT
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

        /// <summary>Fill 이미지 극성 틴트. Intensity는 fillAmount(0~1)로 별도 적용.</summary>
        public static Color ResolveFillTint(MoodPolarity polarity)
        {
            return polarity switch
            {
                MoodPolarity.Positive => PositiveGreen,
                MoodPolarity.Negative => NegativeRed,
                _ => NeutralWhite
            };
        }

        /// <summary>레거시: intensity lerp 틴트. 신규 경로는 ResolveFillTint + fillAmount.</summary>
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
