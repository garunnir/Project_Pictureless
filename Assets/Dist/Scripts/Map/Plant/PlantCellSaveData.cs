// ============================================================
// PlantCellSaveData — 맵 JSON 식물 셀 1건 (till overlay + fertilized)
// ============================================================

using System;

namespace IsoTilemap
{
    [Serializable]
    public class PlantCellSaveData
    {
        public int cx;
        public int cy;
        public int cz;
        public string seedItemId;
        public int plantedWorldMinute;
        public bool fertilized;
        public bool tilled;
    }
}
