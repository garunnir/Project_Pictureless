using UnityEngine;

namespace IsoTilemap
{
    /// <summary>가시성·스트리밍에서 IsStructural 타일의 visibility slice Y를 해석합니다.</summary>
    public static class TileVisibilityCellUtil
    {
        /// <summary>
        /// Floor·EdgeWall 등 structural 타일의 visibility slice Y.
        /// HorizontalFace: walkable 상단. VerticalFace: incident 점유 셀 max Y.
        /// </summary>
        public static int GetVisibilitySliceY(in TileIdentity id)
        {
            if (TileIdentityUtil.IsHorizontalFace(id))
            {
                var key = FloorFaceKey.FromFloorTileIdentity(id);
                int sy = Mathf.Max(1, id.sizeUnit.y);
                return key.CellAbove.y + sy - 1;
            }

            if (TileIdentityUtil.IsVerticalFace(id))
            {
                var key = WallEdgeKey.FromWallTileIdentity(id);
                int sy = Mathf.Max(1, id.sizeUnit.y);
                int maxY = int.MinValue;
                for (int dy = 0; dy < sy; dy++)
                {
                    maxY = Mathf.Max(maxY, key.Anchor.y + dy);
                    maxY = Mathf.Max(maxY, key.NeighborCell().y + dy);
                }

                return maxY == int.MinValue ? id.GridPos.y : maxY;
            }

            if (TileIdentityUtil.IsOccupiedCell(id) && TileIdentityUtil.IsStructural(id))
            {
                int sy = Mathf.Max(1, id.sizeUnit.y);
                return id.GridPos.y + sy - 1;
            }

            return OccupiedCellCoord.PrimaryCellFromIdentity(id).y;
        }

        public static int GetCellY(in TileData tile) =>
            GetVisibilitySliceY(tile.identity);

        /// <summary>player floor 아래 peek 판정 — structural 타일 공통.</summary>
        public static bool IsSliceInPeekBelow(in TileIdentity id, int sliceY, in FloorVisibilityContext ctx)
        {
            if (ctx.VisibleBelowCells == null || ctx.VisibleBelowCells.Count == 0)
                return false;

            if (TileIdentityUtil.IsVerticalFace(id))
            {
                var key = WallEdgeKey.FromWallTileIdentity(id);
                if (ctx.VisibleBelowCells.Contains((key.Anchor.x, key.Anchor.z, sliceY)))
                    return true;

                var neighbor = key.NeighborCell();
                return ctx.VisibleBelowCells.Contains((neighbor.x, neighbor.z, sliceY));
            }

            if (TileIdentityUtil.IsFloorTile(id))
            {
                var walkable = FloorFaceKey.FromFloorTileIdentity(id).CellAbove;
                return ctx.VisibleBelowCells.Contains((walkable.x, walkable.z, sliceY));
            }

            return ctx.VisibleBelowCells.Contains((id.GridPos.x, id.GridPos.z, sliceY));
        }
    }
}
