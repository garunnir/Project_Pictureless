using System;
using System.Collections.Generic;
using UnityEngine;namespace IsoTilemap
{
    public struct TileData
    {
        public Guid tileDefId{init; get;}
        public TileState state;
        public TileIdentity identity{init; get;}
    }
    public struct TileState
    {
        /// <summary>DTO·기본값 호환. 런타임 맵·뷰 동기화는 <see cref="TileViewPresentationApplier"/>.</summary>
        public float characterOcclusion;
        /// <summary>DTO·기본값 호환. 런타임 표현은 applier <c>SetGhosted</c>.</summary>
        public bool isGhosted;
        /// <summary>DTO·기본값 호환. 런타임 표현은 applier <c>SetSelected</c>.</summary>
        public bool isSelected;
    }
    public readonly struct TileIdentity
    {
        /// <summary>255이면 칸 타일. 0=+X 면, 1=+Z 면(GridPos 정렬 앵커 기준, WallFace와 동일).</summary>
        public const byte EdgeFaceNone = 255;

        public string PrefabId{init; get;}

        /// <summary>
        /// 칸 타일이면 점유 셀입니다. 엣지 타일이면 점유 셀이 아니라 두 셀 사이 변을 정렬/저장하기 위한 앵커입니다.
        /// </summary>
        public Vector3Int GridPos{init; get;}
        public Vector3Int sizeUnit{init; get;}
        public byte tileType{init; get;}
        public byte edgeFace{init; get;}

        /// <summary>0=미할당. 야외 여부는 Hub <c>IsOutdoorEvaluation</c>으로만 판정(buildingId==0 추론 금지).</summary>
        public int buildingId { init; get; }

        /// <summary>0=room 미할당; 같은 buildingId·band 내 방 번호.</summary>
        public int roomId { init; get; }
    }

    /// <summary>플레이어 월드와 벽 간 거리에 따른 오클루전 강도(0~1) 매핑.</summary>
    [Serializable]
    public struct OcclusionProximitySettings
    {
        [Tooltip("그리드 1 칸 길이(월드 단위). TileHelper와 동일해야 합니다.")]
        public float CellSize;

        [Tooltip("이 거리(월드 XZ) 미만에서는 occlusion≈1")]
        public float OcclusionFullWithinDistance;

        [Tooltip("이 거리보다 멀면 occlusion=0 (Full 값보다 커야 함)")]
        public float OcclusionNoneBeyondDistance;

        [Tooltip("근접도 재계산 시 이전 값과 차이가 이 미만이면 배치 적용 스킵")]
        public float ApplyEpsilon;

        public static OcclusionProximitySettings DefaultUnity => new OcclusionProximitySettings
        {
            CellSize = 1f,
            OcclusionFullWithinDistance = 0.75f,
            OcclusionNoneBeyondDistance = 8f,
            ApplyEpsilon = 0.015f,
        };
    }

}
