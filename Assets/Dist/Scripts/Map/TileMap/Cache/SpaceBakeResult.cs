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

        public SpaceBakeResult(int spaceId, int buildingId, RoomKey seedRoom, bool isOutdoor = false)
        {
            SpaceId = spaceId;
            BuildingId = buildingId;
            SeedRoom = seedRoom;
            IsOutdoor = isOutdoor;
        }
    }
}
