// ============================================================
// BuildingGroupRegistry — buildingId 역인덱스·야외 집합·room별 EdgeWall
// ============================================================
using System;
using System.Collections.Generic;

namespace IsoTilemap
{
    public sealed class BuildingGroupRegistry
    {
        static readonly Guid[] EmptyEdgeGuids = Array.Empty<Guid>();

        readonly Dictionary<int, HashSet<Guid>> _tilesByBuildingId = new();
        readonly Dictionary<RoomKey, HashSet<Guid>> _edgeIdsByRoom = new();
        readonly HashSet<(int x, int z)> _outdoorXZ = new();
        readonly HashSet<RoomKey> _openOutdoorRooms = new();

        public int NextBuildingId { get; private set; } = 1;

        public IReadOnlyCollection<RoomKey> OpenOutdoorRooms => _openOutdoorRooms;

        public IReadOnlyDictionary<int, HashSet<Guid>> TilesByBuildingId => _tilesByBuildingId;

        public IReadOnlyCollection<(int x, int z)> OutdoorXZ => _outdoorXZ;

        public void Clear()
        {
            _tilesByBuildingId.Clear();
            _edgeIdsByRoom.Clear();
            _outdoorXZ.Clear();
            _openOutdoorRooms.Clear();
            NextBuildingId = 1;
        }

        public void SetOutdoorXZ(HashSet<(int x, int z)> outdoor)
        {
            _outdoorXZ.Clear();
            if (outdoor == null)
                return;

            foreach (var cell in outdoor)
                _outdoorXZ.Add(cell);
        }

        public bool IsOutdoor(int x, int z) => _outdoorXZ.Contains((x, z));

        public bool IsOpenOutdoorRoom(RoomKey roomKey) => _openOutdoorRooms.Contains(roomKey);

        public void RegisterOpenOutdoorRoom(RoomKey roomKey)
        {
            if (roomKey.BuildingId > 0 && roomKey.RoomId > 0)
                _openOutdoorRooms.Add(roomKey);
        }

        public void ClearOpenOutdoorRoomsForSlice(int buildingId, int band)
        {
            if (buildingId <= 0)
                return;

            var toRemove = new List<RoomKey>();
            foreach (var key in _openOutdoorRooms)
            {
                if (key.BuildingId == buildingId && key.Band == band)
                    toRemove.Add(key);
            }

            for (int i = 0; i < toRemove.Count; i++)
                _openOutdoorRooms.Remove(toRemove[i]);
        }

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

        public void ClearEdgeIndexForSlice(int buildingId, int band)
        {
            if (buildingId <= 0)
                return;

            var toRemove = new List<RoomKey>();
            foreach (var kv in _edgeIdsByRoom)
            {
                if (kv.Key.BuildingId == buildingId && kv.Key.Band == band)
                    toRemove.Add(kv.Key);
            }

            for (int i = 0; i < toRemove.Count; i++)
                _edgeIdsByRoom.Remove(toRemove[i]);

            ClearOpenOutdoorRoomsForSlice(buildingId, band);
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

        public void RebuildFromTiles(IEnumerable<TileData> tiles)
        {
            _tilesByBuildingId.Clear();

            if (tiles == null)
                return;

            foreach (var tile in tiles)
            {
                int id = tile.identity.buildingId;
                if (id > 0)
                    RegisterTile(tile.tileDefId, id);
            }
        }

        public HashSet<Guid> GetTilesForBuilding(int buildingId)
        {
            if (buildingId <= 0 || !_tilesByBuildingId.TryGetValue(buildingId, out var set))
                return new HashSet<Guid>();

            return new HashSet<Guid>(set);
        }
    }
}
