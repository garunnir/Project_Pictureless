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

            switch (BodyPartIds.ResolveNodeId(partId))
            {
                case BodyPartIds.Head:
                case BodyPartIds.Eyes:
                case BodyPartIds.Mouth:
                    return 0.5f;
                case BodyPartIds.Neck:
                    return 0.55f;
                case BodyPartIds.UpperArmL:
                case BodyPartIds.UpperArmR:
                case BodyPartIds.ArmL:
                case BodyPartIds.ArmR:
                    return 0.75f;
                case BodyPartIds.LowerArmL:
                case BodyPartIds.LowerArmR:
                    return 0.72f;
                case BodyPartIds.HandL:
                case BodyPartIds.HandR:
                    return 0.65f;
                case BodyPartIds.ThighL:
                case BodyPartIds.ThighR:
                case BodyPartIds.LegL:
                case BodyPartIds.LegR:
                    return 0.8f;
                case BodyPartIds.CalfL:
                case BodyPartIds.CalfR:
                    return 0.78f;
                case BodyPartIds.FootL:
                case BodyPartIds.FootR:
                    return 0.7f;
                default:
                    return 1f;
            }
        }
    }
}
