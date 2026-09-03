using UnityEngine;

namespace IsoTilemap
{
    /// <summary>XZ topology 셀·엣지 막힘. 셀 경계 DDA 기반( micro-step 금지 ).</summary>
    public sealed class MapTopologyCollisionResolver
    {
        readonly IMapTopologyQuery _query;
        readonly float _cellSize;

        public MapTopologyCollisionResolver(IMapTopologyQuery query)
        {
            _query = query;
            _cellSize = query.CellSize > 0f ? query.CellSize : 1f;
        }

        /// <param name="feetWorld">발 월드 위치. gridY 슬라이스는 여기서 결정됩니다.</param>
        /// <param name="footprint">grid footprint (sx,sy,sz). XZ는 수평 점유, Y는 수직 band.</param>
        public Vector3 ClampHorizontal(Vector3 feetWorld, Vector3 delta, Vector3Int footprint)
        {
            footprint = ClampFootprint(footprint);
            Vector3 flatDelta = new Vector3(delta.x, 0f, delta.z);
            if (flatDelta.sqrMagnitude <= 1e-8f)
                return delta;

            int gridY = MapCollisionGrid.WorldToGridY(feetWorld, _cellSize);

            if (!SegmentCrossesBlocking(feetWorld, feetWorld + flatDelta, gridY, footprint))
                return delta;

            Vector3 target = feetWorld + flatDelta;

            Vector3 xSlide = new Vector3(target.x, feetWorld.y, feetWorld.z);
            if (!SegmentCrossesBlocking(feetWorld, xSlide, gridY, footprint))
                return new Vector3(xSlide.x - feetWorld.x, delta.y, 0f);

            Vector3 zSlide = new Vector3(feetWorld.x, feetWorld.y, target.z);
            if (!SegmentCrossesBlocking(feetWorld, zSlide, gridY, footprint))
                return new Vector3(0f, delta.y, zSlide.z - feetWorld.z);

            return Vector3.zero;
        }

        bool SegmentCrossesBlocking(Vector3 from, Vector3 to, int gridY, Vector3Int footprint)
        {
            var toFeetCell = WorldToCell(to);
            if (FootprintXZBlocksAtFeetY(_query, toFeetCell, footprint))
                return true;

            var fromFeetCell = WorldToCell(from);
            if (fromFeetCell.x == toFeetCell.x && fromFeetCell.z == toFeetCell.z)
                return false;

            TryGetAnchorFromFeet(fromFeetCell, footprint, out Vector3Int fromAnchor);
            TryGetAnchorFromFeet(toFeetCell, footprint, out Vector3Int toAnchor);

            int sx = footprint.x;
            int sz = footprint.z;
            for (int dx = 0; dx < sx; dx++)
            {
                for (int dz = 0; dz < sz; dz++)
                {
                    if (MapTopologyGridSegment.CrossesBlockingSegment(
                            _query,
                            fromAnchor.x + dx,
                            fromAnchor.z + dz,
                            toAnchor.x + dx,
                            toAnchor.z + dz,
                            gridY))
                        return true;
                }
            }

            return false;
        }

        /// <summary>발밑 셀 기준 footprint XZ 점유가 gridY에서 벽이면 true.</summary>
        internal static bool FootprintXZBlocksAtFeetY(
            IMapTopologyQuery query,
            Vector3Int feetCell,
            Vector3Int footprint)
        {
            footprint = ClampFootprint(footprint);
            TryGetAnchorFromFeet(feetCell, footprint, out Vector3Int anchor);

            int gridY = feetCell.y;
            for (int dx = 0; dx < footprint.x; dx++)
            {
                for (int dz = 0; dz < footprint.z; dz++)
                {
                    if (query.CellHasSolidWall(anchor.x + dx, anchor.z + dz, gridY))
                        return true;
                }
            }

            return false;
        }

        /// <summary>발밑 셀 기준 footprint 볼륨 내 임의 셀이 벽이면 true.</summary>
        internal static bool FootprintVolumeBlocks(
            IMapTopologyQuery query,
            Vector3Int feetCell,
            Vector3Int footprint)
        {
            footprint = ClampFootprint(footprint);
            TryGetAnchorFromFeet(feetCell, footprint, out Vector3Int anchor);

            int sx = footprint.x;
            int sy = footprint.y;
            int sz = footprint.z;
            for (int dx = 0; dx < sx; dx++)
            {
                for (int dy = 0; dy < sy; dy++)
                {
                    for (int dz = 0; dz < sz; dz++)
                    {
                        int x = anchor.x + dx;
                        int y = anchor.y + dy;
                        int z = anchor.z + dz;
                        if (query.CellHasSolidWall(x, z, y))
                            return true;
                    }
                }
            }

            return false;
        }

        internal static Vector3Int ClampFootprint(Vector3Int footprint) =>
            new Vector3Int(
                Mathf.Max(1, footprint.x),
                Mathf.Max(1, footprint.y),
                Mathf.Max(1, footprint.z));

        static void TryGetAnchorFromFeet(Vector3Int feetCell, Vector3Int footprint, out Vector3Int anchor)
        {
            footprint = ClampFootprint(footprint);
            anchor = new Vector3Int(
                feetCell.x - (footprint.x - 1) / 2,
                feetCell.y,
                feetCell.z - (footprint.z - 1) / 2);
        }

        Vector3Int WorldToCell(Vector3 world) =>
            TileHelper.ConvertWorldToGrid(world, _cellSize);
    }
}
