// ============================================================
// MapBoundsSaveData — JSON mapBounds 필드 SSOT (XZ 직육면체 + Y minY만)
// ============================================================

using System;

namespace IsoTilemap
{
    [Serializable]
    public struct MapBoundsSaveData
    {
        public bool hasMapBounds;
        public int mapBoundsMinX;
        public int mapBoundsMaxX;
        public int mapBoundsMinZ;
        public int mapBoundsMaxZ;
        /// <summary>하단 Y 경계만. maxY 없음 — bounds 안에서는 위로 무제한.</summary>
        public int mapBoundsMinY;
    }
}
