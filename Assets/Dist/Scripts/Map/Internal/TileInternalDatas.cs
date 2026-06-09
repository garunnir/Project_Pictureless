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

        /// <summary>bake: <see cref="BuildingIdUnassigned"/> 미할당, <see cref="BuildingIdOutdoor"/> 광장 Floor, &gt;0 건물. 런타임 야외 분기는 <c>IsOutdoorEvaluation</c>.</summary>
        public int buildingId { init; get; }

        /// <summary>bake 초기·건물 BFS 대기 Floor.</summary>
        public const int BuildingIdUnassigned = 0;

        /// <summary>MinCellY 광장 Floor (야외 BFS 확정 후).</summary>
        public const int BuildingIdOutdoor = -1;

        /// <summary>0=room 미할당; 같은 buildingId·cellY 내 방 번호.</summary>
        public int roomId { init; get; }

        /// <summary><see cref="TileDefinition"/> 충돌·오클루전 bake. <see cref="TileCollisionProfile.FromDefinition"/>.</summary>
        public byte collisionFlags { init; get; }
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

        [Tooltip("플레이어 주변 삼각 마스크로 추가 숨김 벽을 포함합니다. 끄면 BFS·거리 오클루전만 적용됩니다.")]
        public bool PlayerProximityMaskEnabled;

        public static OcclusionProximitySettings DefaultUnity => new OcclusionProximitySettings
        {
            CellSize = 1f,
            OcclusionFullWithinDistance = 0.75f,
            OcclusionNoneBeyondDistance = 8f,
            ApplyEpsilon = 0.015f,
            PlayerProximityMaskEnabled = true,
        };
    }

}
