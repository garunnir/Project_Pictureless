using UnityEngine;

namespace IsoTilemap
{
    /// <summary>선분 topology 막힘 거리. origin gridY 슬라이스 고정·<see cref="TileMapCacheHub.EnumerateOccupiedCells"/> 미사용.</summary>
    public sealed class MapTopologyLineCast
    {
        readonly IMapTopologyQuery _query;
        readonly float _cellSize;

        public MapTopologyLineCast(IMapTopologyQuery query)
        {
            _query = query;
            _cellSize = query.CellSize > 0f ? query.CellSize : 1f;
        }

        public bool TryGetBlockingDistance(
            Vector3 origin,
            Vector3 direction,
            float maxDistance,
            out float hitDistance)
        {
            hitDistance = maxDistance;
            direction.y = 0f;
            if (direction.sqrMagnitude <= 1e-6f || maxDistance <= 1e-4f)
                return false;

            direction.Normalize();
            float step = _cellSize * 0.5f;
            int steps = Mathf.Max(1, Mathf.CeilToInt(maxDistance / step));

            Vector3Int prevCell = TileHelper.ConvertWorldToGrid(origin, _cellSize);
            int gridY = prevCell.y;

            for (int i = 1; i <= steps; i++)
            {
                float travelled = (i / (float)steps) * maxDistance;
                Vector3 sample = origin + direction * travelled;
                Vector3Int cell = TileHelper.ConvertWorldToGrid(sample, _cellSize);

                if (_query.CellHasSolidWall(cell.x, cell.z, gridY))
                {
                    hitDistance = Mathf.Max(0f, travelled - step * 0.5f);
                    return true;
                }

                if (cell.x != prevCell.x || cell.z != prevCell.z)
                {
                    var prev = new Vector3Int(prevCell.x, gridY, prevCell.z);
                    var current = new Vector3Int(cell.x, gridY, cell.z);
                    if (MapTopologyGridSegment.CrossesBlockingBetween(_query, prev, current))
                    {
                        hitDistance = Mathf.Max(0f, travelled - step * 0.5f);
                        return true;
                    }

                    prevCell = cell;
                }
            }

            return false;
        }
    }
}
