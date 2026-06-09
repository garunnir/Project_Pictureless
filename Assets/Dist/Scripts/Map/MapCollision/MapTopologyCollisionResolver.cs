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
        public Vector3 ClampHorizontal(Vector3 feetWorld, Vector3 delta)
        {
            Vector3 flatDelta = new Vector3(delta.x, 0f, delta.z);
            if (flatDelta.sqrMagnitude <= 1e-8f)
                return delta;

            int gridY = MapCollisionGrid.WorldToGridY(feetWorld, _cellSize);

            if (!SegmentCrossesBlocking(feetWorld, feetWorld + flatDelta, gridY))
                return delta;

            Vector3 target = feetWorld + flatDelta;

            Vector3 xSlide = new Vector3(target.x, feetWorld.y, feetWorld.z);
            if (!SegmentCrossesBlocking(feetWorld, xSlide, gridY))
                return new Vector3(xSlide.x - feetWorld.x, delta.y, 0f);

            Vector3 zSlide = new Vector3(feetWorld.x, feetWorld.y, target.z);
            if (!SegmentCrossesBlocking(feetWorld, zSlide, gridY))
                return new Vector3(0f, delta.y, zSlide.z - feetWorld.z);

            return Vector3.zero;
        }

        bool SegmentCrossesBlocking(Vector3 from, Vector3 to, int gridY)
        {
            var toCell = WorldToCell(to);
            if (_query.CellHasSolidWall(toCell.x, toCell.z, gridY))
                return true;

            var fromCell = WorldToCell(from);
            if (fromCell.x == toCell.x && fromCell.z == toCell.z)
                return false;

            return MapTopologyGridSegment.CrossesBlockingSegment(
                _query,
                fromCell.x,
                fromCell.z,
                toCell.x,
                toCell.z,
                gridY);
        }

        Vector3Int WorldToCell(Vector3 world) =>
            TileHelper.ConvertWorldToGrid(world, _cellSize);
    }
}
