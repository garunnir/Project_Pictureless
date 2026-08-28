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

        /// <summary>
        /// liquidAuthoringFaces 전용. true면 Play 시드 직후 해당 액체 셀을 flow dirty로 넣는다.
        /// floorFaces·구 JSON 누락 시 false(정지 수면).
        /// </summary>
        public bool simulateFlow;
    }
}
