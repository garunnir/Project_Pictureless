// ============================================================
// MapLiquidConsts — 맵 액체 시뮬레이션 상수 SSOT (BN 1000L/셀 + CA 임계)
// ============================================================

namespace IsoTilemap
{
    public static class MapLiquidConsts
    {
        public const string WaterTypeId = "water";

        /// <summary>BN 1000L/셀 기준 셀당 최대 ml. 향후 terrain별 bake로 대체 가능(현재는 전 셀 동일).</summary>
        public const int DefaultMaxVolumeMl = 1_000_000;

        public const byte MaxLevel = 255;

        /// <summary>Level 1당 ml. DefaultMaxVolumeMl / MaxLevel ≈ 3921.</summary>
        public const int MlPerLevel = DefaultMaxVolumeMl / MaxLevel;

        /// <summary>이 값 이하 diff는 흐르지 않음 — 진동·튐·"바닷물 폭주" 방지의 단일 게이트.</summary>
        public const int MinFlowMl = MlPerLevel / 8;

        /// <summary>수직 압축 시 아래 칸이 cap보다 약간 더 담을 수 있는 여유(BN pressure 느낌).</summary>
        public const int OverCompressMl = MlPerLevel / 20;

        /// <summary>SHALLOW_WATER 시드 비율(capMl 대비).</summary>
        public const float ShallowSeedFraction = 0.35f;

        /// <summary>한 번의 FlowSolver 처리(WorldClock.MinuteChanged 1회)당 처리할 dirty 셀 수 상한.</summary>
        public const int MaxUpdatesPerTick = 512;
    }
}
