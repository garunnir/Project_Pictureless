// ============================================================
// BodyPartHitDifficulty — 부위별 명중 난이도 배수 SSOT
// ============================================================

namespace Garunnir.Runtime.Gameplay.Data
{
    public static class BodyPartHitDifficulty
    {
        public static float Get(string partId)
        {
            if (string.IsNullOrEmpty(partId))
                return 1f;

            switch (partId)
            {
                case BodyPartIds.Head:
                case BodyPartIds.Eyes:
                case BodyPartIds.Mouth:
                    return 0.5f;
                case BodyPartIds.ArmL:
                case BodyPartIds.ArmR:
                case BodyPartIds.HandL:
                case BodyPartIds.HandR:
                    return 0.75f;
                case BodyPartIds.LegL:
                case BodyPartIds.LegR:
                case BodyPartIds.FootL:
                case BodyPartIds.FootR:
                    return 0.8f;
                default:
                    return 1f;
            }
        }
    }
}
