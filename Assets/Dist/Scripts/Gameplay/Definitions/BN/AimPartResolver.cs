// ============================================================
// AimPartResolver — 선호 부위 → 상대 Body 실존 부위 해석
// ============================================================

using UnityEngine;

namespace Garunnir.Runtime.Gameplay.Data
{
    public static class AimPartResolver
    {
        static readonly string[] NeighborScratch = new string[8];

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

        /// <summary>인접 실존 부위. 없으면 fromMain 유지. 항상 피해 부위.</summary>
        public static string ScatterToNeighbor(ICharacterBody target, string fromMain)
        {
            if (target == null || string.IsNullOrEmpty(fromMain))
                return fromMain;

            int adjCount = BodyPartIds.WriteAdjacentMains(fromMain, NeighborScratch);
            int usable = 0;
            for (int i = 0; i < adjCount; i++)
            {
                string candidate = NeighborScratch[i];
                if (!IsUsableMain(target, candidate))
                    continue;
                NeighborScratch[usable++] = candidate;
            }

            if (usable <= 0)
                return fromMain;

            int pick = UnityEngine.Random.Range(0, usable);
            return NeighborScratch[pick];
        }
    }
}
