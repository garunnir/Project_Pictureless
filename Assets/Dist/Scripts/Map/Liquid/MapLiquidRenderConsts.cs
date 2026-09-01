// ============================================================
// MapLiquidRenderConsts — 액체 수면 렌더 상수 SSOT (가시 임계·리메시 예산·셰이더 키)
// ============================================================

namespace IsoTilemap
{
    public static class MapLiquidRenderConsts
    {
        /// <summary>
        /// 이 Fill01 이하는 물이 없는 것으로 본다. 시뮬 Level 1(<see cref="MapLiquidConsts.MlPerLevel"/>)보다
        /// 낮게 잡아, 부은 물이 시뮬에는 있는데 화면에는 없는 구간이 생기지 않게 한다.
        /// "위 칸이 물인가" 판정도 같은 임계를 쓴다.
        /// </summary>
        public const float MinVisibleFill01 = 0.002f;

        /// <summary>수면을 셀 바닥에서 최소 이만큼 띄운다(cellSize 배율).</summary>
        public const float SurfaceMinLift01 = 0.05f;

        /// <summary>노출 수면만 셀 천장 inset. 잠긴 셀 측면은 SideSurfaceLift로 건너뛴다.</summary>
        public const float SurfaceTopInset01 = 0.04f;

        /// <summary>측면 생략: 이웃 EffectiveFill01 ≥ 자신 × 이 비율일 때만 연결.</summary>
        public const float SideWallConnectMinRatio01 = 0.35f;

        public const int MaxChunkRemeshPerFrame = 4;
        public const int MaxChunkBuildPerFrame = 12;
        public const int FallbackChunkSize = 16;
        public const float ShaderTimeWrapSeconds = 3600f;
        public const string GlobalTimeProperty = "_MapLiquidTime";
        public const string SurfaceShaderName = "Dist/MapLiquidSurface";
        public const string SurfaceMaterialResourcePath = "Map/MapLiquidSurface";
    }
}
