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
