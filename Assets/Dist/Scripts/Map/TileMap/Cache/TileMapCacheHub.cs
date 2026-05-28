// ============================================================
// TileMapCacheHub — 맵 topology·건물·band·room geometry 통합 조회·무효화
// ============================================================
// 계약:
// - 캐시 히트 = FloorRoomFloodFill 재실행만 생략. 오클루전 delta·TileViewPresentationApplier 반영은 매번 수행.
// - 프레젠테이션(오클루전·고스트·선택)은 모델 TileState에 미기록. ApplyTiles/SetTile/RemoveTile = 구조·토폴로지.
// - InvalidateAll = topology 변경 시. 플레이어 셀 이동만으로는 무효화하지 않음.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace IsoTilemap
{
    /// <summary>BFS 옵션 프로필. 캐시 키에 포함되며 소비자별 계약을 분리합니다.</summary>
    public enum FloorRoomBfsProfile
    {
        /// <summary>벽 오클루전: <c>collectEmptyNeighbors: false</c></summary>
        Occlusion,

        /// <summary>층 가시성(구멍·아래층): <c>collectEmptyNeighbors: true</c></summary>
        Visibility,
    }

    public readonly struct GeometryQuery
    {
        public FloorBfsResult Result { get; }
        public RoomKey? RoomKey { get; }

        public GeometryQuery(FloorBfsResult result, RoomKey? roomKey)
        {
            Result = result;
            RoomKey = roomKey;
        }

        public HashSet<(int x, int z)> Visited => Result.Visited;
    }

    /// <summary>셀·엣지 topology 조회 및 점유 (x,z,band) 동기화.</summary>
    public sealed class TopologyLayer
    {
        readonly FloorMapIndex _index;

        internal TopologyLayer(FloorMapIndex index) => _index = index;

        public FloorMapIndex Index => _index;

        public bool HasOccupancy(int x, int z, int band) => _index.HasAnyTile(x, z, band);

        public IEnumerable<(int x, int z, int band)> EnumerateOccupiedCells() =>
            _index.EnumerateOccupiedCells();

        public bool TryGetCellTiles(int x, int z, int band, out List<TileData> list) =>
            _index.TryGetCellTiles(x, z, band, out list);

        public bool TryGetEdgeBetween(Vector3Int cellA, Vector3Int cellB, out TileData edgeWall) =>
            _index.TryGetEdgeBetween(cellA, cellB, out edgeWall);

        public Vector3Int ResolveFloorBfsStart(int band, int startX, int startZ) =>
            _index.ResolveFloorBfsStart(band, startX, startZ);

        public void SyncOccupancyFromChangedCells(IEnumerable<Vector3Int> changedCells) =>
            _index.SyncOccupancyFromChangedCells(changedCells);

        public void RebuildOccupancy() => _index.RebuildOccupancy();
    }

    /// <summary>buildingId·outdoor·room edge wall 역인덱스 (읽기 + Builder 쓰기).</summary>
    public sealed class BuildingLayer
    {
        readonly BuildingGroupRegistry _registry;

        internal BuildingLayer(BuildingGroupRegistry registry) => _registry = registry;

        public BuildingGroupRegistry Registry => _registry;

        public bool IsPlazaFloor(int band, int x, int z) => _registry.IsPlazaFloor(band, x, z);

        public bool TryGetEdgeWalls(RoomKey roomKey, out IReadOnlyCollection<Guid> edgeIds) =>
            _registry.TryGetEdgeWallIds(roomKey, out edgeIds);

        public bool TryGetFloorBuildingRoom(int band, int x, int z, TopologyLayer topology, out int buildingId, out int roomId)
        {
            buildingId = 0;
            roomId = 0;

            if (!topology.TryGetCellTiles(x, z, band, out var list) || !FloorMapIndex.CellHasFloor(list))
                return false;

            for (int i = 0; i < list.Count; i++)
            {
                if ((TileView.TileType)list[i].identity.tileType != TileView.TileType.Floor)
                    continue;

                buildingId = list[i].identity.buildingId;
                roomId = list[i].identity.roomId;
                return true;
            }

            return false;
        }

        public int TryGetBuildingIdAtCell(int band, int x, int z, TopologyLayer topology) =>
            TryGetFloorBuildingRoom(band, x, z, topology, out int buildingId, out _) ? buildingId : 0;
    }

    /// <summary>RoomKey + profile 단위 bake된 BFS geometry.</summary>
    public sealed class RoomGeometryLayer
    {
        readonly Dictionary<(RoomKey key, FloorRoomBfsProfile profile), FloorBfsResult> _byRoomProfile = new();

        public void InvalidateAll() => _byRoomProfile.Clear();

        public void InvalidateRooms(IEnumerable<RoomKey> keys, BandGeometryLayer bands)
        {
            if (keys == null)
                return;

            foreach (var key in keys)
            {
                if (TryGet(key, FloorRoomBfsProfile.Occlusion, out var occ))
                    bands.UnregisterFootprint(key.Band, FloorRoomBfsProfile.Occlusion, occ);
                if (TryGet(key, FloorRoomBfsProfile.Visibility, out var vis))
                    bands.UnregisterFootprint(key.Band, FloorRoomBfsProfile.Visibility, vis);

                _byRoomProfile.Remove((key, FloorRoomBfsProfile.Occlusion));
                _byRoomProfile.Remove((key, FloorRoomBfsProfile.Visibility));
            }
        }

        public void Store(RoomKey key, FloorRoomBfsProfile profile, FloorBfsResult result) =>
            _byRoomProfile[(key, profile)] = result;

        public bool TryGet(RoomKey key, FloorRoomBfsProfile profile, out FloorBfsResult result) =>
            _byRoomProfile.TryGetValue((key, profile), out result);
    }

    /// <summary>(band, profile)별 (x,z) → BFS 결과 역인덱스. lazy 선형 탐색 대체.</summary>
    public sealed class BandGeometryLayer
    {
        readonly TopologyLayer _topology;
        readonly RoomGeometryLayer _rooms;

        readonly Dictionary<(int band, FloorRoomBfsProfile profile), Dictionary<(int x, int z), FloorBfsResult>>
            _cellToResult = new();

        internal BandGeometryLayer(TopologyLayer topology, RoomGeometryLayer rooms)
        {
            _topology = topology;
            _rooms = rooms;
        }

        public void InvalidateAll() => _cellToResult.Clear();

        public void RegisterFootprint(int band, FloorRoomBfsProfile profile, FloorBfsResult result)
        {
            if (result.Visited == null || result.Visited.Count == 0)
                return;

            var dict = GetOrCreateCellMap(band, profile);
            foreach (var (x, z) in result.Visited)
                dict[(x, z)] = result;
        }

        public void UnregisterFootprint(int band, FloorRoomBfsProfile profile, FloorBfsResult result)
        {
            if (result.Visited == null)
                return;

            var key = (band, profile);
            if (!_cellToResult.TryGetValue(key, out var dict))
                return;

            foreach (var (x, z) in result.Visited)
                dict.Remove((x, z));

            if (dict.Count == 0)
                _cellToResult.Remove(key);
        }

        public FloorBfsResult GetForCell(int band, int x, int z, FloorRoomBfsProfile profile)
        {
            if (TryResolveRoomKey(band, x, z, out var roomKey) &&
                _rooms.TryGet(roomKey, profile, out var baked))
                return baked;

            var bandKey = (band, profile);
            if (_cellToResult.TryGetValue(bandKey, out var dict) &&
                dict.TryGetValue((x, z), out var cached))
                return cached;

            bool collectEmpty = profile == FloorRoomBfsProfile.Visibility;
            FloorBfsResult result = FloorRoomFloodFill.Run(_topology.Index, band, x, z, collectEmpty);
            RegisterFootprint(band, profile, result);
            return result;
        }

        public bool TryResolveRoomKey(int band, int x, int z, out RoomKey key)
        {
            key = default;

            if (!_topology.TryGetCellTiles(x, z, band, out var list) || !FloorMapIndex.CellHasFloor(list))
                return false;

            for (int i = 0; i < list.Count; i++)
            {
                var tile = list[i];
                if ((TileView.TileType)tile.identity.tileType != TileView.TileType.Floor)
                    continue;

                int buildingId = tile.identity.buildingId;
                int roomId = tile.identity.roomId;
                if (buildingId > 0 && roomId > 0)
                {
                    key = new RoomKey(buildingId, band, roomId);
                    return true;
                }

                return false;
            }

            return false;
        }

        Dictionary<(int x, int z), FloorBfsResult> GetOrCreateCellMap(int band, FloorRoomBfsProfile profile)
        {
            var key = (band, profile);
            if (!_cellToResult.TryGetValue(key, out var dict))
            {
                dict = new Dictionary<(int x, int z), FloorBfsResult>();
                _cellToResult[key] = dict;
            }

            return dict;
        }
    }

    /// <summary>맵 캐시 단일 진입점 — 소비자는 읽기 API, Builder는 계층 쓰기.</summary>
    public sealed class TileMapCacheHub
    {
        public TopologyLayer Topology { get; }
        public BuildingLayer Buildings { get; }
        public RoomGeometryLayer Rooms { get; }
        public BandGeometryLayer Bands { get; }

        TileMapCacheHub(
            TopologyLayer topology,
            BuildingLayer buildings,
            RoomGeometryLayer rooms,
            BandGeometryLayer bands)
        {
            Topology = topology;
            Buildings = buildings;
            Rooms = rooms;
            Bands = bands;
        }

        public static TileMapCacheHub Create(TileMapModel model, BuildingGroupRegistry registry)
        {
            var index = new FloorMapIndex(model.tiles, model.EdgeBinder.EdgeIndex);
            var topology = new TopologyLayer(index);
            var buildings = new BuildingLayer(registry);
            var rooms = new RoomGeometryLayer();
            var bands = new BandGeometryLayer(topology, rooms);
            return new TileMapCacheHub(topology, buildings, rooms, bands);
        }

        public bool CellHasOccupancy(int x, int z, int band) => Topology.HasOccupancy(x, z, band);

        public bool TryGetCellTiles(int x, int z, int band, out List<TileData> list) =>
            Topology.TryGetCellTiles(x, z, band, out list);

        public bool TryGetEdgeBetween(Vector3Int cellA, Vector3Int cellB, out TileData edgeWall) =>
            Topology.TryGetEdgeBetween(cellA, cellB, out edgeWall);

        public IEnumerable<(int x, int z, int band)> EnumerateOccupiedCells() =>
            Topology.EnumerateOccupiedCells();

        public GeometryQuery GetRoomGeometryForCell(int band, int x, int z, FloorRoomBfsProfile profile)
        {
            RoomKey? roomKey = Bands.TryResolveRoomKey(band, x, z, out var key) ? key : (RoomKey?)null;
            var result = Bands.GetForCell(band, x, z, profile);
            return new GeometryQuery(result, roomKey);
        }

        public HashSet<(int x, int z)> GetVisitedForCell(
            int band, int x, int z, FloorRoomBfsProfile profile) =>
            Bands.GetForCell(band, x, z, profile).Visited;

        /// <summary>야외 분기 판정 단일 API. buildingId==0으로 야외 추론하지 않습니다.</summary>
        public bool IsOutdoorEvaluation(int band, int x, int z)
        {
            if (Buildings.IsPlazaFloor(band, x, z))
                return true;

            if (!Bands.TryResolveRoomKey(band, x, z, out var roomKey))
                return false;

            if (!Rooms.TryGet(roomKey, FloorRoomBfsProfile.Visibility, out var visibility))
                return false;

            return visibility.EmptyDiscovered != null && visibility.EmptyDiscovered.Count > 0
                && visibility.Visited != null && visibility.Visited.Contains((x, z));
        }

        public bool TryGetFloorBuildingRoom(int band, int x, int z, out int buildingId, out int roomId) =>
            Buildings.TryGetFloorBuildingRoom(band, x, z, Topology, out buildingId, out roomId);

        public void InvalidateAll()
        {
            Rooms.InvalidateAll();
            Bands.InvalidateAll();
        }

        public void InvalidateRooms(IEnumerable<RoomKey> keys) =>
            Rooms.InvalidateRooms(keys, Bands);

        public void NotifyTopologyChanged(
            IReadOnlyCollection<Vector3Int> changedCells,
            BuildingGroupBuilder builder,
            bool isRemoval = false,
            TileData removedTile = default)
        {
            if (changedCells != null && changedCells.Count > 0)
                Topology.SyncOccupancyFromChangedCells(changedCells);

            if (builder != null)
            {
                if (isRemoval)
                    builder.HandleRemoveTile(removedTile, ToMutableHashSet(changedCells));
                else
                    builder.HandleSetOrApply(changedCells);
                return;
            }

            InvalidateAll();
        }

        static HashSet<Vector3Int> ToMutableHashSet(IReadOnlyCollection<Vector3Int> cells)
        {
            if (cells is HashSet<Vector3Int> set)
                return set;

            var result = new HashSet<Vector3Int>();
            if (cells == null)
                return result;

            foreach (var c in cells)
                result.Add(c);
            return result;
        }
    }

}
