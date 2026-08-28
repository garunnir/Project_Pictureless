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

        /// <summary>
        /// 수중창 Fill01 임계 등 **레거시 얕은 물 체감**용 비율.
        /// 시드량에는 쓰지 않는다 — Shallow/Deep 타일은 둘 다 cap 가득 시드(깊이 구분은 ml/컬럼).
        /// </summary>
        public const float ShallowSeedFraction = 0.35f;

        /// <summary>한 번의 FlowSolver 처리(WorldClock.MinuteChanged 1회)당 처리할 dirty 셀 수 상한.</summary>
        public const int MaxUpdatesPerTick = 512;

        /// <summary>온도 저장 단위 — 1 deci°C = 0.1 °C. short로 ±3276.7 °C를 담아 용암까지 커버한다.</summary>
        public const int DeciCPerC = 10;

        /// <summary>ambient 공급자가 없을 때 쓰는 기본 기온. deci°C가 저장·연산의 SSOT다.</summary>
        public const short DefaultAmbientDeciC = 200;

        /// <summary>
        /// 셀 온도가 이웃·대기 평균과 이 값 미만으로 차이나면 아무것도 하지 않는다 —
        /// 열 확산판 정적 셀 무연산 게이트. 평형 셀은 자신·이웃을 다시 dirty로 넣지 않으므로
        /// 정지 바다 비용이 0이 된다. 대가로 셀이 평형에서 조금 떨어진 채 멈춘다 — 평균이 결합 수로
        /// 희석되므로 오차 상한은 이 값 × 결합 수다(물 이웃 2 + 대기 1이면 약 0.3 °C).
        /// </summary>
        public const int MinTempStepDeciC = 2;

        /// <summary>이웃 평균으로 가는 relax 비율의 역수. 2 = 한 틱에 평균과의 차이 절반.</summary>
        public const int ThermalRelaxDivisor = 2;

        /// <summary>한 번의 ThermalSolver 처리당 처리할 thermal dirty 셀 수 상한.</summary>
        public const int MaxThermalUpdatesPerTick = 512;

        /// <summary>
        /// 기온이 이만큼 움직였을 때만 노출면을 다시 dirty로 넣는다.
        /// 매 분 재표집하면 액체 셀 전체 순회가 되므로 반드시 이 임계를 거친다.
        /// </summary>
        public const int AmbientResampleStepDeciC = 5;

        /// <summary>
        /// 고체 액체가 위 셀의 바닥이 되기 위한 최소 보유량 — 살얼음 위를 걷게 하지 않는다.
        /// </summary>
        public const int MinSolidSupportMl = DefaultMaxVolumeMl / 2;

        public static short ToDeciC(float tempC) =>
            (short)Mathf.Clamp(Mathf.RoundToInt(tempC * DeciCPerC), short.MinValue, short.MaxValue);

        public static float FromDeciC(short deciC) => deciC / (float)DeciCPerC;

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
