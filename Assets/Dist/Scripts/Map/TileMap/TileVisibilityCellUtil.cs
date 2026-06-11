namespace IsoTilemap
{
    /// <summary>가시성·스트리밍에서 타일의 대표 cellY를 해석합니다.</summary>
    public static class TileVisibilityCellUtil
    {
        public static int GetCellY(in TileData tile)
        {
            var type = (TileView.TileType)tile.identity.tileType;
            if (type == TileView.TileType.Floor && FloorFaceKey.IsAnchorFormat(tile.identity.floorFace))
                return FloorFaceKey.FromFloorTileIdentity(tile.identity).CellAbove.y;

            return tile.identity.GridPos.y;
        }
    }
}
