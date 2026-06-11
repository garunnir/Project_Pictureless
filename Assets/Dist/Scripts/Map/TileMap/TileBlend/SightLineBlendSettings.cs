// ============================================================
// SightLineBlendSettings — 카메라↔플레이어 시선 근접 블렌드 거리·밴드
// ============================================================
using System;
using UnityEngine;

namespace IsoTilemap
{
    [Serializable]
    public struct SightLineBlendSettings
    {
        [Tooltip("그리드 1칸 길이(월드). TileHelper와 동일해야 합니다.")]
        public float CellSize;

        [Tooltip("카메라↔플레이어 3D 선분에 수직 거리가 이 값 미만이면 occlusion≈1")]
        public float FullBlendWithinPerpDistance;

        [Tooltip("3D 선분 수직 거리가 이 값보다 크면 occlusion=0")]
        public float NoneBeyondPerpDistance;

        [Tooltip("세그먼트 샘플 셀 주변 XZ Chebyshev 확장(타일 단위)")]
        public int BandRadiusCells;

        [Tooltip("시선 세그먼트 플레이어 뒤쪽 여유(셀 단위). 이보다 뒤에 있으면 occlusion=0")]
        public float SegmentTEpsilon;

        [Tooltip("변화가 이 미만이면 적용 스킵")]
        public float ApplyEpsilon;

        [Tooltip("0=즉시 반영. 클수록 프레임 간 occlusion 변화가 완만해집니다.")]
        public float OcclusionSmoothSpeed;

        public static SightLineBlendSettings DefaultUnity => new SightLineBlendSettings
        {
            CellSize = 1f,
            FullBlendWithinPerpDistance = 1.25f,
            NoneBeyondPerpDistance = 10f,
            BandRadiusCells = 2,
            SegmentTEpsilon = 0.15f,
            ApplyEpsilon = 0.01f,
            OcclusionSmoothSpeed = 6f,
        };
    }
}
