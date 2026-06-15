// ============================================================
// PlayerFloorVisibilityPolicy — 플레이어 층·실내/야외 분기 타일 가시성
// ============================================================
using System;
using System.Collections.Generic;
using UnityEngine;

namespace IsoTilemap
{
    public readonly struct FloorVisibilityContext : IEquatable<FloorVisibilityContext>
    {
        public bool IsPlayerOutdoor { get; }
        public int PlayerFloorCellY { get; }
        public int MinCellY { get; }
        public int PlayerBuildingId { get; }
        public HashSet<int> PlayerBlockingBuildingIds { get; }
        public HashSet<(int x, int z, int y)> VisibleBelowCells { get; }

        public FloorVisibilityContext(
            bool isPlayerOutdoor,
            int playerFloorCellY,
            int minCellY,
            int playerBuildingId,
            HashSet<int> playerBlockingBuildingIds,
            HashSet<(int x, int z, int y)> visibleBelowCells)
        {
            IsPlayerOutdoor = isPlayerOutdoor;
            PlayerFloorCellY = playerFloorCellY;
            MinCellY = minCellY;
            PlayerBuildingId = playerBuildingId;
            PlayerBlockingBuildingIds = playerBlockingBuildingIds ?? new HashSet<int>();
            VisibleBelowCells = visibleBelowCells ?? new HashSet<(int x, int z, int y)>();
        }

        public bool Equals(FloorVisibilityContext other) =>
            IsPlayerOutdoor == other.IsPlayerOutdoor &&
            PlayerFloorCellY == other.PlayerFloorCellY &&
            MinCellY == other.MinCellY &&
            PlayerBuildingId == other.PlayerBuildingId &&
            SetEquals(PlayerBlockingBuildingIds, other.PlayerBlockingBuildingIds) &&
            SetEquals(VisibleBelowCells, other.VisibleBelowCells);

        public override bool Equals(object obj) => obj is FloorVisibilityContext other && Equals(other);

        public override int GetHashCode()
        {
            int hash = HashCode.Combine(IsPlayerOutdoor, PlayerFloorCellY, MinCellY, PlayerBuildingId);
            hash = HashCombineSet(hash, PlayerBlockingBuildingIds);
            return HashCombineBelowCells(hash, VisibleBelowCells);
        }

        public static bool operator ==(FloorVisibilityContext left, FloorVisibilityContext right) =>
            left.Equals(right);

        public static bool operator !=(FloorVisibilityContext left, FloorVisibilityContext right) =>
            !left.Equals(right);

        static bool SetEquals(HashSet<int> a, HashSet<int> b)
        {
            if (ReferenceEquals(a, b))
                return true;
            if (a == null || b == null || a.Count != b.Count)
                return false;

            foreach (int v in a)
            {
                if (!b.Contains(v))
                    return false;
            }

            return true;
        }

        static bool SetEquals(
            HashSet<(int x, int z, int y)> a,
            HashSet<(int x, int z, int y)> b)
        {
            if (ReferenceEquals(a, b))
                return true;
            if (a == null || b == null || a.Count != b.Count)
                return false;

            foreach (var v in a)
            {
                if (!b.Contains(v))
                    return false;
            }

            return true;
        }

        static int HashCombineSet(int hash, HashSet<int> set)
        {
            if (set == null)
                return hash;

            foreach (int v in set)
                hash = HashCode.Combine(hash, v);

            return hash;
        }

        static int HashCombineBelowCells(int hash, HashSet<(int x, int z, int y)> set)
        {
            if (set == null)
                return hash;

            foreach (var (x, z, y) in set)
                hash = HashCode.Combine(hash, x, z, y);

            return hash;
        }
    }

    /// <summary>플레이어 월드 높이·그리드 XZ 기준 층(Y) 타일 가시성. XZ 청크 스트리밍과 분리.</summary>
    public sealed class PlayerFloorVisibilityPolicy
    {
        static readonly HashSet<int> EmptyBlocking = new();
        static readonly HashSet<(int x, int z, int y)> EmptyVisibleBelow = new();

        readonly float _cellSize;
        readonly float _cellEpsilonWorld;
        readonly int _minCellY;
        readonly TileMapCacheHub _hub;
        readonly IndoorTileVisibilityPipeline _indoor = new();
        readonly OutdoorTileVisibilityPipeline _outdoor = new();
        readonly BlockingBuildingFullHideLayer _blockingBuildingHide;
        readonly BuildingPlayerOcclusionResolver _occlusionResolver;

        readonly HashSet<int> _blockingScratch = new();
        readonly HashSet<(int x, int z, int y)> _visibleBelowScratch = new();

        HashSet<int> _blockingForContext = new();
        HashSet<(int x, int z, int y)> _visibleBelowForContext = new();

        Vector3Int _cachedPlayerOccupiedCell;
        bool _cachedHideEnabled;
        bool _hasStableIdentity;
        bool _cachedIsOutdoor;
        int _cachedPlayerBuildingId;
        HashSet<(int x, int z, int y)> _cachedVisibleBelow = new();

        /// <summary>시선상 가림 건물 전층 Hide(실내·야외 공통). false면 차단 집합을 비웁니다.</summary>
        public bool OutdoorSightLineBuildingHideEnabled { get; set; } = true;

        private PlayerFloorVisibilityPolicy(
            float cellSize,
            float cellEpsilonWorld,
            int minCellY,
            TileMapCacheHub hub,
            BuildingGroupRegistry buildingRegistry,
            BuildingPlayerOcclusionResolver occlusionResolver)
        {
            _cellSize = cellSize;
            _cellEpsilonWorld = cellEpsilonWorld;
            _minCellY = minCellY;
            _hub = hub;
            _occlusionResolver = occlusionResolver;
            _blockingBuildingHide = new BlockingBuildingFullHideLayer(buildingRegistry);
        }

        public int MinCellY => _minCellY;

        public TileMapCacheHub MapCache => _hub;

        public float CellSize => _cellSize;

        public SightLineBuildingDebugSnapshot LastSightLineDebug => _occlusionResolver.LastDebug;

        public static PlayerFloorVisibilityPolicy Build(
            TileMapCacheHub hub,
            float cellSize,
            Func<Camera> resolveCamera,
            BuildingGroupRegistry buildingRegistry,
            float cellEpsilonWorld = 0f,
            float groundPlaneY = 0f)
        {
            if (hub == null)
                throw new ArgumentNullException(nameof(hub));

            if (cellSize <= 0f)
                cellSize = 1f;

            _ = groundPlaneY;
            int minCellY = OccupiedCellCoord.ResolveMinStructuralFloorCellY(hub);
            var resolver = new BuildingPlayerOcclusionResolver(hub, cellSize, resolveCamera);

            return new PlayerFloorVisibilityPolicy(
                cellSize,
                cellEpsilonWorld,
                minCellY,
                hub,
                buildingRegistry,
                resolver);
        }

        public Vector3Int ResolvePlayerOccupiedCell(float playerHeightWorldY, Vector3 playerWorld) =>
            OccupiedCellCoord.ResolveFromWorld(
                _hub, playerWorld, _cellSize, playerHeightWorldY, _cellEpsilonWorld, _minCellY);

        public FloorVisibilityContext ResolveContext(
            float playerHeightWorldY,
            Vector3 playerWorld)
        {
            Vector3Int playerOccupiedCell = ResolvePlayerOccupiedCell(playerHeightWorldY, playerWorld);
            int playerFloorCellY = playerOccupiedCell.y;

            bool reuseIdentity = _hasStableIdentity &&
                playerOccupiedCell == _cachedPlayerOccupiedCell &&
                OutdoorSightLineBuildingHideEnabled == _cachedHideEnabled;

            bool isOutdoor;
            int playerBuildingId;
            HashSet<(int x, int z, int y)> visibleBelow;

            if (reuseIdentity)
            {
                isOutdoor = _cachedIsOutdoor;
                playerBuildingId = _cachedPlayerBuildingId;
                visibleBelow = _cachedVisibleBelow.Count == 0 ? EmptyVisibleBelow : _cachedVisibleBelow;
            }
            else
            {
                isOutdoor = _hub.IsOutdoorEvaluation(
                    playerFloorCellY, playerOccupiedCell.x, playerOccupiedCell.z);
                _hub.TryGetFloorBuildingRoom(
                    playerFloorCellY, playerOccupiedCell.x, playerOccupiedCell.z,
                    out playerBuildingId, out _);

                visibleBelow = isOutdoor
                    ? EmptyVisibleBelow
                    : ResolveVisibleBelowForContext(
                        playerFloorCellY, playerOccupiedCell.x, playerOccupiedCell.z);

                _cachedIsOutdoor = isOutdoor;
                _cachedPlayerBuildingId = playerBuildingId;
                CopyVisibleBelow(visibleBelow, _cachedVisibleBelow);
                _cachedPlayerOccupiedCell = playerOccupiedCell;
                _cachedHideEnabled = OutdoorSightLineBuildingHideEnabled;
                _hasStableIdentity = true;
            }

            HashSet<int> blocking = OutdoorSightLineBuildingHideEnabled
                ? ResolveBlockingForContext(playerWorld, playerOccupiedCell, playerBuildingId)
                : EmptyBlocking;

            return new FloorVisibilityContext(
                isOutdoor, playerFloorCellY, _minCellY, playerBuildingId, blocking, visibleBelow);
        }

        static void CopyVisibleBelow(
            HashSet<(int x, int z, int y)> source,
            HashSet<(int x, int z, int y)> dest)
        {
            dest.Clear();
            if (source == null || source.Count == 0)
                return;

            foreach (var cell in source)
                dest.Add(cell);
        }

        HashSet<int> ResolveBlockingForContext(
            Vector3 playerWorld,
            Vector3Int playerOccupiedCell,
            int playerBuildingId)
        {
            int excludeBuildingId = playerBuildingId > 0 ? playerBuildingId : 0;
            _occlusionResolver.ResolveBlockingBuildingIds(
                playerWorld, playerOccupiedCell, _blockingScratch, excludeBuildingId);

            if (_blockingScratch.Count == 0)
            {
                _blockingForContext = EmptyBlocking;
                return _blockingForContext;
            }

            _blockingForContext = new HashSet<int>(_blockingScratch);
            return _blockingForContext;
        }

        HashSet<(int x, int z, int y)> ResolveVisibleBelowForContext(int playerFloorCellY, int gridX, int gridZ)
        {
            BuildVisibleBelowCells(playerFloorCellY, gridX, gridZ);
            if (SetEqualsBelowCells(_visibleBelowScratch, _visibleBelowForContext))
                return _visibleBelowForContext;

            if (_visibleBelowScratch.Count == 0)
            {
                _visibleBelowForContext = EmptyVisibleBelow;
                return _visibleBelowForContext;
            }

            _visibleBelowForContext = new HashSet<(int x, int z, int y)>(_visibleBelowScratch);
            return _visibleBelowForContext;
        }

        static bool SetEqualsBlocking(HashSet<int> a, HashSet<int> b)
        {
            if (ReferenceEquals(a, b))
                return true;
            if (a == null || b == null || a.Count != b.Count)
                return false;
            foreach (int id in a)
            {
                if (!b.Contains(id))
                    return false;
            }
            return true;
        }

        static bool SetEqualsBelowCells(
            HashSet<(int x, int z, int y)> a,
            HashSet<(int x, int z, int y)> b)
        {
            if (ReferenceEquals(a, b))
                return true;
            if (a == null || b == null || a.Count != b.Count)
                return false;
            foreach (var cell in a)
            {
                if (!b.Contains(cell))
                    return false;
            }
            return true;
        }

        public bool IsTileVisible(TileData tile, in FloorVisibilityContext ctx)
        {
            if (_blockingBuildingHide.Evaluate(tile, ctx) == TileVisibilityVerdict.Hide)
                return false;

            if (ctx.IsPlayerOutdoor)
                return _outdoor.IsVisible(tile, ctx);

            if (ctx.PlayerBuildingId <= 0 || tile.identity.buildingId != ctx.PlayerBuildingId)
                return true;

            return _indoor.IsVisible(tile, ctx);
        }

        /// <summary>플레이어 월드 높이 → 점유 층 cellY (층 가시성·room 조회 공용).</summary>
        public int ResolvePlayerFloorCellY(float playerHeightWorldY, Vector3 playerWorld) =>
            ResolvePlayerOccupiedCell(playerHeightWorldY, playerWorld).y;

        /// <summary>XZ만 있을 때 Y는 <see cref="ResolvePlayerOccupiedCell"/>과 동일 규칙으로 world.y 기준.</summary>
        public int ResolvePlayerFloorCellY(float playerHeightWorldY) =>
            ResolvePlayerFloorCellY(playerHeightWorldY, new Vector3(0f, playerHeightWorldY, 0f));

        void BuildVisibleBelowCells(int playerFloorCellY, int gridX, int gridZ)
        {
            _visibleBelowScratch.Clear();
            if (playerFloorCellY <= _minCellY)
                return;

            FloorBfsResult top = _hub.GetRoomGeometryForCell(
                playerFloorCellY, gridX, gridZ, FloorRoomBfsProfile.Visibility).Result;
            foreach (var (holeX, holeZ) in top.EmptyDiscovered)
                AddVisibleThroughHole(holeX, holeZ, playerFloorCellY);
        }

        private void AddVisibleThroughHole(int holeX, int holeZ, int playerFloorCellY)
        {
            for (int k = playerFloorCellY - 1; k >= _minCellY; k--)
            {
                if (!_hub.CellHasOccupancy(holeX, holeZ, k))
                    continue;

                HashSet<(int x, int z)> room = _hub.GetVisitedForCell(
                    k, holeX, holeZ, FloorRoomBfsProfile.Visibility);
                foreach (var (vx, vz) in room)
                    _visibleBelowScratch.Add((vx, vz, k));
                return;
            }
        }
    }
}
