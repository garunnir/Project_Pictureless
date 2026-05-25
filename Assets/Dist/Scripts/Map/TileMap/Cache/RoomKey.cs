// ============================================================
// RoomKey — (buildingId, band, roomId) 단위 방 캐시 키
// ============================================================
using System;

namespace IsoTilemap
{
    public readonly struct RoomKey : IEquatable<RoomKey>
    {
        public int BuildingId { get; }
        public int Band { get; }
        public int RoomId { get; }

        public RoomKey(int buildingId, int band, int roomId)
        {
            BuildingId = buildingId;
            Band = band;
            RoomId = roomId;
        }

        public bool Equals(RoomKey other) =>
            BuildingId == other.BuildingId &&
            Band == other.Band &&
            RoomId == other.RoomId;

        public override bool Equals(object obj) => obj is RoomKey other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(BuildingId, Band, RoomId);

        public static bool operator ==(RoomKey left, RoomKey right) => left.Equals(right);

        public static bool operator !=(RoomKey left, RoomKey right) => !left.Equals(right);

        public override string ToString() => $"Room({BuildingId},{Band},{RoomId})";
    }
}
