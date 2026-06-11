using System;
using UnityEngine;

namespace IsoTilemap
{
    /// <summary>인접 두 층 사이의 수평 면. 앵커는 정렬과 저장을 위한 키이며, 셀 점유 위치가 아닙니다.</summary>
    /// <remarks>
    /// <see cref="FloorFaceKey"/>는 <see cref="FloorFaceKey.CellBelow"/>와 <see cref="FloorFaceKey.CellAbove"/> 사이의 면을 나타냅니다.
    /// Face는 CellBelow에서 +Y 방향의 CellAbove를 향합니다.
    /// </remarks>
    public enum FloorFace : byte
    {
        /// <summary>미설정. <see cref="TileIdentity.GridPos"/>는 walkable 셀(레거시·에디터).</summary>
        UnsetWalkable = 0,
        /// <summary>+Y 면. <see cref="TileIdentity.GridPos"/>는 CellBelow 앵커.</summary>
        PosY = 1,
    }

    public readonly struct FloorFaceKey : IEquatable<FloorFaceKey>
    {
        /// <summary>정렬과 저장을 위한 아래 셀. 면이 이 셀을 점유한다는 의미는 아닙니다.</summary>
        public Vector3Int Anchor { get; }
        public FloorFace Face { get; }
        public Vector3Int CellBelow => Anchor;
        public Vector3Int CellAbove => Anchor + Vector3Int.up;

        public FloorFaceKey(Vector3Int anchor, FloorFace face)
        {
            Anchor = anchor;
            Face = face;
        }

        public static FloorFaceKey FromFloorTileIdentity(in TileIdentity id) =>
            new FloorFaceKey(id.GridPos, FloorFace.PosY);

        public static bool IsAnchorFormat(byte floorFace) => floorFace == (byte)FloorFace.PosY;

        public bool Equals(FloorFaceKey other) => Anchor.Equals(other.Anchor) && Face == other.Face;

        public override bool Equals(object obj) => obj is FloorFaceKey other && Equals(other);

        public override int GetHashCode() =>
            HashCode.Combine(Anchor.x, Anchor.y, Anchor.z, (byte)Face);

        /// <summary>below→above가 Y축 카드널 이웃일 때 두 층 사이 공유 면 키를 만듭니다. (x,z는 같아야 함.)</summary>
        public static bool TryBetween(Vector3Int cellBelow, Vector3Int cellAbove, out FloorFaceKey key)
        {
            key = default;
            if (cellBelow.x != cellAbove.x || cellBelow.z != cellAbove.z)
                return false;

            var d = cellAbove - cellBelow;
            if (d == Vector3Int.up)
            {
                key = new FloorFaceKey(cellBelow, FloorFace.PosY);
                return true;
            }

            if (d == Vector3Int.down)
            {
                key = new FloorFaceKey(cellAbove, FloorFace.PosY);
                return true;
            }

            return false;
        }

        public void Deconstruct(out Vector3Int cellBelow, out Vector3Int cellAbove)
        {
            cellBelow = CellBelow;
            cellAbove = CellAbove;
        }

        public static void GetWorldPose(in FloorFaceKey key, float cellSize, out Vector3 position, out Quaternion rotation)
        {
            Vector3 c0 = TileHelper.ConvertGridToWorldPos(key.CellBelow, cellSize);
            Vector3 c1 = TileHelper.ConvertGridToWorldPos(key.CellAbove, cellSize);
            position = (c0 + c1) * 0.5f;
            rotation = Quaternion.identity;
        }

        /// <summary>walkable 셀 좌표에서 발밑 face 키를 만듭니다.</summary>
        public static FloorFaceKey ForWalkableCell(Vector3Int walkableCell) =>
            new FloorFaceKey(walkableCell + Vector3Int.down, FloorFace.PosY);
    }

    public static class FloorFaceIdentityUtil
    {
        /// <summary>레거시 walkable GridPos를 face 앵커 형식으로 정규화합니다.</summary>
        public static TileIdentity NormalizeFloorIdentity(in TileIdentity id)
        {
            if ((TileView.TileType)id.tileType != TileView.TileType.Floor)
                return id;

            if (FloorFaceKey.IsAnchorFormat(id.floorFace))
                return id;

            return FromWalkableCellPlacement(id);
        }

        /// <summary>에디터 walkable 셀 중심 배치를 face 앵커 identity로 변환합니다.</summary>
        public static TileIdentity FromWalkableCellPlacement(in TileIdentity draft)
        {
            if ((TileView.TileType)draft.tileType != TileView.TileType.Floor)
                return draft;

            Vector3Int walkable = draft.GridPos;
            return new TileIdentity
            {
                PrefabId = draft.PrefabId,
                GridPos = walkable + Vector3Int.down,
                sizeUnit = draft.sizeUnit,
                tileType = draft.tileType,
                edgeFace = TileIdentity.EdgeFaceNone,
                floorFace = (byte)FloorFace.PosY,
                buildingId = draft.buildingId,
                roomId = draft.roomId,
                collisionFlags = draft.collisionFlags,
            };
        }
    }
}
