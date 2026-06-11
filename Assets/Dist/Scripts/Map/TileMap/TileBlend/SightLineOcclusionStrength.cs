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



        public static float PerpDistanceCurve(float perpDistance, in SightLineBlendSettings settings)

        {

            float full = Mathf.Max(0f, settings.FullBlendWithinPerpDistance);

            float none = Mathf.Max(full + 1e-3f, settings.NoneBeyondPerpDistance);

            float clamped = Mathf.Clamp(perpDistance, full, none);

            return Mathf.InverseLerp(none, full, clamped);

        }



        /// <summary>플레이어 XZ 기준 +X·-Z 사분면만 통과 (dx≥0, dz≤0).</summary>
        public static bool PassesPlayerDownXQuadrant(Vector3Int cell, Vector3Int playerCell) =>
            cell.x >= playerCell.x && cell.z <= playerCell.z;

        /// <summary>
        /// 근접 시선 오클루전 후보가 +X·-Z 사분면에 있는지 검사합니다.
        /// EdgeWall은 변 중점(2배 정수 좌표) 기준 — 밴드 셀만 보면 -X/+Z 면이 누락될 수 있습니다.
        /// </summary>
        public static bool PassesPlayerDownXQuadrantForOccluder(
            in TileData tile,
            Vector3Int occupiedCell,
            Vector3Int playerCell)
        {
            if ((TileView.TileType)tile.identity.tileType == TileView.TileType.EdgeWall &&
                tile.identity.edgeFace != TileIdentity.EdgeFaceNone)
            {
                WallEdgeKey key = WallEdgeKey.FromEdgeTileIdentity(tile.identity);
                int centerX2 = key.CellA.x + key.CellB.x;
                int centerZ2 = key.CellA.z + key.CellB.z;
                int playerX2 = playerCell.x << 1;
                int playerZ2 = playerCell.z << 1;
                return centerX2 >= playerX2 && centerZ2 <= playerZ2;
            }

            return PassesPlayerDownXQuadrant(occupiedCell, playerCell);
        }

        public static bool IsStructuralOcclusionTile(in TileData tile)
        {
            var type = (TileView.TileType)tile.identity.tileType;
            return type is TileView.TileType.Wall or TileView.TileType.EdgeWall;
        }

        /// <summary>실내에서는 Wall·EdgeWall 오클루전을 BFS(<see cref="WallOcclusionFinder"/>)만 담당.</summary>
        public static bool ShouldSkipProximityForIndoorStructural(bool isPlayerOutdoor, in TileData tile) =>
            !isPlayerOutdoor && IsStructuralOcclusionTile(tile);

        /// <summary>플레이어 XZ 기둥 발밑·이하 Floor는 가리지 않음.</summary>

        public static bool ShouldExemptFloor(

            in TileData tile,

            Vector3Int occupiedCell,

            Vector3Int playerCell,

            int playerFloorCellY)

        {

            if ((TileView.TileType)tile.identity.tileType != TileView.TileType.Floor)

                return false;



            if (occupiedCell.x != playerCell.x || occupiedCell.z != playerCell.z)

                return false;



            return occupiedCell.y <= playerFloorCellY;

        }

    }

}


