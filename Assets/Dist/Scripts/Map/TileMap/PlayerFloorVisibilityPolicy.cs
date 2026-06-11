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
        readonly int[] _distinctOccupiedCellYs;
        readonly TileMapCacheHub _hub;
        readonly IndoorTileVisibilityPipeline _indoor = new();
        readonly OutdoorTileVisibilityPipeline _outdoor = new();
        readonly BlockingBuildingFullHideLayer _blockingBuildingHide = new();
        readonly BuildingPlayerOcclusionResolver _occlusionResolver;

        readonly HashSet<int> _blockingScratch = new();
        readonly HashSet<(int x, int z, int y)> _visibleBelowScratch = new();

        HashSet<int> _blockingForContext = new();
        HashSet<(int x, int z, int y)> _visibleBelowForContext = new();

        Vector3Int _cachedPlayerGridCell;
        int _cachedPlayerFloorCellY;
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
            int[] distinctOccupiedCellYs,
            TileMapCacheHub hub,
            BuildingPlayerOcclusionResolver occlusionResolver)
        {
            _cellSize = cellSize;
            _cellEpsilonWorld = cellEpsilonWorld;
            _minCellY = minCellY;
            _distinctOccupiedCellYs = distinctOccupiedCellYs;
            _hub = hub;
            _occlusionResolver = occlusionResolver;
        }

        public int MinCellY => _minCellY;

        public TileMapCacheHub MapCache => _hub;

        public float CellSize => _cellSize;

        public SightLineBuildingDebugSnapshot LastSightLineDebug => _occlusionResolver.LastDebug;

        public static PlayerFloorVisibilityPolicy Build(
            IReadOnlyList<TileData> tiles,
            TileMapCacheHub hub,
            float cellSize,
            Func<Camera> resolveCamera,
            float cellEpsilonWorld = 0f,
            float groundPlaneY = 0f)
        {
            if (cellSize <= 0f)
                cellSize = 1f;

            var bandSet = new HashSet<int>();
            if (tiles != null)
            {
                for (int i = 0; i < tiles.Count; i++)
                    bandSet.Add(TileVisibilityCellUtil.GetCellY(tiles[i]));
            }

            if (bandSet.Count == 0)
                bandSet.Add(0);

            var distinctBands = new int[bandSet.Count];
            bandSet.CopyTo(distinctBands);
            Array.Sort(distinctBands);

            _ = groundPlaneY;
            var resolver = new BuildingPlayerOcclusionResolver(hub, cellSize, resolveCamera);

            return new PlayerFloorVisibilityPolicy(
                cellSize,
                cellEpsilonWorld,
                distinctBands[0],
                distinctBands,
                hub,
                resolver);
        }

        public FloorVisibilityContext ResolveContext(
            float playerHeightWorldY,
            Vector3 playerWorld)
        {
            Vector3Int sightPlayerCell = TileHelper.ConvertWorldToGrid(playerWorld, _cellSize);
            int playerFloorCellY = ResolvePlayerFloorCellY(playerHeightWorldY);

            bool reuseIdentity = _hasStableIdentity &&
                sightPlayerCell == _cachedPlayerGridCell &&
                playerFloorCellY == _cachedPlayerFloorCellY &&
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
                isOutdoor = _hub.IsOutdoorEvaluation(playerFloorCellY, sightPlayerCell.x, sightPlayerCell.z);
                _hub.TryGetFloorBuildingRoom(
                    playerFloorCellY, sightPlayerCell.x, sightPlayerCell.z, out playerBuildingId, out _);

                visibleBelow = isOutdoor
                    ? EmptyVisibleBelow
                    : ResolveVisibleBelowForContext(playerFloorCellY, sightPlayerCell.x, sightPlayerCell.z);

                _cachedIsOutdoor = isOutdoor;
                _cachedPlayerBuildingId = playerBuildingId;
                CopyVisibleBelow(visibleBelow, _cachedVisibleBelow);
                _cachedPlayerGridCell = sightPlayerCell;
                _cachedPlayerFloorCellY = playerFloorCellY;
                _cachedHideEnabled = OutdoorSightLineBuildingHideEnabled;
                _hasStableIdentity = true;
            }

            HashSet<int> blocking = OutdoorSightLineBuildingHideEnabled
                ? ResolveBlockingForContext(playerWorld, sightPlayerCell, playerBuildingId)
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
            Vector3Int sightPlayerCell,
            int playerBuildingId)
        {
            int excludeBuildingId = playerBuildingId > 0 ? playerBuildingId : 0;
            _occlusionResolver.ResolveBlockingBuildingIds(
                playerWorld, _blockingScratch, excludeBuildingId);

            if (SetEqualsBlocking(_blockingScratch, _blockingForContext))
                return _blockingForContext;

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

        public void FilterTiles(List<TileData> buffer, in FloorVisibilityContext ctx)
        {
            if (buffer == null || buffer.Count == 0)
                return;

            for (int i = buffer.Count - 1; i >= 0; i--)
            {
                if (!IsTileVisible(buffer[i], ctx))
                    buffer.RemoveAt(i);
            }
        }

        /// <summary>플레이어 월드 높이 → 점유 층 cellY (층 가시성·room 조회 공용).</summary>
        public int ResolvePlayerFloorCellY(float playerHeightWorldY)
        {
            int playerFloorCellY = _minCellY;
            float ceiling = playerHeightWorldY + _cellEpsilonWorld;

            for (int i = 0; i < _distinctOccupiedCellYs.Length; i++)
            {
                int occupiedCellY = _distinctOccupiedCellYs[i];
                if (occupiedCellY * _cellSize <= ceiling)
                    playerFloorCellY = occupiedCellY;
            }

            return playerFloorCellY;
        }

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
