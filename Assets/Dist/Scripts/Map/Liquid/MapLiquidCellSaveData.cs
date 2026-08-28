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

        /// <summary>
        /// 자체 온도(deci°C). 0은 물의 어는점이므로 "누락"과 구별할 수 없다 —
        /// 유효성은 <see cref="MapSaveJsonDto.hasLiquidTemperature"/>가 판정한다.
        /// </summary>
        public short tempDeciC;
    }
}
