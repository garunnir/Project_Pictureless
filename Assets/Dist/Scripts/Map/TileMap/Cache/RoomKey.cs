// ============================================================
// RoomKey — (buildingId, cellY, roomId) 단위 방 캐시 키
// ============================================================
using System;

namespace IsoTilemap
{
    public readonly struct RoomKey : IEquatable<RoomKey>
    {
        public int BuildingId { get; }
        public int CellY { get; }
        public int RoomId { get; }

        public RoomKey(int buildingId, int cellY, int roomId)
        {
            BuildingId = buildingId;
            CellY = cellY;
            RoomId = roomId;
        }

        public bool Equals(RoomKey other) =>
            BuildingId == other.BuildingId &&
            CellY == other.CellY &&
            RoomId == other.RoomId;

        public override bool Equals(object obj) => obj is RoomKey other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(BuildingId, CellY, RoomId);

        public static bool operator ==(RoomKey left, RoomKey right) => left.Equals(right);

        public static bool operator !=(RoomKey left, RoomKey right) => !left.Equals(right);

        public override string ToString() => $"Room({BuildingId},{CellY},{RoomId})";
    }
}
