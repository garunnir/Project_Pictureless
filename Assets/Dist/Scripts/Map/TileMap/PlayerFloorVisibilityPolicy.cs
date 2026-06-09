// ============================================================
// PlayerFloorVisibilityPolicy — 플레이어 층·실내/야외 분기 타일 가시성
// ============================================================
using System;
using System.Collections.Generic;
using UnityEngine;

namespace IsoTilemap
{
    public enum OcclusionMode
    {
        LegacyCompatible = 0,
        FullDespawn = 1,
        RenderOnly = 2,
        ColliderOnly = 3,
        AlphaBlendPreserve = 4
    }

    public readonly struct FloorVisibilityContext : IEquatable<FloorVisibilityContext>
    {
        public bool IsPlayerOutdoor { get; }
        public int PlayerFloorCellY { get; }
        public int MinCellY { get; }
        public int PlayerBuildingId { get; }
        public HashSet<int> PlayerBlockingBuildingIds { get; }
        public OcclusionMode OcclusionMode { get; }
        public HashSet<(int x, int z, int y)> VisibleBelowCells { get; }

        public FloorVisibilityContext(
            bool isPlayerOutdoor,
            int playerFloorCellY,
            int minCellY,
            int playerBuildingId,
            HashSet<int> playerBlockingBuildingIds,
            OcclusionMode occlusionMode,
            HashSet<(int x, int z, int y)> visibleBelowCells)
        {
            IsPlayerOutdoor = isPlayerOutdoor;
            PlayerFloorCellY = playerFloorCellY;
            MinCellY = minCellY;
            PlayerBuildingId = playerBuildingId;
            PlayerBlockingBuildingIds = playerBlockingBuildingIds ?? new HashSet<int>();
            OcclusionMode = occlusionMode;
            VisibleBelowCells = visibleBelowCells ?? new HashSet<(int x, int z, int y)>();
        }

        public bool Equals(FloorVisibilityContext other) =>
            IsPlayerOutdoor == other.IsPlayerOutdoor &&
            PlayerFloorCellY == other.PlayerFloorCellY &&
            MinCellY == other.MinCellY &&
            PlayerBuildingId == other.PlayerBuildingId &&
            OcclusionMode == other.OcclusionMode &&
            SetEquals(PlayerBlockingBuildingIds, other.PlayerBlockingBuildingIds) &&
            SetEquals(VisibleBelowCells, other.VisibleBelowCells);

        public override bool Equals(object obj) => obj is FloorVisibilityContext other && Equals(other);

        public override int GetHashCode()
        {
            int hash = HashCode.Combine(IsPlayerOutdoor, PlayerFloorCellY, MinCellY, PlayerBuildingId, OcclusionMode);
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

        readonly float _cellSize;
        readonly float _cellEpsilonWorld;
        readonly int _minCellY;
        readonly int[] _distinctOccupiedCellYs;
        readonly TileMapCacheHub _hub;
        readonly IndoorTileVisibilityPipeline _indoor = new();
        readonly OutdoorTileVisibilityPipeline _outdoor = new();
        readonly BuildingPlayerOcclusionResolver _occlusionResolver;

        readonly HashSet<int> _blockingResult = new();
        readonly HashSet<int> _blockingPending = new();
        readonly HashSet<int> _blockingStable = new();
        int _blockingStableFrames;
        const int BlockingStableFramesRequired = 3;

        readonly HashSet<(int x, int z, int y)> _visibleBelowScratch = new();

        /// <summary>야외 시선상 가림 건물 전층 Hide. false면 차단 집합을 비웁니다.</summary>
        public bool OutdoorSightLineBuildingHideEnabled { get; set; } = true;
        public OcclusionMode OutdoorOcclusionMode { get; set; } = OcclusionMode.LegacyCompatible;

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
                    bandSet.Add(tiles[i].identity.GridPos.y);
            }

            if (bandSet.Count == 0)
                bandSet.Add(0);

            var distinctBands = new int[bandSet.Count];
            bandSet.CopyTo(distinctBands);
            Array.Sort(distinctBands);

            var resolver = new BuildingPlayerOcclusionResolver(hub, cellSize, resolveCamera, groundPlaneY);

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
            Vector3Int playerCell,
            Vector3 playerWorld)
        {
            int playerFloorCellY = ResolvePlayerFloorCellY(playerHeightWorldY);
            bool isOutdoor = _hub.IsOutdoorEvaluation(playerFloorCellY, playerCell.x, playerCell.z);
            _hub.TryGetFloorBuildingRoom(playerFloorCellY, playerCell.x, playerCell.z, out int playerBuildingId, out _);

            HashSet<(int x, int z, int y)> visibleBelow;
            HashSet<int> blocking;

            if (isOutdoor)
            {
                visibleBelow = new HashSet<(int x, int z, int y)>();
                int rawBlockingCount = 0;
                if (OutdoorSightLineBuildingHideEnabled)
                {
                    _blockingResult.Clear();
                    foreach (int id in _occlusionResolver.ResolveBlockingBuildingIds(
                                 playerWorld, playerCell))
                        _blockingResult.Add(id);
                    rawBlockingCount = _blockingResult.Count;
                }

                blocking = StabilizeOutdoorBlocking(_blockingResult, rawBlockingCount);
            }
            else
            {
                visibleBelow = BuildVisibleBelowCells(playerFloorCellY, playerCell.x, playerCell.z);
                blocking = EmptyBlocking;
            }

            return new FloorVisibilityContext(
                isOutdoor, playerFloorCellY, _minCellY, playerBuildingId, blocking, OutdoorOcclusionMode, visibleBelow);
        }

        public bool IsTileVisible(TileData tile, in FloorVisibilityContext ctx)
        {
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

        HashSet<int> StabilizeOutdoorBlocking(HashSet<int> raw, int rawCount)
        {
            if (!OutdoorSightLineBuildingHideEnabled || rawCount == 0)
            {
                _blockingPending.Clear();
                _blockingStable.Clear();
                _blockingStableFrames = 0;
                return new HashSet<int>();
            }

            if (!SetEqualsBlocking(raw, _blockingPending))
            {
                _blockingPending.Clear();
                foreach (int id in raw)
                    _blockingPending.Add(id);
                _blockingStableFrames = 1;
            }
            else
            {
                _blockingStableFrames++;
            }

            if (_blockingStableFrames >= BlockingStableFramesRequired)
            {
                _blockingStable.Clear();
                foreach (int id in _blockingPending)
                    _blockingStable.Add(id);
            }

            return new HashSet<int>(_blockingStable);
        }

        static bool SetEqualsBlocking(HashSet<int> a, HashSet<int> b)
        {
            if (a.Count != b.Count)
                return false;
            foreach (int id in a)
            {
                if (!b.Contains(id))
                    return false;
            }
            return true;
        }

        private int ResolvePlayerFloorCellY(float playerHeightWorldY)
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

        private HashSet<(int x, int z, int y)> BuildVisibleBelowCells(int playerFloorCellY, int gridX, int gridZ)
        {
            _visibleBelowScratch.Clear();
            if (playerFloorCellY <= _minCellY)
                return new HashSet<(int x, int z, int y)>(_visibleBelowScratch);

            FloorBfsResult top = _hub.GetRoomGeometryForCell(
                playerFloorCellY, gridX, gridZ, FloorRoomBfsProfile.Visibility).Result;
            foreach (var (holeX, holeZ) in top.EmptyDiscovered)
                AddVisibleThroughHole(holeX, holeZ, playerFloorCellY);

            return new HashSet<(int x, int z, int y)>(_visibleBelowScratch);
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
