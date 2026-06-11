// ============================================================
// FloorRoomQueryResolver — room 베이크 조회용 바닥 (x,z) 앵커 해석
// ============================================================
using UnityEngine;

namespace IsoTilemap
{
    /// <summary>
    /// 의도된 월드 기준점·층 Y로 room geometry 베이크를 읽을 때,
    /// XZ 스냅이 벽/빈칸이어도 같은 층의 논리 바닥 셀을 찾습니다.
    /// </summary>
    public static class FloorRoomQueryResolver
    {
        static readonly Vector3Int[] CardinalNeighbors =
        {
            Vector3Int.right, Vector3Int.back, Vector3Int.left, Vector3Int.forward
        };

        public static bool CellHasFloor(TopologyLayer topology, int floorCellY, int x, int z)
        {
            if (topology == null)
                return false;

            return topology.TryGetCellTiles(x, z, floorCellY, out var list) &&
                   FloorMapIndex.CellHasFloor(list);
        }

        /// <summary>
        /// <paramref name="worldRef"/> XZ 스냅 또는 인접 카드널 바닥 중 월드에 가장 가까운 (x,z).
        /// </summary>
        public static bool TryResolveFloorAnchorXZ(
            TopologyLayer topology,
            int floorCellY,
            Vector3 worldRef,
            float cellSize,
            out int floorX,
            out int floorZ)
        {
            floorX = 0;
            floorZ = 0;

            if (topology == null)
                return false;

            cellSize = Mathf.Max(1e-4f, cellSize);
            Vector3Int snap = TileHelper.ConvertWorldToGrid(worldRef, cellSize);

            float bestSq = float.MaxValue;
            bool found = false;
            int bestX = snap.x;
            int bestZ = snap.z;

            void Consider(int x, int z)
            {
                if (!CellHasFloor(topology, floorCellY, x, z))
                    return;

                Vector3 center = TileHelper.ConvertGridToWorldPos(new Vector3Int(x, floorCellY, z), cellSize);
                float dx = worldRef.x - center.x;
                float dz = worldRef.z - center.z;
                float sq = dx * dx + dz * dz;
                if (!found || sq < bestSq)
                {
                    found = true;
                    bestSq = sq;
                    bestX = x;
                    bestZ = z;
                }
            }

            Consider(snap.x, snap.z);
            for (int i = 0; i < CardinalNeighbors.Length; i++)
            {
                Vector3Int d = CardinalNeighbors[i];
                Consider(snap.x + d.x, snap.z + d.z);
            }

            if (!found)
                return false;

            floorX = bestX;
            floorZ = bestZ;
            return true;
        }
    }
}
