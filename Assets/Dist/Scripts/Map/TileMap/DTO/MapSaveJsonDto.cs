using System;
using System.Collections.Generic;
using UnityEngine;
namespace IsoTilemap
{
    [Serializable]
    public class MapSaveJsonDto
    {
        /// <summary>1 = placementSlot 기반 v1. 0·누락 = 레거시 tiles[].tileType.</summary>
        public int schemaVersion;

        /// <summary>그리드 1칸 월드 길이. 0 이하·누락(구 JSON)이면 로더 fallback 사용.</summary>
        public float gridCellSize = 1f;

        public List<TileSaveData> tiles = new List<TileSaveData>();
        public List<WallEdgeSaveData> wallEdges = new List<WallEdgeSaveData>();
        public List<FloorFaceSaveData> floorFaces = new List<FloorFaceSaveData>();

        /// <summary>맵 혈흔 스탬프 (월드 좌표). tiles와 별 레이어. 구 JSON 누락 시 empty.</summary>
        public List<BloodStampSaveData> bloodStamps = new List<BloodStampSaveData>();
    }
}