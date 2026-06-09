// ============================================================
// TileDefinitionCollisionSettings — TileDefinition Inspector 충돌·오클루전 토글 struct
// ============================================================
using System;
using Sirenix.OdinInspector;

namespace IsoTilemap
{
    [Serializable]
    [Title("점유 셀", "Floor / Wall 등 GridPos 앵커 타일")]
    public struct TileOccupiedCellCollision
    {
        [LabelText("논리 바닥 (착지)")]
        public bool providesLogicalFloor;

        [InfoBox("점유 셀 topology 통행 막힘 + BFS 오클루전 후보를 동시에 켜거나 끕니다.", InfoMessageType.None)]
        [LabelText("통행·오클루전 차단 (연동)")]
        public bool blocksPassageAndOcclusion;

        [LabelText("Physics Collider")]
        public bool usePhysicsCollider;

        [LabelText("통행/오클루전 개별 설정")]
        public bool splitPassageAndOcclusion;

        [ShowIf(nameof(splitPassageAndOcclusion))]
        [LabelText("통행 차단")]
        public bool blocksOccupiedCells;

        [ShowIf(nameof(splitPassageAndOcclusion))]
        [LabelText("오클루전 후보")]
        public bool occludesOccupiedCells;
    }

    [Serializable]
    [Title("엣지", "SlimWall 등 두 셀 사이 변")]
    public struct TileEdgeCollision
    {
        [InfoBox("엣지 topology 통행·방 경계·오클루전 후보를 동시에 켜거나 끕니다.", InfoMessageType.None)]
        [LabelText("통행·방·오클루전 (연동)")]
        public bool blocksPassageAndOcclusion;

        [LabelText("통행/방/오클루전 개별 설정")]
        public bool splitPassageAndOcclusion;

        [ShowIf(nameof(splitPassageAndOcclusion))]
        [LabelText("통행 차단")]
        public bool blocksEdge;

        [ShowIf(nameof(splitPassageAndOcclusion))]
        [LabelText("방 경계")]
        public bool separatesRoom;

        [ShowIf(nameof(splitPassageAndOcclusion))]
        [LabelText("오클루전 후보")]
        public bool occludesEdge;
    }
}
