// ============================================================
// SpaceBakeResult — Space bake 산출 (야외/실내 판정용)
// ============================================================
namespace IsoTilemap
{
    public sealed class SpaceBakeResult
    {
        public int SpaceId { get; }
        public int BuildingId { get; }
        public RoomKey SeedRoom { get; }
        public bool IsOutdoor { get; set; }
        public int MinFloorY { get; private set; } = int.MaxValue;
        public int MaxFloorY { get; private set; } = int.MinValue;
        public bool HasFloorBounds => MinFloorY <= MaxFloorY;

        public SpaceBakeResult(int spaceId, int buildingId, RoomKey seedRoom, bool isOutdoor = false)
        {
            SpaceId = spaceId;
            BuildingId = buildingId;
            SeedRoom = seedRoom;
            IsOutdoor = isOutdoor;
        }

        public void IncludeFloorCell(UnityEngine.Vector3Int floorCell)
        {
            if (floorCell.y < MinFloorY)
                MinFloorY = floorCell.y;
            if (floorCell.y > MaxFloorY)
                MaxFloorY = floorCell.y;
        }
    }
}
