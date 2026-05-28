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

        /// <summary>
        /// 앵커 셀 → 타일 리스트(런타임에서도 동일 List 인스턴스를 유지).
        /// </summary>
        readonly Dictionary<Vector3Int, List<TileData>> _tiles;
        readonly IReadOnlyDictionary<WallEdgeKey, TileData> _edges;

        /// <summary>
        /// 점유 셀 → (원본 List, 인덱스)들의 목록.
        /// TileData는 struct이므로, 점유 셀 조회 시 매번 원본을 읽어 최신 buildingId 값을 반영합니다.
        /// </summary>
        Dictionary<Vector3Int, OccupiedCellEntry> _occupiedEntries = new();

        readonly HashSet<(int x, int z, int band)> _anyTileAt = new();

        readonly struct TileRef
        {
            public readonly List<TileData> OwnerList;
            public readonly int OwnerIndex;

            public TileRef(List<TileData> ownerList, int ownerIndex)
            {
                OwnerList = ownerList;
                OwnerIndex = ownerIndex;
            }
        }

        sealed class OccupiedCellEntry
        {
            public readonly List<TileRef> Refs = new();
            public readonly List<TileData> Scratch = new();
        }

        public FloorMapIndex(
            Dictionary<Vector3Int, List<TileData>> tiles,
            IReadOnlyDictionary<WallEdgeKey, TileData> edges)
        {
            _tiles = tiles;
            _edges = edges ?? new Dictionary<WallEdgeKey, TileData>();
            RebuildOccupancy();
        }

        public static FloorMapIndex FromModel(TileMapModel model) =>
            new FloorMapIndex(model.tiles, model.EdgeBinder.EdgeIndex);

        public bool HasAnyTile(int x, int z, int band) => _anyTileAt.Contains((x, z, band));

        public IEnumerable<(int x, int z, int band)> EnumerateOccupiedCells() => _anyTileAt;

        /// <summary>런타임 topology 변경 후 (x,z,band) 점유 집합을 <see cref="_tiles"/>와 맞춥니다.</summary>
        public void SyncOccupancyForCell(int x, int z, int band)
        {
            // 멀티 점유(sizeUnit) 타일이 있을 수 있어, 단일 셀 증분 sync가 안전하지 않습니다.
            // topology 변경 빈도가 낮다는 전제 하에 전체 rebuild로 정합성을 우선합니다.
            RebuildOccupancy();
        }

        public void SyncOccupancyFromChangedCells(IEnumerable<Vector3Int> changedCells)
        {
            if (changedCells == null)
                return;

            // 변경 셀 기준으로 부분 rebuild가 가능하지만, sizeUnit 확장으로 인해 영향을 받는 셀이 늘 수 있습니다.
            RebuildOccupancy();
        }

        public void RebuildOccupancy() 
        {
            _anyTileAt.Clear();
            _occupiedEntries.Clear();

            // anchor 셀에 있는 tile들을 sizeUnit(x,y,z)만큼 확장해서 점유 셀 엔트리를 만듭니다.
            // TryGetCellTiles가 점유 셀 기준으로 동작하도록 하여, 멀티 점유에서 lookup 누락이 나지 않게 합니다.
            foreach (var kv in _tiles)
            {
                var list = kv.Value;
                if (list == null || list.Count == 0)
                    continue;

                for (int i = 0; i < list.Count; i++)
                {
                    TileData tile = list[i];
                    int sx = tile.identity.sizeUnit.x;
                    int sy = tile.identity.sizeUnit.y;
                    int sz = tile.identity.sizeUnit.z;

                    if (sx < 1) sx = 1;
                    if (sy < 1) sy = 1;
                    if (sz < 1) sz = 1;

                    Vector3Int basePos = tile.identity.GridPos;
                    for (int dx = 0; dx < sx; dx++)
                    {
                        for (int dy = 0; dy < sy; dy++)
                        {
                            for (int dz = 0; dz < sz; dz++)
                            {
                                var cell = new Vector3Int(basePos.x + dx, basePos.y + dy, basePos.z + dz);
                                if (!_occupiedEntries.TryGetValue(cell, out var entry))
                                {
                                    entry = new OccupiedCellEntry();
                                    _occupiedEntries[cell] = entry;
                                }

                                entry.Refs.Add(new TileRef(list, i));
                            }
                        }
                    }
                }
            }

            foreach (var kv in _occupiedEntries)
            {
                if (kv.Value.Refs.Count > 0)
                    _anyTileAt.Add((kv.Key.x, kv.Key.z, kv.Key.y));
            }

            // edge는 TryGetEdgeBetween으로만 막히므로, 점유 셀 lookup에는 포함하지 않지만
            // EnumerateOccupiedCells/HasAnyTile에는 sizeUnit.y만큼 양 끝 Y 슬라이스를 포함합니다.
            foreach (var kv in _edges)
            {
                var edgeKey = kv.Key;
                int sy = kv.Value.identity.sizeUnit.y;
                if (sy < 1) sy = 1;

                for (int dy = 0; dy < sy; dy++)
                {
                    var yOffset = new Vector3Int(0, dy, 0);
                    var cellA = edgeKey.CellA + yOffset;
                    var cellB = edgeKey.CellB + yOffset;
                    _anyTileAt.Add((cellA.x, cellA.z, cellA.y));
                    _anyTileAt.Add((cellB.x, cellB.z, cellB.y));
                }
            }
        }

        public bool TryGetCellTiles(int x, int z, int band, out List<TileData> list) =>
            TryGetCellTiles(new Vector3Int(x, band, z), out list);

        bool TryGetCellTiles(Vector3Int cellPos, out List<TileData> list)
        {
            if (!_occupiedEntries.TryGetValue(cellPos, out var entry) || entry.Refs.Count == 0)
            {
                list = null;
                return false;
            }

            entry.Scratch.Clear();
            for (int i = 0; i < entry.Refs.Count; i++)
            {
                var tr = entry.Refs[i];
                entry.Scratch.Add(tr.OwnerList[tr.OwnerIndex]);
            }

            list = entry.Scratch;
            return true;
        }

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
