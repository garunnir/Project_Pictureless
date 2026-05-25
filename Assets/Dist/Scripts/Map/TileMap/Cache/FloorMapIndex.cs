// ============================================================
// FloorMapIndex — 층(band)별 타일·바닥·벽·엣지 조회 스냅샷
// ============================================================
using System.Collections.Generic;
using UnityEngine;

namespace IsoTilemap
{
    public sealed class FloorMapIndex
    {
        private static readonly Vector3Int[] CardinalNeighbors =
        {
            Vector3Int.right, Vector3Int.back, Vector3Int.left, Vector3Int.forward
        };

        private readonly Dictionary<Vector3Int, List<TileData>> _tiles;
        private readonly IReadOnlyDictionary<WallEdgeKey, TileData> _edges;
        private readonly HashSet<(int x, int z, int band)> _anyTileAt = new();

        public FloorMapIndex(
            Dictionary<Vector3Int, List<TileData>> tiles,
            IReadOnlyDictionary<WallEdgeKey, TileData> edges)
        {
            _tiles = tiles;
            _edges = edges ?? new Dictionary<WallEdgeKey, TileData>();

            foreach (var kv in _tiles)
            {
                var pos = kv.Key;
                _anyTileAt.Add((pos.x, pos.z, pos.y));
            }

            foreach (var kv in _edges)
            {
                var key = kv.Key;
                _anyTileAt.Add((key.CellA.x, key.CellA.z, key.CellA.y));
                _anyTileAt.Add((key.CellB.x, key.CellB.z, key.CellB.y));
            }
        }

        public static FloorMapIndex FromModel(TileMapModel model) =>
            new FloorMapIndex(model.tiles, model.EdgeBinder.EdgeIndex);

        public bool HasAnyTile(int x, int z, int band) => _anyTileAt.Contains((x, z, band));

        public IEnumerable<(int x, int z, int band)> EnumerateOccupiedCells() => _anyTileAt;

        /// <summary>런타임 topology 변경 후 (x,z,band) 점유 집합을 <see cref="_tiles"/>와 맞춥니다.</summary>
        public void SyncOccupancyForCell(int x, int z, int band)
        {
            var key = (x, z, band);
            if (CellHasTilesAt(x, z, band))
                _anyTileAt.Add(key);
            else
                _anyTileAt.Remove(key);
        }

        public void SyncOccupancyFromChangedCells(IEnumerable<Vector3Int> changedCells)
        {
            if (changedCells == null)
                return;

            foreach (var cell in changedCells)
                SyncOccupancyForCell(cell.x, cell.z, cell.y);
        }

        public void RebuildOccupancy()
        {
            _anyTileAt.Clear();

            foreach (var kv in _tiles)
            {
                var pos = kv.Key;
                if (kv.Value != null && kv.Value.Count > 0)
                    _anyTileAt.Add((pos.x, pos.z, pos.y));
            }

            foreach (var kv in _edges)
            {
                var edgeKey = kv.Key;
                _anyTileAt.Add((edgeKey.CellA.x, edgeKey.CellA.z, edgeKey.CellA.y));
                _anyTileAt.Add((edgeKey.CellB.x, edgeKey.CellB.z, edgeKey.CellB.y));
            }
        }

        bool CellHasTilesAt(int x, int z, int band) =>
            _tiles.TryGetValue(new Vector3Int(x, band, z), out var list) && list != null && list.Count > 0;

        public bool TryGetCellTiles(int x, int z, int band, out List<TileData> list) =>
            _tiles.TryGetValue(new Vector3Int(x, band, z), out list);

        public bool TryGetEdgeBetween(Vector3Int cellA, Vector3Int cellB, out TileData edgeWall)
        {
            edgeWall = default;
            return WallEdgeKey.TryBetween(cellA, cellB, out var edgeKey) &&
                   _edges.TryGetValue(edgeKey, out edgeWall);
        }

        public static bool CellHasFloor(IReadOnlyList<TileData> list)
        {
            if (list == null)
                return false;

            for (int i = 0; i < list.Count; i++)
            {
                if ((TileView.TileType)list[i].identity.tileType == TileView.TileType.Floor)
                    return true;
            }

            return false;
        }

        public static bool CellHasSolidWall(IReadOnlyList<TileData> list)
        {
            if (list == null)
                return false;

            for (int i = 0; i < list.Count; i++)
            {
                var type = (TileView.TileType)list[i].identity.tileType;
                if (type == TileView.TileType.Wall || type == TileView.TileType.Obstacle)
                    return true;
            }

            return false;
        }

        public Vector3Int ResolveFloorBfsStart(int band, int startX, int startZ)
        {
            var start = new Vector3Int(startX, band, startZ);
            if (!TryGetCellTiles(startX, startZ, band, out var startList) ||
                !CellHasSolidWall(startList))
                return start;

            foreach (var d in CardinalNeighbors)
            {
                int nx = startX + d.x;
                int nz = startZ + d.z;
                if (!TryGetCellTiles(nx, nz, band, out var nList))
                    continue;

                if (!CellHasSolidWall(nList))
                    return new Vector3Int(nx, band, nz);
            }

            return start;
        }
    }
}
