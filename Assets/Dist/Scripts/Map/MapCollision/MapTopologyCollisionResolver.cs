using System.Collections.Generic;
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

        public Vector3 ClampHorizontal(Vector3 worldFrom, Vector3 delta, int band)
        {
            Vector3 flatDelta = new Vector3(delta.x, 0f, delta.z);
            if (flatDelta.sqrMagnitude <= 1e-8f)
                return delta;

            if (!SegmentCrossesBlocking(worldFrom, worldFrom + flatDelta, band))
                return delta;

            Vector3 target = worldFrom + flatDelta;

            Vector3 xSlide = new Vector3(target.x, worldFrom.y, worldFrom.z);
            if (!SegmentCrossesBlocking(worldFrom, xSlide, band))
                return new Vector3(xSlide.x - worldFrom.x, delta.y, 0f);

            Vector3 zSlide = new Vector3(worldFrom.x, worldFrom.y, target.z);
            if (!SegmentCrossesBlocking(worldFrom, zSlide, band))
                return new Vector3(0f, delta.y, zSlide.z - worldFrom.z);

            return Vector3.zero;
        }

        bool SegmentCrossesBlocking(Vector3 from, Vector3 to, int band)
        {
            var toCell = WorldToCell(to);
            if (_query.CellHasSolidWall(toCell.x, toCell.z, band))
                return true;

            var fromCell = WorldToCell(from);
            if (fromCell == toCell)
                return false;

            foreach (var step in TraverseCells(fromCell, toCell))
            {
                if (_query.CellHasSolidWall(step.current.x, step.current.z, band))
                    return true;

                if (_query.TryGetEdgeBetween(step.prev, step.current, out _))
                    return true;
            }

            return false;
        }

        readonly struct GridStep
        {
            public readonly Vector3Int prev;
            public readonly Vector3Int current;

            public GridStep(Vector3Int prev, Vector3Int current)
            {
                this.prev = prev;
                this.current = current;
            }
        }

        IEnumerable<GridStep> TraverseCells(Vector3Int fromCell, Vector3Int toCell)
        {
            int x0 = fromCell.x;
            int z0 = fromCell.z;
            int x1 = toCell.x;
            int z1 = toCell.z;
            int band = fromCell.y;

            int dx = Mathf.Abs(x1 - x0);
            int dz = Mathf.Abs(z1 - z0);
            int sx = x0 < x1 ? 1 : -1;
            int sz = z0 < z1 ? 1 : -1;
            int err = dx - dz;

            int x = x0;
            int z = z0;
            var prev = new Vector3Int(x, band, z);

            while (x != x1 || z != z1)
            {
                int e2 = 2 * err;
                if (e2 > -dz)
                {
                    err -= dz;
                    x += sx;
                }

                if (e2 < dx)
                {
                    err += dx;
                    z += sz;
                }

                var current = new Vector3Int(x, band, z);
                yield return new GridStep(prev, current);
                prev = current;
            }
        }

        Vector3Int WorldToCell(Vector3 world) =>
            TileHelper.ConvertWorldToGrid(world, _cellSize);
    }
}
