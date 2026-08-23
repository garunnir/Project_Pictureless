// ============================================================
// BloodStampSaveData — 맵 JSON 혈흔 스탬프 1건
// ============================================================

using System;

namespace IsoTilemap
{
    [Serializable]
    public class BloodStampSaveData
    {
        public float wx;
        public float wy;
        public float wz;
        public float yaw;
        public float scale;
        public float alpha;
        public int cx;
        public int cy;
        public int cz;
    }
}
