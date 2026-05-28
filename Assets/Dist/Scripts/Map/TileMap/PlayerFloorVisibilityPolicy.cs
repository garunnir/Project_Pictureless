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
        public int FloorBand { get; }
        public int MinBand { get; }
        public int PlayerBuildingId { get; }
        public HashSet<int> PlayerBlockingBuildingIds { get; }
        public HashSet<(int x, int z, int band)> VisibleBelowCells { get; }

        public FloorVisibilityContext(
            bool isPlayerOutdoor,
            int floorBand,
            int minBand,
            int playerBuildingId,
            HashSet<int> playerBlockingBuildingIds,
            HashSet<(int x, int z, int band)> visibleBelowCells)
        {
            IsPlayerOutdoor = isPlayerOutdoor;
            FloorBand = floorBand;
            MinBand = minBand;
            PlayerBuildingId = playerBuildingId;
            PlayerBlockingBuildingIds = playerBlockingBuildingIds ?? new HashSet<int>();
            VisibleBelowCells = visibleBelowCells ?? new HashSet<(int x, int z, int band)>();
        }

        public bool Equals(FloorVisibilityContext other) =>
            IsPlayerOutdoor == other.IsPlayerOutdoor &&
            FloorBand == other.FloorBand &&
            MinBand == other.MinBand &&
            PlayerBuildingId == other.PlayerBuildingId &&
            SetEquals(PlayerBlockingBuildingIds, other.PlayerBlockingBuildingIds) &&
            SetEquals(VisibleBelowCells, other.VisibleBelowCells);

        public override bool Equals(object obj) => obj is FloorVisibilityContext other && Equals(other);

        public override int GetHashCode()
        {
            int hash = HashCode.Combine(IsPlayerOutdoor, FloorBand, MinBand, PlayerBuildingId);
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
            HashSet<(int x, int z, int band)> a,
            HashSet<(int x, int z, int band)> b)
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

        static int HashCombineBelowCells(int hash, HashSet<(int x, int z, int band)> set)
        {
            if (set == null)
                return hash;

            foreach (var (x, z, band) in set)
                hash = HashCode.Combine(hash, x, z, band);

            return hash;
        }
    }

    /// <summary>플레이어 월드 높이·그리드 XZ 기준 층(Y) 타일 가시성. XZ 청크 스트리밍과 분리.</summary>
    public sealed class PlayerFloorVisibilityPolicy
    {
        static readonly HashSet<int> EmptyBlocking = new();

        readonly float _cellSize;
        readonly float _bandEpsilonWorld;
        readonly int _minBand;
        readonly int[] _distinctFloorBands;
        readonly TileMapCacheHub _hub;
        readonly IndoorTileVisibilityPipeline _indoor = new();
        readonly OutdoorTileVisibilityPipeline _outdoor = new();
        readonly BuildingPlayerOcclusionResolver _occlusionResolver;

        readonly HashSet<int> _blockingResult = new();
        readonly HashSet<int> _blockingPending = new();
        readonly HashSet<int> _blockingStable = new();
        int _blockingStableFrames;
        const int BlockingStableFramesRequired = 3;

        readonly HashSet<(int x, int z, int band)> _visibleBelowScratch = new();

        /// <summary>야외 시선상 가림 건물 전층 Hide. false면 차단 집합을 비웁니다.</summary>
        public bool OutdoorSightLineBuildingHideEnabled { get; set; } = true;

        private PlayerFloorVisibilityPolicy(
            float cellSize,
            float bandEpsilonWorld,
            int minBand,
            int[] distinctFloorBands,
            TileMapCacheHub hub,
            BuildingPlayerOcclusionResolver occlusionResolver)
        {
            _cellSize = cellSize;
            _bandEpsilonWorld = bandEpsilonWorld;
            _minBand = minBand;
            _distinctFloorBands = distinctFloorBands;
            _hub = hub;
            _occlusionResolver = occlusionResolver;
        }

        public int MinBand => _minBand;

        public TileMapCacheHub MapCache => _hub;

        public static PlayerFloorVisibilityPolicy Build(
            IReadOnlyList<TileData> tiles,
            TileMapCacheHub hub,
            float cellSize,
            Func<Camera> resolveCamera,
            float bandEpsilonWorld = 0f,
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
                bandEpsilonWorld,
                distinctBands[0],
                distinctBands,
                hub,
                resolver);
        }

        public FloorVisibilityContext ResolveContext(
            float playerHeightWorldY,
            int gridX,
            int gridZ,
            Vector3 playerWorld)
        {
            int floorBand = ResolveFloorBand(playerHeightWorldY);
            bool isOutdoor = _hub.IsOutdoorEvaluation(floorBand, gridX, gridZ);
            _hub.TryGetFloorBuildingRoom(floorBand, gridX, gridZ, out int playerBuildingId, out _);

            HashSet<(int x, int z, int band)> visibleBelow;
            HashSet<int> blocking;

            if (isOutdoor)
            {
                visibleBelow = new HashSet<(int x, int z, int band)>();
                int rawBlockingCount = 0;
                if (OutdoorSightLineBuildingHideEnabled)
                {
                    _blockingResult.Clear();
                    foreach (int id in _occlusionResolver.ResolveBlockingBuildingIds(
                                 playerWorld, gridX, gridZ))
                        _blockingResult.Add(id);
                    rawBlockingCount = _blockingResult.Count;
                }

                blocking = StabilizeOutdoorBlocking(_blockingResult, rawBlockingCount);
            }
            else
            {
                visibleBelow = BuildVisibleBelowCells(floorBand, gridX, gridZ);
                blocking = EmptyBlocking;
            }

            return new FloorVisibilityContext(
                isOutdoor, floorBand, _minBand, playerBuildingId, blocking, visibleBelow);
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

        private int ResolveFloorBand(float playerHeightWorldY)
        {
            int floorBand = _minBand;
            float ceiling = playerHeightWorldY + _bandEpsilonWorld;

            for (int i = 0; i < _distinctFloorBands.Length; i++)
            {
                int band = _distinctFloorBands[i];
                if (band * _cellSize <= ceiling)
                    floorBand = band;
            }

            return floorBand;
        }

        private HashSet<(int x, int z, int band)> BuildVisibleBelowCells(int floorBand, int gridX, int gridZ)
        {
            _visibleBelowScratch.Clear();
            if (floorBand <= _minBand)
                return new HashSet<(int x, int z, int band)>(_visibleBelowScratch);

            FloorBfsResult top = _hub.GetRoomGeometryForCell(
                floorBand, gridX, gridZ, FloorRoomBfsProfile.Visibility).Result;
            foreach (var (holeX, holeZ) in top.EmptyDiscovered)
                AddVisibleThroughHole(holeX, holeZ, floorBand);

            return new HashSet<(int x, int z, int band)>(_visibleBelowScratch);
        }

        private void AddVisibleThroughHole(int holeX, int holeZ, int floorBand)
        {
            for (int k = floorBand - 1; k >= _minBand; k--)
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
