// ============================================================
// PlantCellSaveData — legacy plantCells[] DTO (load-migrate only)
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
