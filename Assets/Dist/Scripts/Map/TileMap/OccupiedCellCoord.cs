// ============================================================
// OccupiedCellCoord — 월드·identity → 점유 인덱스 기준 Vector3Int
// ============================================================
using System;
using UnityEngine;

namespace IsoTilemap
{
    /// <summary>
    /// 점유 인덱스에 실재하는 셀 좌표 해석. 맵 전역 Y 목록·기둥 인덱스 API 없음.
    /// <para><b>점유 인덱스를 우선 신뢰</b> — 시선·차단·블렌드는 <c>ConvertWorldToGrid</c> + <c>CellHasOccupancy</c>.
    /// <see cref="ResolveFromWorld"/>는 <b>플레이어 발밑 바닥(층)</b> 전용이며 시선 샘플에 쓰지 않는다.</para>
    /// </summary>
    public static class OccupiedCellCoord
    {
        public static int ResolveMinOccupiedCellY(TileMapCacheHub hub)
        {
            if (hub == null)
                return 0;

            int minY = 0;
            bool any = false;
            foreach (var (_, _, y) in hub.EnumerateOccupiedCells())
            {
                if (!any || y < minY)
                {
                    minY = y;
                    any = true;
                }
            }

            return any ? minY : 0;
        }

        /// <summary>
        /// bake·건물 밑바닥 흔적과 동일 — Floor face <see cref="FloorFaceKey.CellAbove"/> 최소 Y.
        /// <see cref="ResolveMinOccupiedCellY"/>와 달리 CellBelow 점유는 포함하지 않음.
        /// </summary>
        public static int ResolveMinStructuralFloorCellY(TileMapCacheHub hub)
        {
            if (hub == null)
                return 0;

            int minY = int.MaxValue;
            foreach (var face in hub.Topology.Index.EnumerateFaceTiles())
            {
                if (!TileIdentityUtil.IsFloorTile(face.identity))
                    continue;

                var key = FloorFaceKey.FromFloorTileIdentity(face.identity);
                int sy = face.identity.sizeUnit.y;
                if (sy < 1) sy = 1;

                for (int dy = 0; dy < sy; dy++)
                {
                    int y = key.CellAbove.y + dy;
                    if (y < minY)
                        minY = y;
                }
            }

            return minY == int.MaxValue ? 0 : minY;
        }

        /// <summary>
        /// 월드 위치 → 발 높이 이하 논리 바닥이 있는 최상위 점유셀.
        /// seed <c>(x,y0,z)</c>에서 <c>y0..minCellY</c>로 <c>y--</c> 탐색.
        /// <b>플레이어 층 전용.</b> 시선 샘플·건물 차단에는 사용하지 않는다 (→ DATA.md §좌표 규약).
        /// </summary>
        public static Vector3Int ResolveFromWorld(
            TileMapCacheHub hub,
            Vector3 world,
            float cellSize,
            float? feetWorldY = null,
            float cellEpsilonWorld = 0f,
            int minCellY = int.MinValue)
        {
            if (hub == null)
                throw new ArgumentNullException(nameof(hub));

            cellSize = Mathf.Max(1e-4f, cellSize);
            if (minCellY == int.MinValue)
                minCellY = ResolveMinOccupiedCellY(hub);

            Vector3Int seed = TileHelper.ConvertWorldToGrid(world, cellSize);
            float feetCeiling = (feetWorldY ?? world.y) + cellEpsilonWorld;
            int x = seed.x;
            int z = seed.z;

            for (int y = seed.y; y >= minCellY; y--)
            {
                if (!hub.CellHasOccupancy(x, z, y))
                    continue;
                if (!hub.CellHasFloor(x, y, z))
                    continue;
                if (y * cellSize > feetCeiling)
                    continue;

                return new Vector3Int(x, y, z);
            }

            return new Vector3Int(x, minCellY, z);
        }

        /// <summary>타일 대표 점유셀 — 모든 슬롯에서 <see cref="TileIdentity.GridPos"/>.</summary>
        public static Vector3Int PrimaryCellFromIdentity(in TileIdentity id) => id.GridPos;

        /// <summary>walkable → FloorFaceKey 앵커(CellBelow). 내부 면 키 전용.</summary>
        public static Vector3Int FloorAnchorFromOccupiedCell(Vector3Int occupiedCell) =>
            occupiedCell + Vector3Int.down;

        /// <summary>
        /// 시선 샘플 월드점 → 그리드 높이 유지. <see cref="TileMapCacheHub.CellHasOccupancy"/> true일 때만 true.
        /// 건물 차단·구조 오클루전용. <see cref="ResolveFromWorld"/> 사용 금지.
        /// </summary>
        public static bool TryResolveSightOccupiedCell(
            TileMapCacheHub hub,
            Vector3 world,
            float cellSize,
            out Vector3Int occupiedCell)
        {
            occupiedCell = TileHelper.ConvertWorldToGrid(world, cellSize);
            if (hub == null)
                return false;

            return hub.CellHasOccupancy(occupiedCell.x, occupiedCell.z, occupiedCell.y);
        }

        /// <summary>시선 샘플 그리드 좌표 (높이 유지). 근접 블렌드 XZ 반경 슬라이스용.</summary>
        public static Vector3Int GridAtSightSampleHeight(Vector3 world, float cellSize) =>
            TileHelper.ConvertWorldToGrid(world, cellSize);
    }
}
