// ============================================================
// BuildingGroupBuilder — buildingId·roomId bake 및 room 단위 재빌드
// ============================================================
using System;
using System.Collections.Generic;
using UnityEngine;

namespace IsoTilemap
{
    public sealed class BuildingGroupBuilder
    {
        static readonly Vector3Int[] CardinalDirs =
        {
            Vector3Int.right, Vector3Int.back, Vector3Int.left, Vector3Int.forward
        };

        readonly TileMapModel _model;
        readonly TileMapCacheHub _hub;
        readonly TopologyLayer _topology;
        readonly BuildingGroupRegistry _registry;
        readonly TileEdgeBinder _edgeBinder;

        int _minBand;
        int _maxBand;

        public BuildingGroupBuilder(TileMapModel model, TileMapCacheHub hub)
        {
            _model = model;
            _hub = hub;
            _topology = hub.Topology;
            _registry = hub.Buildings.Registry;
            _edgeBinder = (TileEdgeBinder)model.EdgeBinder;
        }

        public BuildingGroupRegistry Registry => _registry;

        public void AssignAll()
        {
            _hub.InvalidateAll();
            _registry.Clear();
            ComputeBandRange();
            ResetStructuralIds();
            RecomputeOutdoorFromMin();
            AssignBuildingsFromSeeds(CollectY0BuildingSeeds());
            AssignOrphanFloorBuildings();
            BakeAllRooms();
            _model.ReindexTilesByIdFromRuntime();
            _registry.RebuildFromTiles(_model.TilesSnapshot);
            _registry.RebuildMinBandFloorIndex(_model.TilesSnapshot, _minBand);
            _model.MarkTilesDirty();
        }

        public void RecomputeOutdoorFromMin()
        {
            var oldOutdoor = new HashSet<(int x, int z)>(_registry.PlazaFloorXZ);
            var newOutdoor = ComputeOutdoorXZ();
            _registry.SetPlazaOutdoor(_minBand, newOutdoor);

            foreach (var (x, z) in newOutdoor)
                SetFloorBuildingRoom(x, _minBand, z, TileIdentity.BuildingIdOutdoor, 0);

            foreach (var (x, z) in oldOutdoor)
            {
                if (newOutdoor.Contains((x, z)))
                    continue;

                if (_model.TryGetCellTiles(x, z, _minBand, out _))
                    SetFloorBuildingRoom(x, _minBand, z, TileIdentity.BuildingIdUnassigned, 0);
            }
        }

        public HashSet<RoomKey> CollectAffectedRoomKeys(
            TileData removed,
            IReadOnlyCollection<Vector3Int> changedCells)
        {
            var keys = new HashSet<RoomKey>();
            TryAddRoomKeyFromTile(removed, keys);

            if (changedCells != null)
            {
                foreach (var cell in changedCells)
                    CollectRoomKeysNearCell(cell, keys);
            }

            return keys;
        }

        public void RebuildRooms(HashSet<RoomKey> affectedKeys, HashSet<(int x, int z, int band)> extraSeeds = null)
        {
            if (affectedKeys == null || affectedKeys.Count == 0)
            {
                if (extraSeeds == null || extraSeeds.Count == 0)
                    return;
            }

            _hub.InvalidateRooms(affectedKeys);

            var slices = new HashSet<(int buildingId, int band)>();
            if (affectedKeys != null)
            {
                foreach (var key in affectedKeys)
                {
                    if (key.BuildingId <= 0)
                        continue;

                    ClearRoomIdsOnSlice(key.BuildingId, key.Band);
                    slices.Add((key.BuildingId, key.Band));
                }
            }

            if (extraSeeds != null)
            {
                foreach (var (x, z, band) in extraSeeds)
                {
                    int buildingId = GetFloorBuildingId(x, band, z);
                    if (buildingId <= 0)
                        continue;

                    slices.Add((buildingId, band));
                }
            }

            foreach (var (buildingId, band) in slices)
            {
                _registry.ClearEdgeIndexForSlice(buildingId, band);
                BakeRoomsForSlice(buildingId, band, extraSeeds);
            }

            TagPerimeterForSlices(slices);
            IndexEdgeWallsForSlices(slices);
            _model.ReindexTilesByIdFromRuntime();
            _registry.RebuildFromTiles(_model.TilesSnapshot);
            _registry.RebuildMinBandFloorIndex(_model.TilesSnapshot, _minBand);
            _model.MarkTilesDirty();
        }

        public void HandleRemoveTile(TileData removed, HashSet<Vector3Int> changedCells)
        {
            int buildingId = removed.identity.buildingId;
            int band = removed.identity.GridPos.y;

            if (IsFloorTile(removed) &&
                (buildingId == TileIdentity.BuildingIdOutdoor ||
                 buildingId == TileIdentity.BuildingIdUnassigned))
                RecomputeOutdoorFromMinAndRebuildLost(changedCells);
            else if (buildingId > 0)
                RebuildRooms(CollectAffectedRoomKeys(removed, changedCells));
            else if (IsY0FloorChange(changedCells))
                RecomputeOutdoorFromMinAndRebuildLost(changedCells);
        }

        public void HandleSetOrApply(IReadOnlyCollection<Vector3Int> changedCells)
        {
            if (changedCells == null || changedCells.Count == 0)
                return;

            if (IsY0FloorChange(changedCells))
                RecomputeOutdoorFromMinAndRebuildLost(changedCells);

            var keys = new HashSet<RoomKey>();
            var extraSeeds = new HashSet<(int x, int z, int band)>();

            foreach (var cell in changedCells)
            {
                CollectRoomKeysNearCell(cell, keys);
                extraSeeds.Add((cell.x, cell.z, cell.y));
                foreach (var d in CardinalDirs)
                {
                    var n = cell + d;
                    extraSeeds.Add((n.x, n.z, n.y));
                }
            }

            TryAssignLocalBuildingSeeds(extraSeeds);
            RebuildRooms(keys, extraSeeds);
        }

        void RecomputeOutdoorFromMinAndRebuildLost(IReadOnlyCollection<Vector3Int> changedCells)
        {
            var oldOutdoor = new HashSet<(int x, int z)>(_registry.PlazaFloorXZ);
            RecomputeOutdoorFromMin();

            var lost = new HashSet<(int x, int z)>();
            foreach (var (x, z) in oldOutdoor)
            {
                if (!_registry.IsPlazaXZ(x, z))
                    lost.Add((x, z));
            }

            if (lost.Count > 0)
                AssignBuildingsFromSeeds(lost);

            var keys = new HashSet<RoomKey>();
            if (changedCells != null)
            {
                foreach (var cell in changedCells)
                    CollectRoomKeysNearCell(cell, keys);
            }

            var extraSeeds = new HashSet<(int x, int z, int band)>();
            foreach (var (x, z) in lost)
                extraSeeds.Add((x, z, _minBand));

            RebuildRooms(keys, extraSeeds);
        }

        void ComputeBandRange()
        {
            _minBand = int.MaxValue;
            _maxBand = int.MinValue;

            foreach (var (x, z, band) in _model.EnumerateOccupiedCells())
            {
                if (band < _minBand) _minBand = band;
                if (band > _maxBand) _maxBand = band;
            }

            if (_minBand == int.MaxValue)
            {
                _minBand = 0;
                _maxBand = 0;
            }
        }

        void ResetStructuralIds()
        {
            _model.ForEachRuntimeTile(tile =>
            {
                if (!IsStructural(tile))
                    return;

                _model.PatchTileIdentity(tile.tileDefId, TileIdentity.BuildingIdUnassigned, 0);
            });
        }

        HashSet<(int x, int z)> ComputeOutdoorXZ()
        {
            if (!TryFindOutdoorSeed(out int seedX, out int seedZ))
                return new HashSet<(int x, int z)>();

            var outdoor = FloorRoomFloodFill.Run(
                _topology.Index, _minBand, seedX, seedZ, collectEmptyNeighbors: false).Visited;

            return outdoor;
        }

        bool TryFindOutdoorSeed(out int seedX, out int seedZ)
        {
            seedX = int.MaxValue;
            seedZ = int.MaxValue;
            bool found = false;

            foreach (var (x, z, band) in _model.EnumerateOccupiedCells())
            {
                if (band != _minBand)
                    continue;

                if (!_model.TryGetCellTiles(x, z, band, out var list) || !FloorMapIndex.CellHasFloor(list))
                    continue;

                if (x < seedX || (x == seedX && z < seedZ))
                {
                    seedX = x;
                    seedZ = z;
                    found = true;
                }
            }

            if (!found)
            {
                seedX = 0;
                seedZ = 0;
            }

            return found;
        }

        HashSet<(int x, int z)> CollectY0BuildingSeeds()
        {
            var seeds = new HashSet<(int x, int z)>();

            foreach (var (x, z, band) in _model.EnumerateOccupiedCells())
            {
                if (band != _minBand)
                    continue;

                if (_registry.IsPlazaXZ(x, z))
                    continue;

                if (!_model.TryGetCellTiles(x, z, band, out var list) || !FloorMapIndex.CellHasFloor(list))
                    continue;

                if (!IsFloorBuildingUnassigned(x, band, z))
                    continue;

                seeds.Add((x, z));
            }

            return seeds;
        }

        void AssignBuildingsFromSeeds(IEnumerable<(int x, int z)> seeds)
        {
            if (seeds == null)
                return;

            var outdoor = new HashSet<(int x, int z)>(_registry.PlazaFloorXZ);

            foreach (var (seedX, seedZ) in seeds)
            {
                if (!IsFloorBuildingUnassigned(seedX, _minBand, seedZ))
                    continue;

                if (outdoor.Contains((seedX, seedZ)))
                    continue;

                var footprint = FloorRoomFloodFill.Run(
                    _topology.Index, _minBand, seedX, seedZ,
                    collectEmptyNeighbors: false,
                    excludeCells: outdoor).Visited;

                if (footprint.Count == 0)
                    continue;

                int buildingId = _registry.AllocateBuildingId();
                AssignBuildingToFootprint(buildingId, footprint, _minBand, outdoor);
            }
        }

        void AssignBuildingToFootprint(
            int buildingId,
            HashSet<(int x, int z)> seedFootprint,
            int seedBand,
            HashSet<(int x, int z)> outdoorExclude)
        {
            var columns = new HashSet<(int x, int z)>(seedFootprint);

            for (int band = seedBand; band <= _maxBand; band++)
            {
                var bandFloors = AssignBandFloors(buildingId, band, seedBand, seedFootprint, columns, outdoorExclude);

                var structuralCells = new HashSet<(int x, int z)>(columns);
                foreach (var cell in bandFloors)
                    structuralCells.Add(cell);

                SetStructuralBuildingId(structuralCells, band, buildingId);

                var probeXZ = CollectVerticalProbeXZ(band, buildingId);
                if (band >= _maxBand)
                    break;

                var nextColumns = new HashSet<(int x, int z)>();
                foreach (var (x, z) in probeXZ)
                {
                    if (BuildingVerticalLink.CellHasStructural(
                            _model, x, z, band + 1,
                            includeIncidentEdgeWalls: true))
                        nextColumns.Add((x, z));
                }

                if (nextColumns.Count == 0)
                    break;

                columns = nextColumns;
            }
        }

        HashSet<(int x, int z)> AssignBandFloors(
            int buildingId,
            int band,
            int seedBand,
            HashSet<(int x, int z)> seedFootprint,
            HashSet<(int x, int z)> columns,
            HashSet<(int x, int z)> outdoorExclude)
        {
            var bandFloors = new HashSet<(int x, int z)>();

            if (band == seedBand)
            {
                foreach (var (x, z) in seedFootprint)
                {
                    if (outdoorExclude != null && band == _minBand && outdoorExclude.Contains((x, z)))
                        continue;

                    if (!IsFloorBuildingUnassigned(x, band, z))
                        continue;

                    SetFloorBuildingRoom(x, band, z, buildingId, 0);
                    bandFloors.Add((x, z));
                }

                return bandFloors;
            }

            foreach (var (x, z) in columns)
            {
                if (!_model.TryGetCellTiles(x, z, band, out var list) ||
                    !FloorMapIndex.CellHasFloor(list))
                    continue;

                if (!IsFloorBuildingUnassigned(x, band, z))
                {
                    if (GetFloorBuildingId(x, band, z) == buildingId)
                        bandFloors.Add((x, z));
                    continue;
                }

                var footprint = FloorRoomFloodFill.Run(
                    _topology.Index, band, x, z,
                    collectEmptyNeighbors: false,
                    excludeCells: band == _minBand ? outdoorExclude : null).Visited;

                foreach (var (fx, fz) in footprint)
                {
                    if (!IsFloorBuildingUnassigned(fx, band, fz))
                        continue;

                    SetFloorBuildingRoom(fx, band, fz, buildingId, 0);
                    bandFloors.Add((fx, fz));
                }
            }

            return bandFloors;
        }

        HashSet<(int x, int z)> CollectVerticalProbeXZ(int band, int buildingId)
        {
            var probe = new HashSet<(int x, int z)>();

            foreach (var (x, z, b) in _model.EnumerateOccupiedCells())
            {
                if (b != band)
                    continue;

                if (BuildingVerticalLink.CellHasVerticalSource(
                        _model, x, z, band, buildingId,
                        includeIncidentEdgeWalls: true))
                    probe.Add((x, z));
            }

            return probe;
        }

        void SetStructuralBuildingId(IEnumerable<(int x, int z)> cells, int band, int buildingId)
        {
            foreach (var (x, z) in cells)
            {
                if (!_model.TryGetCellTiles(x, z, band, out var list))
                    continue;

                for (int i = 0; i < list.Count; i++)
                {
                    var tile = list[i];
                    var type = (TileView.TileType)tile.identity.tileType;
                    if (type != TileView.TileType.Wall && type != TileView.TileType.EdgeWall)
                        continue;

                    if (tile.identity.buildingId != TileIdentity.BuildingIdUnassigned)
                        continue;

                    _model.PatchTileIdentity(tile.tileDefId, buildingId, 0);
                }
            }
        }

        void AssignOrphanFloorBuildings()
        {
            var seeds = new HashSet<(int x, int z, int band)>();

            foreach (var (x, z, band) in _model.EnumerateOccupiedCells())
            {
                if (!_model.TryGetCellTiles(x, z, band, out var list) || !FloorMapIndex.CellHasFloor(list))
                    continue;

                if (!IsFloorBuildingUnassigned(x, band, z))
                    continue;

                seeds.Add((x, z, band));
            }

            foreach (var (seedX, seedZ, seedBand) in seeds)
            {
                if (!IsFloorBuildingUnassigned(seedX, seedBand, seedZ))
                    continue;

                var outdoor = new HashSet<(int x, int z)>(_registry.PlazaFloorXZ);
                var footprint = FloorRoomFloodFill.Run(
                    _topology.Index, seedBand, seedX, seedZ,
                    collectEmptyNeighbors: false,
                    excludeCells: seedBand == _minBand ? outdoor : null).Visited;

                if (footprint.Count == 0)
                    continue;

                int buildingId = _registry.AllocateBuildingId();
                HashSet<(int x, int z)> outdoorExclude = seedBand == _minBand ? outdoor : null;
                AssignBuildingToFootprint(buildingId, footprint, seedBand, outdoorExclude);
            }
        }

        void TryAssignLocalBuildingSeeds(HashSet<(int x, int z, int band)> cells)
        {
            if (cells == null || cells.Count == 0)
                return;

            var seeds = new HashSet<(int x, int z)>();
            foreach (var (x, z, band) in cells)
            {
                if (band != _minBand)
                    continue;

                if (_registry.IsPlazaXZ(x, z))
                    continue;

                if (!IsFloorBuildingUnassigned(x, band, z))
                    continue;

                if (_model.TryGetCellTiles(x, z, band, out var list) && FloorMapIndex.CellHasFloor(list))
                    seeds.Add((x, z));
            }

            if (seeds.Count > 0)
                AssignBuildingsFromSeeds(seeds);
        }

        void BakeAllRooms()
        {
            var slices = new HashSet<(int buildingId, int band)>();

            foreach (var tile in _model.TilesSnapshot)
            {
                if (!IsFloorTile(tile))
                    continue;

                int buildingId = tile.identity.buildingId;
                if (buildingId <= 0)
                    continue;

                slices.Add((buildingId, tile.identity.GridPos.y));
            }

            foreach (var (buildingId, band) in slices)
                BakeRoomsForSlice(buildingId, band, null);

            TagPerimeterForSlices(slices);
            IndexEdgeWallsForSlices(slices);
        }

        void IndexEdgeWallsForSlices(HashSet<(int buildingId, int band)> slices)
        {
            foreach (var (buildingId, band) in slices)
                IndexEdgeWallsForSlice(buildingId, band);
        }

        void IndexEdgeWallsForSlice(int buildingId, int band)
        {
            foreach (var edge in _edgeBinder.EnumerateTiles())
            {
                var edgeKey = WallEdgeKey.FromEdgeTileIdentity(edge.identity);
                if (edgeKey.Anchor.y != band)
                    continue;

                var roomKeys = new HashSet<RoomKey>();
                TryAddRoomKeyAtFloorCell(edgeKey.CellA, band, roomKeys);
                TryAddRoomKeyAtFloorCell(edgeKey.CellB, band, roomKeys);

                foreach (var roomKey in roomKeys)
                {
                    if (roomKey.BuildingId != buildingId)
                        continue;

                    _registry.RegisterEdgeForRoom(roomKey, edge.tileDefId);
                }
            }
        }

        void TryAddRoomKeyAtFloorCell(Vector3Int cell, int band, HashSet<RoomKey> roomKeys)
        {
            if (cell.y != band)
                return;

            int b = GetFloorBuildingId(cell.x, band, cell.z);
            int r = GetFloorRoomId(cell.x, band, cell.z);
            if (b > 0 && r > 0)
                roomKeys.Add(new RoomKey(b, band, r));
        }

        void BakeRoomsForSlice(
            int buildingId,
            int band,
            HashSet<(int x, int z, int band)> extraSeeds)
        {
            var visitedAnchors = new HashSet<(int x, int z)>();
            int nextRoomId = 1;

            void TrySeed(int x, int z)
            {
                if (visitedAnchors.Contains((x, z)))
                    return;

                if (GetFloorBuildingId(x, band, z) != buildingId)
                    return;

                if (!_model.TryGetCellTiles(x, z, band, out var list) ||
                    !FloorMapIndex.CellHasFloor(list))
                    return;

                if (GetFloorRoomId(x, band, z) != 0)
                    return;

                var occlusion = FloorRoomFloodFill.Run(
                    _topology.Index, band, x, z, collectEmptyNeighbors: false, buildingId);
                var visibility = FloorRoomFloodFill.Run(
                    _topology.Index, band, x, z, collectEmptyNeighbors: true, buildingId);

                if (occlusion.Visited.Count == 0)
                    return;

                int roomId = nextRoomId++;
                var key = new RoomKey(buildingId, band, roomId);

                foreach (var (vx, vz) in occlusion.Visited)
                {
                    visitedAnchors.Add((vx, vz));
                    SetFloorBuildingRoom(vx, band, vz, buildingId, roomId);
                }

                _hub.Rooms.Store(key, FloorRoomBfsProfile.Occlusion, occlusion);
                _hub.Bands.RegisterFootprint(band, FloorRoomBfsProfile.Occlusion, occlusion);
                _hub.Rooms.Store(key, FloorRoomBfsProfile.Visibility, visibility);
                _hub.Bands.RegisterFootprint(band, FloorRoomBfsProfile.Visibility, visibility);
            }

            foreach (var (x, z, b) in _model.EnumerateOccupiedCells())
            {
                if (b != band)
                    continue;

                TrySeed(x, z);
            }

            if (extraSeeds != null)
            {
                foreach (var (x, z, b) in extraSeeds)
                {
                    if (b != band)
                        continue;

                    TrySeed(x, z);
                }
            }
        }

        void ClearRoomIdsOnSlice(int buildingId, int band)
        {
            _model.ForEachRuntimeTile(tile =>
            {
                if (!IsFloorTile(tile))
                    return;

                var pos = tile.identity.GridPos;
                if (pos.y != band || tile.identity.buildingId != buildingId)
                    return;

                _model.PatchTileIdentity(tile.tileDefId, buildingId, 0);
            });
        }

        void TagPerimeterForSlices(HashSet<(int buildingId, int band)> slices)
        {
            foreach (var (buildingId, band) in slices)
                TagPerimeterForSlice(buildingId, band);
        }

        void TagPerimeterForSlice(int buildingId, int band)
        {
            _model.ForEachRuntimeTile(tile =>
            {
                var type = (TileView.TileType)tile.identity.tileType;
                if (type != TileView.TileType.Wall &&
                    type != TileView.TileType.EdgeWall &&
                    type != TileView.TileType.Obstacle)
                    return;

                var pos = tile.identity.GridPos;
                if (pos.y != band)
                    return;

                if (!TryGetAdjacentFloorRoom(pos, out int adjBuilding, out int adjRoom))
                    return;

                if (adjBuilding != buildingId)
                    return;

                _model.PatchTileIdentity(tile.tileDefId, buildingId, adjRoom);
            });
        }

        bool TryGetAdjacentFloorRoom(Vector3Int cell, out int buildingId, out int roomId)
        {
            buildingId = 0;
            roomId = 0;

            foreach (var d in CardinalDirs)
            {
                int nx = cell.x + d.x;
                int nz = cell.z + d.z;
                int band = cell.y;

                if (!_model.TryGetCellTiles(nx, nz, band, out var list) || !FloorMapIndex.CellHasFloor(list))
                    continue;

                int r = GetFloorRoomId(nx, band, nz);
                int b = GetFloorBuildingId(nx, band, nz);
                if (b > 0 && r > 0)
                {
                    buildingId = b;
                    roomId = r;
                    return true;
                }
            }

            return false;
        }

        void CollectRoomKeysNearCell(Vector3Int cell, HashSet<RoomKey> keys)
        {
            CollectRoomKeysAt(cell.x, cell.y, cell.z, keys);

            foreach (var d in CardinalDirs)
            {
                var n = cell + d;
                CollectRoomKeysAt(n.x, n.y, n.z, keys);
            }

            int below = cell.y - 1;
            int above = cell.y + 1;
            if (below >= _minBand)
                CollectRoomKeysAt(cell.x, below, cell.z, keys);
            if (above <= _maxBand)
                CollectRoomKeysAt(cell.x, above, cell.z, keys);
        }

        void CollectRoomKeysAt(int x, int band, int z, HashSet<RoomKey> keys)
        {
            int buildingId = GetFloorBuildingId(x, band, z);
            int roomId = GetFloorRoomId(x, band, z);
            if (buildingId > 0 && roomId > 0)
                keys.Add(new RoomKey(buildingId, band, roomId));
        }

        void TryAddRoomKeyFromTile(TileData tile, HashSet<RoomKey> keys)
        {
            if (IsFloorTile(tile) && tile.identity.buildingId > 0 && tile.identity.roomId > 0)
            {
                var pos = tile.identity.GridPos;
                keys.Add(new RoomKey(tile.identity.buildingId, pos.y, tile.identity.roomId));
                return;
            }

            if (IsWallLike(tile))
            {
                var pos = tile.identity.GridPos;
                CollectRoomKeysNearCell(pos, keys);
            }
        }

        bool IsY0FloorChange(IReadOnlyCollection<Vector3Int> cells)
        {
            if (cells == null)
                return false;

            foreach (var c in cells)
            {
                if (c.y == _minBand)
                    return true;
            }

            return false;
        }

        bool IsFloorBuildingUnassigned(int x, int band, int z) =>
            GetFloorBuildingId(x, band, z) == TileIdentity.BuildingIdUnassigned;

        int GetFloorBuildingId(int x, int band, int z)
        {
            if (!_model.TryGetCellTiles(x, z, band, out var list))
                return TileIdentity.BuildingIdUnassigned;

            for (int i = 0; i < list.Count; i++)
            {
                if ((TileView.TileType)list[i].identity.tileType != TileView.TileType.Floor)
                    continue;

                return list[i].identity.buildingId;
            }

            return TileIdentity.BuildingIdUnassigned;
        }

        int GetFloorRoomId(int x, int band, int z)
        {
            if (!_model.TryGetCellTiles(x, z, band, out var list))
                return 0;

            for (int i = 0; i < list.Count; i++)
            {
                if ((TileView.TileType)list[i].identity.tileType != TileView.TileType.Floor)
                    continue;

                return list[i].identity.roomId;
            }

            return 0;
        }

        void SetFloorBuildingRoom(int x, int band, int z, int buildingId, int roomId)
        {
            if (!_model.TryGetCellTiles(x, z, band, out var list))
                return;

            for (int i = 0; i < list.Count; i++)
            {
                if ((TileView.TileType)list[i].identity.tileType != TileView.TileType.Floor)
                    continue;

                _model.PatchTileIdentity(list[i].tileDefId, buildingId, roomId);
            }
        }

        static bool IsStructural(TileData tile)
        {
            var type = (TileView.TileType)tile.identity.tileType;
            return type is TileView.TileType.Floor
                or TileView.TileType.Wall
                or TileView.TileType.EdgeWall
                or TileView.TileType.Obstacle;
        }

        static bool IsFloorTile(TileData tile) =>
            (TileView.TileType)tile.identity.tileType == TileView.TileType.Floor;

        static bool IsWallLike(TileData tile)
        {
            var type = (TileView.TileType)tile.identity.tileType;
            return type is TileView.TileType.Wall
                or TileView.TileType.EdgeWall
                or TileView.TileType.Obstacle;
        }
    }
}
