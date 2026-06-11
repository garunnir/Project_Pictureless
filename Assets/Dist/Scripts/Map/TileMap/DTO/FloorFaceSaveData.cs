using System;

namespace IsoTilemap
{
    [Serializable]
    public class FloorFaceSaveData
    {
        public int x;
        public int y;
        public int z;
        /// <summary>0 = +Y 면 (앵커=CellBelow).</summary>
        public byte face;
        public string prefabId;
    }
}
