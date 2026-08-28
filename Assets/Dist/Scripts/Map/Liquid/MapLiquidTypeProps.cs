// ============================================================
// MapLiquidTypeProps — 액체 타입별 물성 SSOT (어는점 등)
// ============================================================
// typeId → 물성. 미등록 타입은 "얼지 않음"으로 폴백해, 새 액체를 추가해도
// 등록 전에는 상변화가 일어나지 않는다(조용한 오작동 대신 무동작).

using System.Collections.Generic;

namespace IsoTilemap
{
    public static class MapLiquidTypeProps
    {
        public readonly struct Props
        {
            /// <summary>이 온도 이하에서 고체 — 흐르지 않고 위 셀에 바닥을 제공한다.</summary>
            public readonly short FreezingPointDeciC;

            public Props(short freezingPointDeciC) => FreezingPointDeciC = freezingPointDeciC;
        }

        /// <summary>어는점이 short 최솟값 = 사실상 절대 얼지 않는다.</summary>
        static readonly Props Fallback = new Props(short.MinValue);

        static readonly Dictionary<string, Props> ByTypeId = new Dictionary<string, Props>
        {
            [MapLiquidConsts.WaterTypeId] = new Props(0),
        };

        public static Props Get(string typeId)
        {
            if (!string.IsNullOrEmpty(typeId) && ByTypeId.TryGetValue(typeId, out Props props))
                return props;

            return Fallback;
        }

        public static short FreezingPointDeciC(string typeId) => Get(typeId).FreezingPointDeciC;

        public static bool IsSolidAt(string typeId, short tempDeciC) =>
            tempDeciC <= FreezingPointDeciC(typeId);
    }
}
