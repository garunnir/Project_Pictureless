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

        /// <summary>맵 bake 후 MinCellY Floor 타일 guid 집합을 타일 스냅샷에서 재구성합니다.</summary>
        public void RebuildMinCellYFloorIndex(IEnumerable<TileData> tiles, int MinCellY)
        {
            _minCellYFloorTilesByBuildingId.Clear();
            if (tiles == null)
                return;

            foreach (var tile in tiles)
            {
                int buildingId = tile.identity.buildingId;
                if (buildingId <= 0)
                    continue;

                if (tile.identity.GridPos.y != MinCellY)
                    continue;

                if ((TileView.TileType)tile.identity.tileType != TileView.TileType.Floor)
                    continue;

                RegisterMinCellYFloorTile(buildingId, tile.tileDefId);
            }
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
