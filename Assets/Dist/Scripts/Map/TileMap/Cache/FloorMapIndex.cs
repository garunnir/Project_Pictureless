// ============================================================
// FloorMapIndex — 셀 Y별 타일·바닥 face·벽·엣지 조회 스냅샷
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
        /// 앵커 셀 → 타일 리스트(런타임에서도 동일 List 인스턴스를 유지). Floor는 포함하지 않습니다.
        /// </summary>
        readonly Dictionary<Vector3Int, List<TileData>> _tiles;
        readonly IReadOnlyDictionary<WallEdgeKey, TileData> _edges;
        readonly IReadOnlyDictionary<FloorFaceKey, TileData> _faces;

        /// <summary>
        /// 점유 셀 → (원본 List, 인덱스)들의 목록.
        /// TileData는 struct이므로, 점유 셀 조회 시 매번 원본을 읽어 최신 buildingId 값을 반영합니다.
        /// </summary>
        Dictionary<Vector3Int, OccupiedCellEntry> _occupiedEntries = new();

        readonly HashSet<(int x, int z, int y)> _anyTileAt = new();

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
            IReadOnlyDictionary<WallEdgeKey, TileData> edges,
            IReadOnlyDictionary<FloorFaceKey, TileData> faces)
        {
            _tiles = tiles;
            _edges = edges ?? new Dictionary<WallEdgeKey, TileData>();
            _faces = faces ?? new Dictionary<FloorFaceKey, TileData>();
            RebuildOccupancy();
        }

        public static FloorMapIndex FromModel(TileMapModel model) =>
            new FloorMapIndex(model.tiles, model.EdgeBinder.EdgeIndex, model.FloorFaceBinder.FaceIndex);

        public bool HasAnyTile(int x, int z, int y) => _anyTileAt.Contains((x, z, y));

        public IEnumerable<(int x, int z, int y)> EnumerateOccupiedCells() => _anyTileAt;

        /// <summary>런타임 topology 변경 후 (x,z,y) 점유 집합을 <see cref="_tiles"/>와 맞춥니다.</summary>
        public void SyncOccupancyForCell(int x, int z, int y) => RebuildOccupancy();

        public void SyncOccupancyFromChangedCells(IEnumerable<Vector3Int> changedCells) => RebuildOccupancy();

        public void RebuildOccupancy()
        {
            _anyTileAt.Clear();
            _occupiedEntries.Clear();

            foreach (var kv in _tiles)
            {
                var list = kv.Value;
                if (list == null || list.Count == 0)
                    continue;

                for (int i = 0; i < list.Count; i++)
                {
                    TileData tile = list[i];
                    if ((TileView.TileType)tile.identity.tileType == TileView.TileType.Floor)
                        continue;

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

            foreach (var kv in _faces)
            {
                var faceKey = kv.Key;
                int sy = kv.Value.identity.sizeUnit.y;
                if (sy < 1) sy = 1;

                for (int dy = 0; dy < sy; dy++)
                {
                    var yOffset = new Vector3Int(0, dy, 0);
                    var below = faceKey.CellBelow + yOffset;
                    var above = faceKey.CellAbove + yOffset;
                    _anyTileAt.Add((below.x, below.z, below.y));
                    _anyTileAt.Add((above.x, above.z, above.y));
                }
            }
        }

        public bool TryGetCellTiles(int x, int z, int cellY, out List<TileData> list) =>
            TryGetCellTiles(new Vector3Int(x, cellY, z), out list);

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

        public bool TryGetHorizontalFaceBetween(Vector3Int cellBelow, Vector3Int cellAbove, out TileData face)
        {
            face = default;
            return FloorFaceKey.TryBetween(cellBelow, cellAbove, out var faceKey) &&
                   _faces.TryGetValue(faceKey, out face);
        }

        public bool TryGetFloorFaceForWalkableCell(int x, int cellY, int z, out TileData face) =>
            TryGetHorizontalFaceBetween(
                new Vector3Int(x, cellY - 1, z),
                new Vector3Int(x, cellY, z),
                out face);

        public bool CellHasFloor(int x, int cellY, int z)
        {
            if (!TryGetFloorFaceForWalkableCell(x, cellY, z, out var face))
                return false;

            return TileCollisionFlagsUtil.Has(
                face.identity.collisionFlags,
                TileCollisionFlags.ProvidesLogicalFloor);
        }

        public IEnumerable<TileData> EnumerateEdgeTiles()
        {
            foreach (var kv in _edges)
                yield return kv.Value;
        }

        public IEnumerable<TileData> EnumerateFaceTiles()
        {
            foreach (var kv in _faces)
                yield return kv.Value;
        }

        /// <summary>등록된 Floor face의 walkable 셀(CellAbove)을 순회합니다.</summary>
        public IEnumerable<(int x, int cellY, int z)> EnumerateWalkableFloorCells()
        {
            foreach (var kv in _faces)
            {
                if (!TileCollisionFlagsUtil.Has(
                        kv.Value.identity.collisionFlags,
                        TileCollisionFlags.ProvidesLogicalFloor))
                    continue;

                var key = kv.Key;
                int sy = kv.Value.identity.sizeUnit.y;
                if (sy < 1) sy = 1;

                for (int dy = 0; dy < sy; dy++)
                {
                    var above = key.CellAbove + new Vector3Int(0, dy, 0);
                    yield return (above.x, above.y, above.z);
                }
            }
        }

        /// <summary>점유 셀 타일 리스트에 Floor collision이 있는지(레거시 호환).</summary>
        public static bool CellHasFloor(IReadOnlyList<TileData> list) =>
            TileCollisionFlagsUtil.CellProvidesLogicalFloor(list);

        public static bool CellHasSolidWall(IReadOnlyList<TileData> list) =>
            TileCollisionFlagsUtil.CellBlocksOccupied(list);

        public bool EdgeBlocksPassage(Vector3Int cellA, Vector3Int cellB)
        {
            if (!TryGetEdgeBetween(cellA, cellB, out var edge))
                return false;

            return TileCollisionFlagsUtil.EdgeBlocksPassage(edge);
        }

        public bool EdgeSeparatesRoom(Vector3Int cellA, Vector3Int cellB)
        {
            if (!TryGetEdgeBetween(cellA, cellB, out var edge))
                return false;

            return TileCollisionFlagsUtil.EdgeSeparatesRoom(edge);
        }

        public Vector3Int ResolveFloorBfsStart(int cellY, int startX, int startZ)
        {
            var start = new Vector3Int(startX, cellY, startZ);
            if (!TryGetCellTiles(startX, startZ, cellY, out var startList) ||
                !CellHasSolidWall(startList))
                return start;

            foreach (var d in CardinalNeighbors)
            {
                int nx = startX + d.x;
                int nz = startZ + d.z;
                if (!TryGetCellTiles(nx, nz, cellY, out var nList))
                    continue;

                if (!CellHasSolidWall(nList))
                    return new Vector3Int(nx, cellY, nz);
            }

            return start;
        }
    }
}
