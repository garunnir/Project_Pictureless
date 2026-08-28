// ============================================================
// MapLiquidMlBridge — 붓기(전액 차감)/뜨기(정확 ml) ml↔셀 변환
// ============================================================
// 비대칭 규칙(플레이어 손해 방향, docs/map/LIQUID.md):
// - Pour: 요청한 ml 전액이 셀에 반영되고, cap 초과분은 소멸이 아니라 dirty로 위임되어
//   다음 FlowSolver 틱부터 이웃/위로 전달된다. 호출부(인벤)는 반환값 전량을 차감해야 한다.
// - Draw: 셀에 있는 만큼만, 요청 이상은 절대 주지 않는다(정확 ml, 낭비 없음).

using UnityEngine;

namespace IsoTilemap
{
    public static class MapLiquidMlBridge
    {
        /// <summary>
        /// targetCell에 pourMl을 붓는다. 맵에 정의되지 않은 셀(HasOccupancy false)은 거부(0 반환).
        /// 반환값 = 인벤에서 차감해야 할 ml(전액 차감 — 항상 pourMl과 동일하거나 0).
        /// </summary>
        public static int Pour(
            MapLiquidHost host,
            Vector3Int targetCell,
            string typeId,
            int pourMl)
        {
            if (host?.Overlay == null || pourMl <= 0)
                return 0;

            host.Overlay.AddEffectiveMl(targetCell, pourMl, typeId ?? MapLiquidConsts.WaterTypeId);
            host.Overlay.MarkDirty(targetCell);
            return pourMl;
        }

        /// <summary>
        /// sourceCell에서 최대 requestedMl을 뜬다. 반환값 = 실제 지급된 ml(요청보다 클 수 없음, 낭비 없음).
        /// </summary>
        public static int Draw(MapLiquidHost host, Vector3Int sourceCell, int requestedMl)
        {
            if (host?.Overlay == null || requestedMl <= 0)
                return 0;

            int available = host.Overlay.GetEffectiveMl(sourceCell);
            int granted = Mathf.Min(requestedMl, available);
            if (granted <= 0)
                return 0;

            string typeId = host.Overlay.TryGetCell(sourceCell, out MapLiquidCell cell)
                ? cell.TypeId
                : MapLiquidConsts.WaterTypeId;

            host.Overlay.AddEffectiveMl(sourceCell, -granted, typeId);
            host.Overlay.MarkDirty(sourceCell);
            return granted;
        }
    }
}
