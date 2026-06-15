// ============================================================
// TileWorldPointUtil — 타일 월드 대표점 (오클루전·거리 계산용)
// ============================================================
using UnityEngine;

namespace IsoTilemap
{
    public static class TileWorldPointUtil
    {
        /// <summary>점유 셀 중심(수직·수평 면 타일은 면 pose).</summary>
        public static Vector3 GetOcclusionWorldPoint(in TileIdentity identity, Vector3Int occupiedCell, float cellSize)
        {
            if (TileIdentityUtil.IsFaceSlot(identity))
                return GetRepresentativeWorldPoint(identity, cellSize);

            return TileHelper.ConvertGridToWorldPos(occupiedCell, cellSize);
        }

        public static Vector3 GetRepresentativeWorldPoint(in TileIdentity identity, float cellSize)
        {
            cellSize = Mathf.Max(1e-4f, cellSize);
            if (TileIdentityUtil.IsVerticalFace(identity))
            {
                WallEdgeKey key = WallEdgeKey.FromWallTileIdentity(identity);
                WallEdgeKey.GetWorldPose(key, cellSize, out Vector3 pose, out _);
                return pose;
            }

            if (TileIdentityUtil.IsHorizontalFace(identity))
            {
                FloorFaceKey key = FloorFaceKey.FromFloorTileIdentity(identity);
                FloorFaceKey.GetWorldPose(key, cellSize, out Vector3 pose, out _);
                return pose;
            }

            Vector3 sizeF = (Vector3)identity.sizeUnit;
            Vector3 centroidOffset = (sizeF - Vector3.one) * 0.5f;
            Vector3 gridCenter = (Vector3)identity.GridPos + centroidOffset;
            return TileHelper.ConvertGridToWorldPos(gridCenter, cellSize);
        }
    }
}
