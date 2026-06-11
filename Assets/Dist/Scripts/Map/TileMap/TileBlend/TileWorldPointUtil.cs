// ============================================================
// TileWorldPointUtil — 타일 월드 대표점 (오클루전·거리 계산용)
// ============================================================
using UnityEngine;

namespace IsoTilemap
{
    public static class TileWorldPointUtil
    {
        /// <summary>점유 셀 중심(EdgeWall·Floor face는 면 중점).</summary>
        public static Vector3 GetOcclusionWorldPoint(in TileIdentity identity, Vector3Int occupiedCell, float cellSize)
        {
            var type = (TileView.TileType)identity.tileType;
            if (type == TileView.TileType.EdgeWall && identity.edgeFace != TileIdentity.EdgeFaceNone)
                return GetRepresentativeWorldPoint(identity, cellSize);

            if (type == TileView.TileType.Floor && FloorFaceKey.IsAnchorFormat(identity.floorFace))
                return GetRepresentativeWorldPoint(identity, cellSize);

            return TileHelper.ConvertGridToWorldPos(occupiedCell, cellSize);
        }

        public static Vector3 GetRepresentativeWorldPoint(in TileIdentity identity, float cellSize)
        {
            cellSize = Mathf.Max(1e-4f, cellSize);
            var type = (TileView.TileType)identity.tileType;
            if (type == TileView.TileType.EdgeWall && identity.edgeFace != TileIdentity.EdgeFaceNone)
            {
                WallEdgeKey key = WallEdgeKey.FromEdgeTileIdentity(identity);
                WallEdgeKey.GetWorldPose(key, cellSize, out Vector3 pose, out _);
                return pose;
            }

            if (type == TileView.TileType.Floor && FloorFaceKey.IsAnchorFormat(identity.floorFace))
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
