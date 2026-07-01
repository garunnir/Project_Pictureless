// ============================================================
// SpaceVisibilityUtil — Space 범위 기준 structural visibility 판정 보조
// ============================================================
using System.Collections.Generic;
using UnityEngine;

namespace IsoTilemap
{
    public static class SpaceVisibilityUtil
    {
        public static bool TryGetStructuralBand(in TileIdentity id, out int minY, out int maxY)
        {
            int sy = Mathf.Max(1, id.sizeUnit.y);

            if (TileIdentityUtil.IsHorizontalFace(id))
            {
                var key = FloorFaceKey.FromFloorTileIdentity(id);
                minY = key.CellAbove.y;
                maxY = key.CellAbove.y + sy - 1;
                return true;
            }

            if (TileIdentityUtil.IsVerticalFace(id))
            {
                var key = WallEdgeKey.FromWallTileIdentity(id);
                int bottom = Mathf.Min(key.Anchor.y, key.NeighborCell().y);
                int top = Mathf.Max(key.Anchor.y, key.NeighborCell().y);
                minY = bottom;
                maxY = top + sy - 1;
                return true;
            }

            if (TileIdentityUtil.IsOccupiedCell(id) && TileIdentityUtil.IsStructural(id))
            {
                minY = id.GridPos.y;
                maxY = id.GridPos.y + sy - 1;
                return true;
            }

            Vector3Int cell = OccupiedCellCoord.PrimaryCellFromIdentity(id);
            minY = cell.y;
            maxY = cell.y;
            return false;
        }

        public static bool IsEntirelyAbovePlayerSpace(in TileIdentity id, in FloorVisibilityContext ctx)
        {
            TryGetStructuralBand(id, out int minY, out _);
            return minY > ctx.PlayerSpaceMaxY;
        }

        public static bool IsEntirelyBelowPlayerSpace(in TileIdentity id, in FloorVisibilityContext ctx)
        {
            TryGetStructuralBand(id, out _, out int maxY);
            return maxY < ctx.PlayerSpaceMinY;
        }

        public static bool TouchesPlayerSpace(in TileIdentity id, in FloorVisibilityContext ctx)
        {
            HashSet<Vector3Int> cells = ctx.PlayerSpaceFloorCells;
            if (cells == null || cells.Count == 0)
                return false;

            if (TileIdentityUtil.IsHorizontalFace(id))
            {
                Vector3Int walkable = FloorFaceKey.FromFloorTileIdentity(id).CellAbove;
                return ContainsAnyY(cells, walkable, Mathf.Max(1, id.sizeUnit.y));
            }

            if (TileIdentityUtil.IsVerticalFace(id))
            {
                var key = WallEdgeKey.FromWallTileIdentity(id);
                int sy = Mathf.Max(1, id.sizeUnit.y);
                return ContainsAnyY(cells, key.Anchor, sy) ||
                       ContainsAnyY(cells, key.NeighborCell(), sy);
            }

            if (TileIdentityUtil.IsOccupiedCell(id) && TileIdentityUtil.IsStructural(id))
                return ContainsAnyY(cells, id.GridPos, Mathf.Max(1, id.sizeUnit.y));

            return cells.Contains(OccupiedCellCoord.PrimaryCellFromIdentity(id));
        }

        static bool ContainsAnyY(HashSet<Vector3Int> cells, Vector3Int baseCell, int sizeY)
        {
            for (int dy = 0; dy < sizeY; dy++)
            {
                if (cells.Contains(new Vector3Int(baseCell.x, baseCell.y + dy, baseCell.z)))
                    return true;
            }

            return false;
        }
    }
}
