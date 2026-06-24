// ============================================================
// BuildingGroupRegistry — buildingId 역인덱스·광장 바닥 집합·room별 EdgeWall
// ============================================================
using System;
using System.Collections.Generic;

namespace IsoTilemap
{
    public sealed class BuildingGroupRegistry
    {
        static readonly Guid[] EmptyEdgeGuids = Array.Empty<Guid>();

        readonly Dictionary<int, HashSet<Guid>> _tilesByBuildingId = new();
        readonly Dictionary<int, HashSet<Guid>> _minCellYFloorTilesByBuildingId = new();
        readonly Dictionary<int, BuildingExtent> _extentsByBuildingId = new();
        readonly Dictionary<RoomKey, HashSet<Guid>> _edgeIdsByRoom = new();
        readonly HashSet<(int x, int z)> _plazaFloorXZ = new();
        int _plazaCellY = int.MinValue;

        public int NextBuildingId { get; private set; } = 1;

        public IReadOnlyDictionary<int, HashSet<Guid>> TilesByBuildingId => _tilesByBuildingId;

        public int PlazaCellY => _plazaCellY;

        public IReadOnlyCollection<(int x, int z)> PlazaFloorXZ => _plazaFloorXZ;

        public void Clear()
        {
            _tilesByBuildingId.Clear();
            _minCellYFloorTilesByBuildingId.Clear();
            _extentsByBuildingId.Clear();
            _edgeIdsByRoom.Clear();
            _plazaFloorXZ.Clear();
            _plazaCellY = int.MinValue;
            NextBuildingId = 1;
        }

        public void SetPlazaOutdoor(int plazaCellY, HashSet<(int x, int z)> plazaFloor)
        {
            _plazaCellY = plazaCellY;
            _plazaFloorXZ.Clear();
            if (plazaFloor == null)
                return;

            foreach (var cell in plazaFloor)
                _plazaFloorXZ.Add(cell);
        }

        public bool IsPlazaFloor(int cellY, int x, int z) =>
            cellY == _plazaCellY && _plazaFloorXZ.Contains((x, z));

        public bool IsPlazaXZ(int x, int z) => _plazaFloorXZ.Contains((x, z));

        public int AllocateBuildingId() => NextBuildingId++;

        public void RegisterTile(Guid tileId, int buildingId)
        {
            if (buildingId <= 0)
                return;

            if (!_tilesByBuildingId.TryGetValue(buildingId, out var set))
            {
                set = new HashSet<Guid>();
                _tilesByBuildingId[buildingId] = set;
            }

            set.Add(tileId);
        }

        public void UnregisterTile(Guid tileId, int buildingId)
        {
            if (buildingId <= 0)
                return;

            if (_tilesByBuildingId.TryGetValue(buildingId, out var set))
            {
                set.Remove(tileId);
                if (set.Count == 0)
                    _tilesByBuildingId.Remove(buildingId);
            }
        }

        public void RegisterEdgeForRoom(RoomKey roomKey, Guid edgeTileId)
        {
            if (roomKey.BuildingId <= 0 || roomKey.RoomId <= 0)
                return;

            if (!_edgeIdsByRoom.TryGetValue(roomKey, out var set))
            {
                set = new HashSet<Guid>();
                _edgeIdsByRoom[roomKey] = set;
            }

            set.Add(edgeTileId);
        }

        public void ClearEdgeIndexForSlice(int buildingId, int cellY)
        {
            if (buildingId <= 0)
                return;

            var toRemove = new List<RoomKey>();
            foreach (var kv in _edgeIdsByRoom)
            {
                if (kv.Key.BuildingId == buildingId && kv.Key.CellY == cellY)
                    toRemove.Add(kv.Key);
            }

            for (int i = 0; i < toRemove.Count; i++)
                _edgeIdsByRoom.Remove(toRemove[i]);
        }

        public bool TryGetEdgeWallIds(RoomKey roomKey, out IReadOnlyCollection<Guid> edgeIds)
        {
            if (_edgeIdsByRoom.TryGetValue(roomKey, out var set))
            {
                edgeIds = set;
                return true;
            }

            edgeIds = EmptyEdgeGuids;
            return false;
        }

        public void RebuildFromTiles(IEnumerable<TileData> tiles) =>
            RebuildIndicesFromTiles(tiles);

        /// <summary>tile guid 역인덱스·최하층 floor·<see cref="BuildingExtent"/>를 한 패스로 재구성합니다.</summary>
        public void RebuildIndicesFromTiles(IEnumerable<TileData> tiles)
        {
            _tilesByBuildingId.Clear();
            _minCellYFloorTilesByBuildingId.Clear();
            _extentsByBuildingId.Clear();

            if (tiles == null)
                return;

            var extentBuilders = new Dictionary<int, BuildingExtent.Builder>();

            foreach (var tile in tiles)
            {
                int buildingId = tile.identity.buildingId;
                if (buildingId <= 0)
                    continue;

                RegisterTile(tile.tileDefId, buildingId);

                if (!extentBuilders.TryGetValue(buildingId, out var builder))
                {
                    builder = new BuildingExtent.Builder(buildingId);
                    extentBuilders[buildingId] = builder;
                }

                builder.IncludeTile(tile);
            }

            foreach (var kv in extentBuilders)
            {
                var extent = kv.Value.Build();
                if (extent.HasBounds)
                    _extentsByBuildingId[kv.Key] = extent;

                foreach (Guid tileId in kv.Value.MinFloorTileIds)
                    RegisterMinCellYFloorTile(kv.Key, tileId);
            }
        }

        public bool TryGetBuildingExtent(int buildingId, out BuildingExtent extent)
        {
            if (buildingId > 0 && _extentsByBuildingId.TryGetValue(buildingId, out extent))
                return true;

            extent = BuildingExtent.Empty;
            return false;
        }

        [Obsolete("Use RebuildIndicesFromTiles — min floor index is included.")]
        public void RebuildMinCellYFloorIndex(IEnumerable<TileData> tiles, int MinCellY)
        {
            _ = MinCellY;
            RebuildIndicesFromTiles(tiles);
        }

        public HashSet<Guid> GetTilesForBuilding(int buildingId)
        {
            if (buildingId <= 0 || !_tilesByBuildingId.TryGetValue(buildingId, out var set))
                return new HashSet<Guid>();

            return new HashSet<Guid>(set);
        }

        public void EnumerateTilesForBuilding(int buildingId, Action<Guid> visitor)
        {
            if (buildingId <= 0 || visitor == null ||
                !_tilesByBuildingId.TryGetValue(buildingId, out var set))
                return;

            foreach (Guid tileId in set)
                visitor(tileId);
        }

        public IReadOnlyCollection<Guid> GetTileIdsReadOnly(int buildingId)
        {
            if (buildingId <= 0 || !_tilesByBuildingId.TryGetValue(buildingId, out var set))
                return Array.Empty<Guid>();

            return set;
        }

        public bool IsBottomFloorTile(int buildingId, Guid tileId)
        {
            if (buildingId <= 0 ||
                !_minCellYFloorTilesByBuildingId.TryGetValue(buildingId, out var set))
                return false;

            return set.Contains(tileId);
        }

        public void RegisterMinCellYFloorTile(int buildingId, Guid tileId)
        {
            if (buildingId <= 0)
                return;

            if (!_minCellYFloorTilesByBuildingId.TryGetValue(buildingId, out var set))
            {
                set = new HashSet<Guid>();
                _minCellYFloorTilesByBuildingId[buildingId] = set;
            }

            set.Add(tileId);
        }

        public IReadOnlyCollection<Guid> GetMinCellYFloorTilesForBuilding(int buildingId)
        {
            if (buildingId <= 0 ||
                !_minCellYFloorTilesByBuildingId.TryGetValue(buildingId, out var set))
                return Array.Empty<Guid>();

            return set;
        }
    }
}
