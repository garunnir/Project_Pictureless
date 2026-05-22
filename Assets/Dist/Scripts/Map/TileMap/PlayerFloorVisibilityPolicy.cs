// ============================================================
// PlayerFloorVisibilityPolicy — 플레이어 층·방 BFS 기준 Y축 타일 가시성
// ============================================================
using System;
using System.Collections.Generic;

namespace IsoTilemap
{
    public readonly struct FloorVisibilityContext : IEquatable<FloorVisibilityContext>
    {
        public float PlayerHeightWorldY { get; }
        public int FloorBand { get; }
        public HashSet<(int x, int z, int band)> VisibleBelowCells { get; }

        public FloorVisibilityContext(
            float playerHeightWorldY,
            int floorBand,
            HashSet<(int x, int z, int band)> visibleBelowCells)
        {
            PlayerHeightWorldY = playerHeightWorldY;
            FloorBand = floorBand;
            VisibleBelowCells = visibleBelowCells ?? new HashSet<(int x, int z, int band)>();
        }

        public bool Equals(FloorVisibilityContext other) =>
            FloorBand == other.FloorBand &&
            ReferenceEquals(VisibleBelowCells, other.VisibleBelowCells);

        public override bool Equals(object obj) => obj is FloorVisibilityContext other && Equals(other);

        public override int GetHashCode() => FloorBand;

        public static bool operator ==(FloorVisibilityContext left, FloorVisibilityContext right) =>
            left.Equals(right);

        public static bool operator !=(FloorVisibilityContext left, FloorVisibilityContext right) =>
            !left.Equals(right);
    }

    /// <summary>플레이어 월드 높이·그리드 XZ 기준 층(Y) 타일 가시성. XZ 청크 스트리밍과 분리.</summary>
    public sealed class PlayerFloorVisibilityPolicy
    {
        private readonly float _cellSize;
        private readonly float _bandEpsilonWorld;
        private readonly int _minBand;
        private readonly int[] _distinctFloorBands;
        private readonly FloorMapIndex _index;
        private readonly FloorRoomCache _roomCache;

        private PlayerFloorVisibilityPolicy(
            float cellSize,
            float bandEpsilonWorld,
            int minBand,
            int[] distinctFloorBands,
            FloorMapIndex index,
            FloorRoomCache roomCache)
        {
            _cellSize = cellSize;
            _bandEpsilonWorld = bandEpsilonWorld;
            _minBand = minBand;
            _distinctFloorBands = distinctFloorBands;
            _index = index;
            _roomCache = roomCache;
        }

        public int MinBand => _minBand;

        public FloorRoomCache RoomCache => _roomCache;

        public static PlayerFloorVisibilityPolicy Build(
            IReadOnlyList<TileData> tiles,
            FloorRoomCache roomCache,
            float cellSize,
            float bandEpsilonWorld = 0f)
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

            return new PlayerFloorVisibilityPolicy(
                cellSize,
                bandEpsilonWorld,
                distinctBands[0],
                distinctBands,
                roomCache.Index,
                roomCache);
        }

        public FloorVisibilityContext ResolveContext(float playerHeightWorldY, int gridX, int gridZ)
        {
            int floorBand = ResolveFloorBand(playerHeightWorldY);
            var visibleBelow = BuildVisibleBelowCells(floorBand, gridX, gridZ);
            return new FloorVisibilityContext(playerHeightWorldY, floorBand, visibleBelow);
        }

        public bool IsTileVisible(TileData tile, in FloorVisibilityContext ctx)
        {
            var gridPos = tile.identity.GridPos;
            float tileFloorWorldY = gridPos.y * _cellSize;
            if (tileFloorWorldY > ctx.PlayerHeightWorldY)
                return false;

            int tileBand = gridPos.y;
            if (tileBand >= ctx.FloorBand)
                return true;

            var type = (TileView.TileType)tile.identity.tileType;
            if (type is TileView.TileType.Wall or TileView.TileType.EdgeWall or TileView.TileType.Obstacle)
                return true;

            if (ctx.VisibleBelowCells.Contains((gridPos.x, gridPos.z, tileBand)))
                return true;

            return false;
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
            var visible = new HashSet<(int x, int z, int band)>();
            if (floorBand <= _minBand)
                return visible;

            FloorBfsResult top = _roomCache.GetOrCompute(
                floorBand, gridX, gridZ, FloorRoomBfsProfile.Visibility);
            foreach (var (holeX, holeZ) in top.EmptyDiscovered)
                AddVisibleThroughHole(visible, holeX, holeZ, floorBand);

            return visible;
        }

        private void AddVisibleThroughHole(
            HashSet<(int x, int z, int band)> visible,
            int holeX,
            int holeZ,
            int floorBand)
        {
            for (int k = floorBand - 1; k >= _minBand; k--)
            {
                if (!_index.HasAnyTile(holeX, holeZ, k))
                    continue;

                HashSet<(int x, int z)> room = _roomCache.GetOrComputeVisited(
                    k, holeX, holeZ, FloorRoomBfsProfile.Visibility);
                foreach (var (vx, vz) in room)
                    visible.Add((vx, vz, k));
                return;
            }
        }
    }
}
