// ============================================================
// MapLiquidConsts — 맵 액체 시뮬레이션 상수 SSOT (BN 1000L/셀 + CA 임계)
// ============================================================

using UnityEngine;

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

        /// <summary>
        /// <see cref="MapLiquidQuery.ColumnMlDownward"/>가 아래로 훑는 최대 셀 수.
        /// 물이 끊기면 그 전에 멈추므로 실제 비용은 보통 1~2셀이며, 이 값은 비정상 깊이 상한 가드다.
        /// </summary>
        public const int MaxColumnScanCells = 8;

        /// <summary>ml → 0..1 충만도. 쿼리와 렌더 메셔가 공유하는 단일 변환(terrain별 capMl 도입 시 여기만 고친다).</summary>
        public static float ToFill01(int effectiveMl)
        {
            if (effectiveMl <= 0)
                return 0f;

            return Mathf.Clamp01((float)effectiveMl / DefaultMaxVolumeMl);
        }
    }
}
