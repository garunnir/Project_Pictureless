// ============================================================
// AimPartResolver — 선호 부위 → 상대 Body 실존 부위 해석
// ============================================================

namespace Garunnir.Runtime.Gameplay.Data
{
    public static class AimPartResolver
    {
        public static bool TryResolve(
            ICharacterBody target,
            string preferredPartId,
            out string aimedPartId)
        {
            aimedPartId = null;
            if (target == null)
                return false;

            string preferred = string.IsNullOrEmpty(preferredPartId)
                ? BodyPartIds.Torso
                : preferredPartId;

            string main = BodyPartIds.GetMainConditionPart(preferred) ?? preferred;
            if (IsUsableMain(target, main))
            {
                aimedPartId = main;
                return true;
            }

            if (main == BodyPartIds.ThighL && IsUsableMain(target, BodyPartIds.ThighR))
            {
                aimedPartId = BodyPartIds.ThighR;
                return true;
            }

            if (main == BodyPartIds.ThighR && IsUsableMain(target, BodyPartIds.ThighL))
            {
                aimedPartId = BodyPartIds.ThighL;
                return true;
            }

            if (IsUsableMain(target, BodyPartIds.Chest))
            {
                aimedPartId = BodyPartIds.Chest;
                return true;
            }

            for (int i = 0; i < BodyPartIds.MainConditionParts.Length; i++)
            {
                string candidate = BodyPartIds.MainConditionParts[i];
                if (!IsUsableMain(target, candidate))
                    continue;
                aimedPartId = candidate;
                return true;
            }

            return false;
        }

        static bool IsUsableMain(ICharacterBody target, string mainPartId)
        {
            if (string.IsNullOrEmpty(mainPartId) ||
                !target.TryGet(mainPartId, out BodyPartNode node) ||
                !node.HasCondition)
                return false;
            return node.ConditionCur > 0;
        }
    }
}
