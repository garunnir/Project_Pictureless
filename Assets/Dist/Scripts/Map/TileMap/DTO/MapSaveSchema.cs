namespace IsoTilemap
{
    /// <summary>맵 JSON <see cref="MapSaveJsonDto.schemaVersion"/> 단계.</summary>
    public static class MapSaveSchema
    {
        public const int Current = 2;

        /// <summary>1 = placementSlot v1. 0·누락 = 레거시 tiles[].tileType.</summary>
        public const int PlacementSlotV1 = 1;

        /// <summary>floorFaces x,y,z = walkable (TileView.gridPos와 동일).</summary>
        public const int FloorWalkableCoords = 2;
    }
}
