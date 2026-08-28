// ============================================================
// MapLiquidCellSaveData — 맵 JSON 액체 셀 1건
// ============================================================

using System;

namespace IsoTilemap
{
    [Serializable]
    public class MapLiquidCellSaveData
    {
        public int x;
        public int y;
        public int z;
        public string typeId;
        public byte level;
        public ushort remainderMl;
    }
}
