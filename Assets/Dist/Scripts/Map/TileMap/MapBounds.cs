// ============================================================
// MapBounds — Play 런타임 mapBounds (저장 SSOT 또는 로드 fallback)
// ============================================================

namespace IsoTilemap
{
    public readonly struct MapBounds
    {
        public static MapBounds Unbounded => default;

        public bool HasBounds { get; }
        public int MinX { get; }
        public int MaxX { get; }
        public int MinZ { get; }
        public int MaxZ { get; }
        public int MinY { get; }

        public MapBounds(int minX, int maxX, int minZ, int maxZ, int minY)
        {
            HasBounds = true;
            MinX = minX;
            MaxX = maxX;
            MinZ = minZ;
            MaxZ = maxZ;
            MinY = minY;
        }

        public bool IsInMapBounds(int x, int y, int z)
        {
            if (!HasBounds)
                return true;

            return x >= MinX && x <= MaxX
                && z >= MinZ && z <= MaxZ
                && y >= MinY;
        }

        public bool IsInMapBoundsXZ(int x, int z)
        {
            if (!HasBounds)
                return true;

            return x >= MinX && x <= MaxX && z >= MinZ && z <= MaxZ;
        }
    }
}
