// ============================================================
// TilePlacementSlot — 타일이 그리드 어디에 놓이는지 (셀 vs 면)
// ============================================================
namespace IsoTilemap
{
    public enum TilePlacementSlot : byte
    {
        None = 0,
        /// <summary>Wall, Slope 등. <see cref="TileIdentity.GridPos"/> = 점유 셀.</summary>
        OccupiedCell = 1,
        /// <summary>SlimWall 등. <see cref="TileIdentity.GridPos"/> = 앵커, <see cref="TileIdentity.wallFace"/> = 방향.</summary>
        VerticalFace = 2,
        /// <summary>바닥면. <see cref="TileIdentity.GridPos"/> = walkable, <see cref="TileIdentity.floorFace"/> = 방향.</summary>
        HorizontalFace = 3,
    }
}
