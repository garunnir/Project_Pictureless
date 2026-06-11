using System.Collections.Generic;
using UnityEngine;

namespace IsoTilemap
{
    /// <summary><see cref="TileMapCacheHub"/> topology 조회. 다유닛 안전을 위해 즉시 판정만 제공합니다.</summary>
    public sealed class MapTopologyQuery : IMapTopologyQuery
    {
        readonly TileMapCacheHub _hub;
        readonly float _cellSize;

        public MapTopologyQuery(TileMapCacheHub hub, float cellSize)
        {
            _hub = hub;
            _cellSize = cellSize > 0f ? cellSize : 1f;
        }

        public float CellSize => _cellSize;

        public bool TryGetCellTiles(int x, int z, int gridY, out IReadOnlyList<TileData> list)
        {
            if (_hub.TryGetCellTiles(x, z, gridY, out var hubList))
            {
                list = hubList;
                return true;
            }

            list = null;
            return false;
        }

        public bool CellHasSolidWall(int x, int z, int gridY)
        {
            if (!TryGetCellTiles(x, z, gridY, out var list))
                return false;

            return FloorMapIndex.CellHasSolidWall(list);
        }

        public bool CellHasFloor(int x, int z, int gridY) =>
            _hub.Topology.CellHasFloor(x, gridY, z);

        public bool TryGetEdgeBetween(Vector3Int cellA, Vector3Int cellB, out TileData edgeWall) =>
            _hub.TryGetEdgeBetween(cellA, cellB, out edgeWall);
    }
}
