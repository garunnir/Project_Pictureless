using System;

namespace IsoTilemap
{
    [Serializable]
    public class FloorFaceSaveData
    {
        public int x;
        public int y;
        public int z;
        /// <summary>+Y 면. JSON x,y,z = CellBelow 앵커.</summary>
        public byte face;
        public string prefabId;
    }
}
