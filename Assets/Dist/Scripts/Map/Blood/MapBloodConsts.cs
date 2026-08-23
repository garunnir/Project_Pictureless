// ============================================================
// MapBloodConsts — 맵 혈흔 스탬프 상수 SSOT
// ============================================================

namespace IsoTilemap
{
    public static class MapBloodConsts
    {
        public const int MaxStamps = 1024;
        public const int DrawBatchSize = 1023;

        public const float DefaultScale = 0.35f;
        public const float DefaultAlpha = 0.85f;
        public const float MinScale = 0.12f;
        public const float MaxScale = 0.7f;

        public const float DripDrainThreshold = 0.02f;
        public const float DripJitterWorld = 0.15f;
        public const float DripScale = 0.22f;
        public const float DripAlpha = 0.7f;

        public const int HitSprayMinCount = 3;
        public const int HitSprayMaxCount = 10;
        public const float HitSprayConeHalfRad = 0.55f;
        public const float HitSprayMinDist = 0.05f;
        public const float HitSprayMaxDist = 0.85f;
        public const float HitSprayGroundBias = 0.08f;

        public const float ParticleNearGroundY = 0.12f;
        public const float ParticleStampMinInterval = 0.04f;
        public const int ParticleStampMaxPerBurst = 8;
        public const float ParticleStampScale = 0.2f;
        public const float ParticleStampAlpha = 0.65f;

        public const float StainYOffset = 0.02f;
    }
}
