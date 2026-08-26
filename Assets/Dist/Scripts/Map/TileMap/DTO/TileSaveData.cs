using System;
namespace IsoTilemap
{
    // 한 타일(Anchor 기준)의 순수 데이터 구조
    [Serializable]
    public class TileSaveData
    {
        public int x;
        public int y;
        public int z;

        public int sizeX;
        public int sizeY;
        public int sizeZ;

        public string prefabId;
        public byte tileType;

        /// <summary>tileType이 EdgeWall(4)일 때만 사용. 0=+X 면, 1=+Z 면. 생략 시 JSON 역직렬화 기본값 0.</summary>
        public byte face;

        /// <summary>Plant OccupiedCell only. Empty/null = non-plant tile.</summary>
        public string seedItemId;
        public int plantedWorldMinute;
        public bool fertilized;
        public int lastFruitHarvestWorldMinute;

        /// <summary>Water-cell fish trap (trap-only tiles[] row: prefabId empty). Omitted in legacy JSON.</summary>
        public string fishTrapBaitId;
        public int fishTrapBaitRemaining;
        public int fishTrapDeployedMinute;
        public int fishTrapAccumulatedFish;
    }
}