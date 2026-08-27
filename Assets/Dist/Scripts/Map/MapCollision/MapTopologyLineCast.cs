using UnityEngine;

namespace IsoTilemap
{
    /// <summary>
    /// 선분 topology 막힘 거리.
    /// 수평 cast: origin gridY 고정.
    /// 3D cast: 시선이 지나는 셀 Y의 벽 + gridY 교차 시 Floor(위층). 전 층 벽 밴드 없음.
    /// </summary>
    public sealed class MapTopologyLineCast
    {
        readonly IMapTopologyQuery _query;
        readonly float _cellSize;

        public MapTopologyLineCast(IMapTopologyQuery query)
        {
            _query = query;
            _cellSize = query.CellSize > 0f ? query.CellSize : 1f;
        }

        public float CellSize => _cellSize;

        public IMapTopologyQuery Query => _query;

        /// <summary>수평(XZ) LOS. direction.y 무시, origin의 gridY 고정.</summary>
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

        /// <summary>
        /// 3D 시선 LOS. 벽은 시선 샘플 높이(cell.y) + 발 층(feetGridY).
        /// Floor는 gridY가 바뀔 때(위·아래 층 가로막힘). 전 층 벽 밴드 순회 없음.
        /// </summary>
        public bool TryGetBlockingDistance3D(
            Vector3 origin,
            Vector3 destination,
            int feetGridY,
            out float hitDistance)
        {
            Vector3 delta = destination - origin;
            float maxDistance = delta.magnitude;
            hitDistance = maxDistance;
            if (maxDistance <= 1e-4f)
                return false;

            Vector3 direction = delta / maxDistance;
            float step = _cellSize * 0.5f;
            int steps = Mathf.Max(1, Mathf.CeilToInt(maxDistance / step));

            Vector3Int prevCell = TileHelper.ConvertWorldToGrid(origin, _cellSize);

            for (int i = 1; i <= steps; i++)
            {
                float travelled = (i / (float)steps) * maxDistance;
                Vector3 sample = origin + direction * travelled;
                Vector3Int cell = TileHelper.ConvertWorldToGrid(sample, _cellSize);

                if (HasWallAtSightOrFeet(cell.x, cell.z, cell.y, feetGridY))
                {
                    hitDistance = Mathf.Max(0f, travelled - step * 0.5f);
                    return true;
                }

                if (cell == prevCell)
                    continue;

                // 수직: 층 면 교차 → Floor가 시야를 가로막음
                if (cell.y != prevCell.y)
                {
                    int floorY = Mathf.Max(prevCell.y, cell.y);
                    int floorX = cell.y > prevCell.y ? cell.x : prevCell.x;
                    int floorZ = cell.y > prevCell.y ? cell.z : prevCell.z;
                    if (_query.CellHasFloor(floorX, floorZ, floorY))
                    {
                        hitDistance = Mathf.Max(0f, travelled - step * 0.5f);
                        return true;
                    }
                }

                if (cell.x != prevCell.x || cell.z != prevCell.z)
                {
                    int edgeY = cell.y;
                    var prev = new Vector3Int(prevCell.x, edgeY, prevCell.z);
                    var current = new Vector3Int(cell.x, edgeY, cell.z);
                    if (MapTopologyGridSegment.CrossesBlockingBetween(_query, prev, current))
                    {
                        hitDistance = Mathf.Max(0f, travelled - step * 0.5f);
                        return true;
                    }

                    if (edgeY != feetGridY)
                    {
                        var prevFeet = new Vector3Int(prevCell.x, feetGridY, prevCell.z);
                        var currentFeet = new Vector3Int(cell.x, feetGridY, cell.z);
                        if (MapTopologyGridSegment.CrossesBlockingBetween(_query, prevFeet, currentFeet))
                        {
                            hitDistance = Mathf.Max(0f, travelled - step * 0.5f);
                            return true;
                        }
                    }
                }

                prevCell = cell;
            }

            return false;
        }

        bool HasWallAtSightOrFeet(int x, int z, int sightGridY, int feetGridY)
        {
            if (_query.CellHasSolidWall(x, z, sightGridY))
                return true;
            if (sightGridY != feetGridY && _query.CellHasSolidWall(x, z, feetGridY))
                return true;
            return false;
        }
    }
}
