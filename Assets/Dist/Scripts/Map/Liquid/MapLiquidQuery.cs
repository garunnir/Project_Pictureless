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
        public static float Fill01(Vector3Int cell) => MapLiquidConsts.ToFill01(GetEffectiveMl(cell));

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

        /// <summary>이 셀의 액체가 어는점 이하로 내려가 고체인지.</summary>
        public static bool IsSolid(Vector3Int cell) =>
            TryGetLiquidCell(cell, out MapLiquidCell c) && c.IsSolid;

        /// <summary>
        /// 이 셀의 고체 액체가 **위 셀의 바닥**이 되는지.
        /// 살얼음(부분 충전)은 제외 — <see cref="MapLiquidConsts.MinSolidSupportMl"/> 이상만 지지한다.
        /// </summary>
        public static bool ProvidesSolidSupport(Vector3Int cell) =>
            TryGetLiquidCell(cell, out MapLiquidCell c)
            && c.IsSolid
            && c.EffectiveMl >= MapLiquidConsts.MinSolidSupportMl;

        static bool TryGetLiquidCell(Vector3Int cell, out MapLiquidCell liquidCell)
        {
            liquidCell = null;
            MapLiquidHost host = MapLiquidHost.Runtime;
            if (host == null)
                return false;

            return host.Overlay.TryGetCell(cell, out liquidCell)
                && liquidCell != null
                && !liquidCell.IsEmpty;
        }

        /// <summary>
        /// <paramref name="topCell"/>에서 아래로 물이 이어지는 동안의 누적 ml (수심).
        /// 물이 없는 셀을 만나면 즉시 멈추고, <see cref="MapLiquidConsts.MaxColumnScanCells"/>에서 끊는다.
        /// </summary>
        /// <remarks>
        /// 셀 하나는 cap + 압축 여유에서 클램프되므로 <see cref="Fill01"/>만으로는 "몇 셀 깊이인가"를 알 수 없다.
        /// 수직 2셀 이상을 요구하는 임계(예: 낚시)는 이 누적값으로만 판정할 수 있다.
        /// </remarks>
        public static int ColumnMlDownward(Vector3Int topCell)
        {
            MapLiquidHost host = MapLiquidHost.Runtime;
            if (host == null)
                return 0;

            MapLiquidOverlay overlay = host.Overlay;
            int totalMl = 0;
            Vector3Int cell = topCell;

            for (int i = 0; i < MapLiquidConsts.MaxColumnScanCells; i++)
            {
                int ml = overlay.GetEffectiveMl(cell);
                if (ml <= 0)
                    break;

                totalMl += ml;
                cell.y -= 1;
            }

            return totalMl;
        }
    }
}
