// ============================================================
// MapLogicalFloorCross — 수직 논리 바닥 1스텝 교차 판정 SSOT
// ============================================================
namespace IsoTilemap
{
    /// <summary>
    /// 캐릭터 발·파티클 착지가 공유하는 curr/pred vs surface 교차.
    /// XZ LineCast(<see cref="MapTopologyLineCast"/>)와 별개.
    /// </summary>
    public static class MapLogicalFloorCross
    {
        /// <summary>표면 위 착지 밴드. 파티클 터널 여유와 캐릭터 snap에 공통.</summary>
        public const float Tolerance = 0.12f;

        /// <summary>
        /// 이미 밴드/아래에 있거나, 이번 스텝 예측이 밴드에 도달·관통하면 true.
        /// </summary>
        public static bool StepCrossesOrLands(float currY, float predictedY, float surfaceY)
        {
            float band = surfaceY + Tolerance;
            if (currY <= band)
                return true;

            return predictedY <= band;
        }
    }
}
