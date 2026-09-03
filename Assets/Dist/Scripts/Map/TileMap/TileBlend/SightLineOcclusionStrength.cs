// ============================================================
// SightLineOcclusionStrength — 카메라↔플레이어 시선 3D 수직 거리 가림 강도(0~1)
// ============================================================
using UnityEngine;

namespace IsoTilemap
{
    public static class SightLineOcclusionStrength
    {
        const float SegmentLengthEpsilonSqr = 1e-8f;

        /// <summary>
        /// 플레이어를 강하게 가릴수록 1에 가깝게.
        /// 카메라↔플레이어 3D 선분에 대한 수직 거리(XYZ)로 강도를 산출합니다.
        /// </summary>
        public static float Evaluate(
            Vector3 cameraWorld,
            Vector3 playerWorld,
            Vector3 tileWorld,
            float cellSize,
            in SightLineBlendSettings settings)
        {
            cellSize = Mathf.Max(1e-4f, cellSize);

            Vector3 ab = cameraWorld - playerWorld;
            float abSqr = ab.sqrMagnitude;
            if (abSqr < SegmentLengthEpsilonSqr)
            {
                float dist = Vector3.Distance(tileWorld, playerWorld);
                return PerpDistanceCurve(dist, settings);
            }

            Vector3 ap = tileWorld - playerWorld;
            float t = Vector3.Dot(ap, ab) / abSqr;
            float behindMargin = cellSize * Mathf.Max(0f, settings.SegmentTEpsilon);
            if (t < 0f)
            {
                float behindDist = -t * Mathf.Sqrt(abSqr);
                if (behindDist > behindMargin)
                    return 0f;
            }

            t = Mathf.Clamp01(t);
            Vector3 closest = playerWorld + t * ab;
            float perpDist = Vector3.Distance(tileWorld, closest);
            return PerpDistanceCurve(perpDist, settings);
        }

        public static float PerpDistanceCurve(float perpDistance, in SightLineBlendSettings settings) =>
            OcclusionBlendMath.DistanceToOcclusion01(
                perpDistance,
                settings.FullBlendWithinPerpDistance,
                settings.NoneBeyondPerpDistance);

        /// <summary>플레이어 XZ 기준 +X·-Z 사분면만 통과 (dx≥0, dz≤0).</summary>
        public static bool PassesPlayerDownXQuadrant(Vector3Int cell, Vector3Int playerCell) =>
            cell.x >= playerCell.x && cell.z <= playerCell.z;

        /// <summary>
        /// 근접 시선 오클루전 후보가 +X·-Z 사분면에 있는지 검사합니다.
        /// 수직 면 벽은 변 중점(2배 정수 좌표) 기준 — 밴드 셀만 보면 -X/+Z 면이 누락될 수 있습니다.
        /// </summary>
        public static bool PassesPlayerDownXQuadrantForOccluder(
            in TileData tile,
            Vector3Int occupiedCell,
            Vector3Int playerCell)
        {
            if (TileIdentityUtil.IsVerticalFace(tile.identity))
            {
                WallEdgeKey key = WallEdgeKey.FromWallTileIdentity(tile.identity);
                int centerX2 = key.CellA.x + key.CellB.x;
                int centerZ2 = key.CellA.z + key.CellB.z;
                int playerX2 = playerCell.x << 1;
                int playerZ2 = playerCell.z << 1;
                return centerX2 >= playerX2 && centerZ2 <= playerZ2;
            }

            return PassesPlayerDownXQuadrant(occupiedCell, playerCell);
        }

        /// <summary>
        /// walkable 층(CellAbove)이 플레이어 층 이하인 Floor face는 §3 면제.
        /// 윗층 천장 face(walkable y &gt; playerFloor)만 근접 블렌드 대상.
        /// </summary>
        public static bool ShouldExemptFloor(
            in TileData tile,
            Vector3Int occupiedCell,
            Vector3Int playerCell,
            int playerFloorCellY)
        {
            if (!TileIdentityUtil.IsFloorTile(tile.identity))
                return false;

            Vector3Int walkable = TileIdentityUtil.GetWalkableCell(tile.identity);

            return walkable.y <= playerFloorCellY;
        }
    }
}
