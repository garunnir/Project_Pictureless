// ============================================================
// MapParticleFloorLandingConsts — 파티클 논리 바닥 착지 상수 SSOT
// ============================================================
namespace IsoTilemap
{
    public static class MapParticleFloorLandingConsts
    {
        /// <summary>교차 밴드는 <see cref="MapLogicalFloorCross.Tolerance"/> SSOT.</summary>
        public const float NearGroundTolerance = MapLogicalFloorCross.Tolerance;
        public const float DownwardVelocityThreshold = 0.05f;
        public const float DefaultSurfaceYOffset = 0.02f;
        public const int DefaultMaxLandingsPerFrame = 32;
    }
}
