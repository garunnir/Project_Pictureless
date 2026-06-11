// ============================================================
// TileMapCacheHub — 맵 topology·건물·cellY·room geometry 통합 조회·무효화
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

    /// <summary>셀·엣지 topology 조회 및 점유 (x,z,y) 동기화.</summary>
    public sealed class TopologyLayer
    {
        readonly FloorMapIndex _index;

        internal TopologyLayer(FloorMapIndex index) => _index = index;

        public FloorMapIndex Index => _index;

        public bool HasOccupancy(int x, int z, int y) => _index.HasAnyTile(x, z, y);

        public IEnumerable<(int x, int z, int y)> EnumerateOccupiedCells() =>
            _index.EnumerateOccupiedCells();

        public bool TryGetCellTiles(int x, int z, int cellY, out List<TileData> list) =>
            _index.TryGetCellTiles(x, z, cellY, out list);

        public bool TryGetEdgeBetween(Vector3Int cellA, Vector3Int cellB, out TileData edgeWall) =>
            _index.TryGetEdgeBetween(cellA, cellB, out edgeWall);

        public Vector3Int ResolveFloorBfsStart(int cellY, int startX, int startZ) =>
            _index.ResolveFloorBfsStart(cellY, startX, startZ);

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

        public bool IsPlazaFloor(int cellY, int x, int z) => _registry.IsPlazaFloor(cellY, x, z);

        public bool TryGetEdgeWalls(RoomKey roomKey, out IReadOnlyCollection<Guid> edgeIds) =>
            _registry.TryGetEdgeWallIds(roomKey, out edgeIds);

        public bool TryGetFloorBuildingRoom(int cellY, int x, int z, TopologyLayer topology, out int buildingId, out int roomId)
        {
            buildingId = 0;
            roomId = 0;

            if (!topology.TryGetCellTiles(x, z, cellY, out var list) || !FloorMapIndex.CellHasFloor(list))
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

        public int TryGetBuildingIdAtCell(int cellY, int x, int z, TopologyLayer topology) =>
            TryGetFloorBuildingRoom(cellY, x, z, topology, out int buildingId, out _) ? buildingId : 0;
    }

    /// <summary>RoomKey + profile 단위 bake된 BFS geometry.</summary>
    public sealed class RoomGeometryLayer
    {
        readonly Dictionary<(RoomKey key, FloorRoomBfsProfile profile), FloorBfsResult> _byRoomProfile = new();

        public void InvalidateAll() => _byRoomProfile.Clear();

        public void InvalidateRooms(IEnumerable<RoomKey> keys, CellYGeometryLayer cellYGeometry)
        {
            if (keys == null)
                return;

            foreach (var key in keys)
            {
                if (TryGet(key, FloorRoomBfsProfile.Occlusion, out var occ))
                    cellYGeometry.UnregisterFootprint(key.CellY, FloorRoomBfsProfile.Occlusion, occ);
                if (TryGet(key, FloorRoomBfsProfile.Visibility, out var vis))
                    cellYGeometry.UnregisterFootprint(key.CellY, FloorRoomBfsProfile.Visibility, vis);

                _byRoomProfile.Remove((key, FloorRoomBfsProfile.Occlusion));
                _byRoomProfile.Remove((key, FloorRoomBfsProfile.Visibility));
            }
        }

        public void Store(RoomKey key, FloorRoomBfsProfile profile, FloorBfsResult result) =>
            _byRoomProfile[(key, profile)] = result;

        public bool TryGet(RoomKey key, FloorRoomBfsProfile profile, out FloorBfsResult result) =>
            _byRoomProfile.TryGetValue((key, profile), out result);
    }

    /// <summary>(cellY, profile)별 (x,z) → BFS 결과 역인덱스. lazy 선형 탐색 대체.</summary>
    public sealed class CellYGeometryLayer
    {
        readonly TopologyLayer _topology;
        readonly RoomGeometryLayer _rooms;

        readonly Dictionary<(int cellY, FloorRoomBfsProfile profile), Dictionary<(int x, int z), FloorBfsResult>>
            _cellToResult = new();

        internal CellYGeometryLayer(TopologyLayer topology, RoomGeometryLayer rooms)
        {
            _topology = topology;
            _rooms = rooms;
        }

        public void InvalidateAll() => _cellToResult.Clear();

        public void RegisterFootprint(int cellY, FloorRoomBfsProfile profile, FloorBfsResult result)
        {
            if (result.Visited == null || result.Visited.Count == 0)
                return;

            var dict = GetOrCreateCellMap(cellY, profile);
            foreach (var (x, z) in result.Visited)
                dict[(x, z)] = result;
        }

        public void UnregisterFootprint(int cellY, FloorRoomBfsProfile profile, FloorBfsResult result)
        {
            if (result.Visited == null)
                return;

            var key = (cellY, profile);
            if (!_cellToResult.TryGetValue(key, out var dict))
                return;

            foreach (var (x, z) in result.Visited)
                dict.Remove((x, z));

            if (dict.Count == 0)
                _cellToResult.Remove(key);
        }

        public FloorBfsResult GetForCell(int cellY, int x, int z, FloorRoomBfsProfile profile)
        {
            if (TryResolveRoomKey(cellY, x, z, out var roomKey) &&
                _rooms.TryGet(roomKey, profile, out var baked))
                return baked;

            var cellYKey = (cellY, profile);
            if (_cellToResult.TryGetValue(cellYKey, out var dict) &&
                dict.TryGetValue((x, z), out var cached))
                return cached;

            bool collectEmpty = profile == FloorRoomBfsProfile.Visibility;
            FloorBfsResult result = FloorRoomFloodFill.Run(_topology.Index, cellY, x, z, collectEmpty);
            RegisterFootprint(cellY, profile, result);
            return result;
        }

        public bool TryResolveRoomKey(int cellY, int x, int z, out RoomKey key)
        {
            key = default;

            if (!_topology.TryGetCellTiles(x, z, cellY, out var list) || !FloorMapIndex.CellHasFloor(list))
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
                    key = new RoomKey(buildingId, cellY, roomId);
                    return true;
                }

                return false;
            }

            return false;
        }

        Dictionary<(int x, int z), FloorBfsResult> GetOrCreateCellMap(int cellY, FloorRoomBfsProfile profile)
        {
            var key = (cellY, profile);
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
        public CellYGeometryLayer CellYGeometry { get; }

        BuildingGroupBuilder _roomBakeBuilder;

        TileMapCacheHub(
            TopologyLayer topology,
            BuildingLayer buildings,
            RoomGeometryLayer rooms,
            CellYGeometryLayer cellYGeometry)
        {
            Topology = topology;
            Buildings = buildings;
            Rooms = rooms;
            CellYGeometry = cellYGeometry;
        }

        public static TileMapCacheHub Create(TileMapModel model, BuildingGroupRegistry registry)
        {
            var index = new FloorMapIndex(model.tiles, model.EdgeBinder.EdgeIndex);
            var topology = new TopologyLayer(index);
            var buildings = new BuildingLayer(registry);
            var rooms = new RoomGeometryLayer();
            var cellYGeometry = new CellYGeometryLayer(topology, rooms);
            return new TileMapCacheHub(topology, buildings, rooms, cellYGeometry);
        }

        internal void BindRoomBakeBuilder(BuildingGroupBuilder builder) => _roomBakeBuilder = builder;

        bool TryEnsureRoomKeyAtCell(int cellY, int x, int z, out RoomKey roomKey)
        {
            if (CellYGeometry.TryResolveRoomKey(cellY, x, z, out roomKey))
                return true;

            if (_roomBakeBuilder == null ||
                !_roomBakeBuilder.EnsureRoomAtFloorCell(cellY, x, z))
                return false;

            return CellYGeometry.TryResolveRoomKey(cellY, x, z, out roomKey);
        }

        public bool CellHasOccupancy(int x, int z, int y) => Topology.HasOccupancy(x, z, y);

        public bool TryGetCellTiles(int x, int z, int cellY, out List<TileData> list) =>
            Topology.TryGetCellTiles(x, z, cellY, out list);

        public bool TryGetEdgeBetween(Vector3Int cellA, Vector3Int cellB, out TileData edgeWall) =>
            Topology.TryGetEdgeBetween(cellA, cellB, out edgeWall);

        public IEnumerable<(int x, int z, int y)> EnumerateOccupiedCells() =>
            Topology.EnumerateOccupiedCells();

        public GeometryQuery GetRoomGeometryForCell(int cellY, int x, int z, FloorRoomBfsProfile profile)
        {
            RoomKey? roomKey = CellYGeometry.TryResolveRoomKey(cellY, x, z, out var key) ? key : (RoomKey?)null;
            var result = CellYGeometry.GetForCell(cellY, x, z, profile);
            return new GeometryQuery(result, roomKey);
        }

        public HashSet<(int x, int z)> GetVisitedForCell(
            int cellY, int x, int z, FloorRoomBfsProfile profile) =>
            CellYGeometry.GetForCell(cellY, x, z, profile).Visited;

        /// <summary>
        /// 월드 XZ 스냅 셀의 roomId로 bake된 <see cref="FloorBfsResult.Visited"/>를 읽습니다.
        /// roomId가 없으면 BFS 탐색·부여 후 반환. 바닥·buildingId 없으면 null.
        /// </summary>
        public HashSet<(int x, int z)> GetVisitedForWorld(
            int floorCellY,
            Vector3 worldRef,
            float cellSize,
            FloorRoomBfsProfile profile)
        {
            Vector3Int snap = TileHelper.ConvertWorldToGrid(worldRef, Mathf.Max(1e-4f, cellSize));
            if (!TryEnsureRoomKeyAtCell(floorCellY, snap.x, snap.z, out var roomKey))
                return null;

            if (!Rooms.TryGet(roomKey, profile, out var baked))
                return null;

            return baked.Visited;
        }

        /// <summary>야외 분기 판정 단일 API. buildingId==0으로 야외 추론하지 않습니다.</summary>
        public bool IsOutdoorEvaluation(int cellY, int x, int z)
        {
            if (Buildings.IsPlazaFloor(cellY, x, z))
                return true;

            if (!TryEnsureRoomKeyAtCell(cellY, x, z, out var roomKey))
                return false;

            if (!Rooms.TryGet(roomKey, FloorRoomBfsProfile.Visibility, out var visibility))
                return false;

            return visibility.EmptyDiscovered != null && visibility.EmptyDiscovered.Count > 0
                && visibility.Visited != null && visibility.Visited.Contains((x, z));
        }

        public bool TryGetFloorBuildingRoom(int cellY, int x, int z, out int buildingId, out int roomId) =>
            Buildings.TryGetFloorBuildingRoom(cellY, x, z, Topology, out buildingId, out roomId);

        public void InvalidateAll()
        {
            Rooms.InvalidateAll();
            CellYGeometry.InvalidateAll();
        }

        public void InvalidateRooms(IEnumerable<RoomKey> keys) =>
            Rooms.InvalidateRooms(keys, CellYGeometry);

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
