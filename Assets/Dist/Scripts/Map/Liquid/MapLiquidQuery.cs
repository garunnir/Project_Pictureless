// ============================================================
// MapLiquidQuery — 다른 시스템(Fish/Climate/Combat)용 읽기 전용 조회 SSOT
// ============================================================
// 전체 overlay 순회 금지 — 좌표 단건 조회만 제공(정적 셀 무연산 보증 §3).

using UnityEngine;

namespace IsoTilemap
{
    public static class MapLiquidQuery
    {
        public static int GetEffectiveMl(Vector3Int cell)
        {
            MapLiquidHost host = MapLiquidHost.Runtime;
            return host != null ? host.Overlay.GetEffectiveMl(cell) : 0;
        }

        /// <summary>0..1 충만도. capMl은 현재 전 셀 공통(DefaultMaxVolumeMl).</summary>
        public static float Fill01(Vector3Int cell)
        {
            int ml = GetEffectiveMl(cell);
            if (ml <= 0)
                return 0f;

            return Mathf.Clamp01((float)ml / MapLiquidConsts.DefaultMaxVolumeMl);
        }

        public static bool TryGetTypeId(Vector3Int cell, out string typeId)
        {
            MapLiquidHost host = MapLiquidHost.Runtime;
            if (host != null && host.Overlay.TryGetCell(cell, out MapLiquidCell c))
            {
                typeId = c.TypeId;
                return true;
            }

            typeId = null;
            return false;
        }

        public static bool HasAnyLiquid(Vector3Int cell) => GetEffectiveMl(cell) > 0;
    }
}
