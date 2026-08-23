// ============================================================
// BodyIllness — 출혈·감염·독소 틱 상수 SSOT
// ============================================================

namespace Garunnir.Runtime.Gameplay.Data
{
    /// <summary>밸런스 수치는 스캐폴딩. 공식은 BodyCapacity / BodyEffectTicker.</summary>
    public static class BodyIllness
    {
        /// <summary>Bleed intensity 1당 초당 Blood01 감소.</summary>
        public const float BleedBloodPerIntensityPerSecond = 0.008f;

        /// <summary>같은 부위 Bleed 지속 후 Infected 부여.</summary>
        public const float InfectedOnsetSeconds = 20f;

        /// <summary>프로토타입 손 Bleed 초 — onset보다 짧게.</summary>
        public const float PrototypeBleedSeconds = 12f;

        public const float InfectedProgressPerSecond = 0.012f;
        public const float ImmunityPerSecond = 0.015f;

        public const float InfectionConsciousnessK = 1f;
        public const float ToxinConsciousnessK = 1f;
        public const float ToxinFiltrationK = 1f;

        public const float ToxinClearPerSecond = 0.02f;
        public const float RotToxinAdd = 0.15f;

        public const float MedInfectionClear = 0.35f;
        public const float MedToxinClear = 0.25f;
        public const int MedBleedIntensityReduce = 1;

        public const float ToxinMoodMin = 0.2f;
        public const float LowImmunityFiltration = 0.4f;

        // 장기 HP0 파괴 출혈 intensity (뇌 제외)
        public const int OrganDestroyedBleedHeart = 8;
        public const int OrganDestroyedBleedLiver = 6;
        public const int OrganDestroyedBleedLung = 5;
        public const int OrganDestroyedBleedKidney = 4;
        public const int OrganDestroyedBleedStomach = 4;
        public const int OrganDestroyedBleedDefault = 3;
    }
}
