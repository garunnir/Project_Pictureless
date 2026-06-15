using System;
using UnityEngine;

namespace IsoTilemap
{
    /// <summary>인접 두 층 사이의 수평 면. 앵커는 정렬과 저장을 위한 키이며, 셀 점유 위치가 아닙니다.</summary>
    public enum FloorFace : byte
    {
        PosY = 1,
    }

    public readonly struct FloorFaceKey : IEquatable<FloorFaceKey>
    {
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

        public bool Equals(FloorFaceKey other) => Anchor.Equals(other.Anchor) && Face == other.Face;

        public override bool Equals(object obj) => obj is FloorFaceKey other && Equals(other);

        public override int GetHashCode() =>
            HashCode.Combine(Anchor.x, Anchor.y, Anchor.z, (byte)Face);

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
            position = TileHelper.GetHorizontalCellFaceWorldPos(key.CellBelow, key.CellAbove, cellSize);
            rotation = Quaternion.identity;
        }

        public static FloorFaceKey ForWalkableCell(Vector3Int walkableCell) =>
            new FloorFaceKey(walkableCell + Vector3Int.down, FloorFace.PosY);
    }

    /// <summary>
    /// 월드 좌표에서 가장 가까운 수평 바닥면(<see cref="FloorFaceKey.GetWorldPose"/> 격자 경계)을 고릅니다.
    /// </summary>
    public static class FloorFacePicker
    {
        /// <summary>hub 점유 인덱스 기준 walkable 셀 → Floor 앵커.</summary>
        public static bool TryPickFromHub(
            TileMapCacheHub hub,
            Vector3 world,
            float cellSize,
            float feetWorldY,
            float cellEpsilonWorld,
            out FloorFaceKey key)
        {
            key = default;
            if (hub == null)
                return false;

            cellSize = Mathf.Max(1e-4f, cellSize);
            Vector3Int occupied = OccupiedCellCoord.ResolveFromWorld(
                hub, world, cellSize, feetWorldY, cellEpsilonWorld);
            if (!hub.CellHasFloor(occupied.x, occupied.y, occupied.z))
                return false;

            key = new FloorFaceKey(OccupiedCellCoord.FloorAnchorFromOccupiedCell(occupied), FloorFace.PosY);
            return true;
        }

        public static bool TryPickNearest(Vector3 world, float cellSize, out FloorFaceKey key)
        {
            key = default;
            cellSize = Mathf.Max(1e-4f, cellSize);

            Vector3Int rough = TileHelper.ConvertWorldToGrid(world, cellSize);
            int anchorY = Mathf.RoundToInt(world.y / cellSize) - 1;

            float bestSq = float.MaxValue;
            FloorFaceKey best = default;
            bool found = false;

            for (int dz = -3; dz <= 3; dz++)
            for (int dx = -3; dx <= 3; dx++)
            for (int dy = -2; dy <= 2; dy++)
            {
                Vector3Int cellBelow = new Vector3Int(rough.x + dx, anchorY + dy, rough.z + dz);
                ConsiderFace(cellBelow);
            }

            if (!found)
                return false;

            key = best;
            return true;

            void ConsiderFace(Vector3Int cellBelow)
            {
                FloorFaceKey k = new FloorFaceKey(cellBelow, FloorFace.PosY);
                FloorFaceKey.GetWorldPose(k, cellSize, out Vector3 pose, out _);
                float sq = (world - pose).sqrMagnitude;
                if (sq >= bestSq)
                    return;

                bestSq = sq;
                best = k;
                found = true;
            }
        }
    }
}
