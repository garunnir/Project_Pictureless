// ============================================================
// MapParticleFloorLandingProbe — 파티클용 논리 바닥 착지면 조회 (컬럼 인덱스)
// ============================================================
using UnityEngine;

namespace IsoTilemap
{
    /// <summary>
    /// 프레임 단위 (x,z,maxCellY) → surface 캐시. <see cref="MapParticleFloorLanding"/>이 소유합니다.
    /// </summary>
    public sealed class MapParticleFloorLandingQueryCache
    {
        struct Key : System.IEquatable<Key>
        {
            public readonly int X;
            public readonly int Z;
            public readonly int MaxCellY;

            public Key(int x, int z, int maxCellY)
            {
                X = x;
                Z = z;
                MaxCellY = maxCellY;
            }

            public bool Equals(Key other) =>
                X == other.X && Z == other.Z && MaxCellY == other.MaxCellY;

            public override bool Equals(object obj) =>
                obj is Key other && Equals(other);

            public override int GetHashCode() =>
                (X * 397) ^ (Z * 31) ^ MaxCellY;
        }

        readonly System.Collections.Generic.Dictionary<Key, (float surfaceY, Vector3Int floorCell)> _map =
            new(64);

        public void Clear() => _map.Clear();

        public bool TryGet(int x, int z, int maxCellY, out float surfaceY, out Vector3Int floorCell)
        {
            if (_map.TryGetValue(new Key(x, z, maxCellY), out var hit))
            {
                surfaceY = hit.surfaceY;
                floorCell = hit.floorCell;
                return true;
            }

            surfaceY = 0f;
            floorCell = default;
            return false;
        }

        public void Set(int x, int z, int maxCellY, float surfaceY, Vector3Int floorCell) =>
            _map[new Key(x, z, maxCellY)] = (surfaceY, floorCell);

        public void SetMiss(int x, int z, int maxCellY) =>
            _map[new Key(x, z, maxCellY)] = (float.NaN, default);

        public bool IsMiss(float surfaceY) => float.IsNaN(surfaceY);
    }

    /// <summary>
    /// 컬럼 인덱스로 파티클 월드 위치의 논리 바닥 표면을 조회합니다.
    /// <see cref="OccupiedCellCoord.ResolveFromWorld"/> Y루프를 쓰지 않습니다.
    /// </summary>
    public static class MapParticleFloorLandingProbe
    {
        public static bool TryResolveSurface(
            TileMapCacheHub hub,
            Vector3 world,
            float cellSize,
            out float surfaceY,
            out Vector3Int floorCell,
            MapParticleFloorLandingQueryCache cache = null)
        {
            surfaceY = 0f;
            floorCell = default;

            if (hub == null)
                return false;

            cellSize = Mathf.Max(1e-4f, cellSize);
            Vector3Int seed = TileHelper.ConvertWorldToGrid(world, cellSize);
            int maxCellY = seed.y;

            if (cache != null &&
                cache.TryGet(seed.x, seed.z, maxCellY, out float cachedY, out Vector3Int cachedCell))
            {
                if (cache.IsMiss(cachedY))
                    return false;

                surfaceY = cachedY;
                floorCell = cachedCell;
                return true;
            }

            if (!hub.Topology.Index.TryGetHighestWalkableFloorAtOrBelow(
                    seed.x,
                    seed.z,
                    maxCellY,
                    out int floorCellY))
            {
                cache?.SetMiss(seed.x, seed.z, maxCellY);
                return false;
            }

            floorCell = new Vector3Int(seed.x, floorCellY, seed.z);
            surfaceY = floorCellY * cellSize;
            cache?.Set(seed.x, seed.z, maxCellY, surfaceY, floorCell);
            return true;
        }
    }
}
