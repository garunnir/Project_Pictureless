// ============================================================
// OrganHitResolver — 바깥 부위 피격 → 장기/잔여 부위 분배
// ============================================================

using UnityEngine;

namespace Garunnir.Runtime.Gameplay.Data
{
    /// <summary>방어 mitigate 후 남은 피해가 들어갈 실제 부위.</summary>
    public static class OrganHitResolver
    {
        public const float HeadToBrain = 0.35f;
        public const float ChestToHeart = 0.2f;
        public const float ChestToLungEach = 0.15f;
        public const float BellyToLiver = 0.25f;
        public const float BellyToStomach = 0.2f;
        public const float BellyToKidneyEach = 0.15f;

        public static string Resolve(ICharacterBody body, string aimedMainPartId)
        {
            if (body == null || string.IsNullOrEmpty(aimedMainPartId))
                return aimedMainPartId;

            string main = BodyPartIds.GetMainConditionPart(aimedMainPartId)
                          ?? BodyPartIds.ResolveNodeId(aimedMainPartId);

            if (main == BodyPartIds.Head)
                return PickHead(body);
            if (main == BodyPartIds.Chest)
                return PickChest(body);
            if (main == BodyPartIds.Belly)
                return PickBelly(body);
            return main;
        }

        static string PickHead(ICharacterBody body)
        {
            float roll = Random.value;
            if (roll < HeadToBrain && IsUsable(body, BodyPartIds.Brain))
                return BodyPartIds.Brain;
            return BodyPartIds.Head;
        }

        static string PickChest(ICharacterBody body)
        {
            float roll = Random.value;
            float cursor = 0f;
            cursor += ChestToHeart;
            if (roll < cursor && IsUsable(body, BodyPartIds.Heart))
                return BodyPartIds.Heart;
            cursor += ChestToLungEach;
            if (roll < cursor && IsUsable(body, BodyPartIds.LungL))
                return BodyPartIds.LungL;
            cursor += ChestToLungEach;
            if (roll < cursor && IsUsable(body, BodyPartIds.LungR))
                return BodyPartIds.LungR;
            return BodyPartIds.Chest;
        }

        static string PickBelly(ICharacterBody body)
        {
            float roll = Random.value;
            float cursor = 0f;
            cursor += BellyToLiver;
            if (roll < cursor && IsUsable(body, BodyPartIds.Liver))
                return BodyPartIds.Liver;
            cursor += BellyToStomach;
            if (roll < cursor && IsUsable(body, BodyPartIds.Stomach))
                return BodyPartIds.Stomach;
            cursor += BellyToKidneyEach;
            if (roll < cursor && IsUsable(body, BodyPartIds.KidneyL))
                return BodyPartIds.KidneyL;
            cursor += BellyToKidneyEach;
            if (roll < cursor && IsUsable(body, BodyPartIds.KidneyR))
                return BodyPartIds.KidneyR;
            return BodyPartIds.Belly;
        }

        static bool IsUsable(ICharacterBody body, string partId)
        {
            if (!body.TryGet(partId, out BodyPartNode node) || !node.HasCondition)
                return false;
            return node.ConditionCur > 0 && node.ConditionMax > 0;
        }
    }
}
