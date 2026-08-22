// ============================================================
// BodyPartRestoreService — 절단 복원 + MED heal condition
// ============================================================

namespace Garunnir.Runtime.Gameplay.Data
{
    /// <summary>
    /// 절단 복원은 <see cref="ICharacterBody.TryAttach"/>, heal은 <see cref="ICharacterBody.SetCondition"/>.
    /// </summary>
    /// <remarks>
    /// flowchart LR
    ///   Host[CharacterBodyHost debug] --> Restore[BodyPartRestoreService]
    ///   Restore --> Factory[CharacterBody.TryCreateLimbFrom]
    ///   Factory --> Attach[ICharacterBody.TryAttach]
    ///   Damage[BodyDamageService HP0] --> Remove[RemovePart]
    ///   Remove --> Socket[parent remains]
    ///   Socket --> Attach
    /// </remarks>
    public static class BodyPartRestoreService
    {
        public static bool TryRegenerate(ICharacterBody body, string partId) =>
            TryAttachLimb(body, partId, BodyPartKind.Organic, addRegenerating: true);

        public static bool TryAttachProsthetic(ICharacterBody body, string partId) =>
            TryAttachLimb(body, partId, BodyPartKind.Prosthetic, addRegenerating: false);

        /// <summary>MED heal consume: restore condition on an existing part (chest default).</summary>
        public static bool TryHeal(ICharacterBody body, string partId, int amount)
        {
            if (body == null || amount <= 0 || string.IsNullOrEmpty(partId))
                return false;

            string main = BodyPartIds.GetMainConditionPart(partId) ?? partId;
            if (string.IsNullOrEmpty(main) || !body.Has(main))
                return false;

            int cur = body.GetConditionCur(main);
            int max = body.GetConditionMax(main);
            if (max <= 0 || cur >= max)
                return false;

            int next = cur + amount;
            if (next > max)
                next = max;

            body.SetCondition(main, next, max);
            return true;
        }

        static bool TryAttachLimb(
            ICharacterBody body,
            string partId,
            BodyPartKind kind,
            bool addRegenerating)
        {
            if (body == null || string.IsNullOrEmpty(partId))
                return false;

            string startId = BodyPartIds.ResolveNodeId(partId);
            if (!BodyPartIds.IsSeverable(startId) || body.Has(startId))
                return false;

            string parentId = BodyPartIds.GetSocketParentId(startId);
            if (!string.IsNullOrEmpty(parentId) && !body.Has(parentId))
                return false;

            int conditionMax = ResolveConditionMax(body);
            if (!CharacterBody.TryCreateLimbFrom(startId, conditionMax, kind, out BodyPartNode subtree)
                || subtree == null)
                return false;

            if (!body.TryAttach(parentId, subtree))
                return false;

            if (addRegenerating)
                body.AddEffect(startId, new BodyPartEffect(BodyPartEffectIds.Regenerating, 1, -1f));

            return true;
        }

        static int ResolveConditionMax(ICharacterBody body)
        {
            int max = body.GetConditionMax(BodyPartIds.Chest);
            if (max > 0)
                return max;

            max = body.GetConditionMax(BodyPartIds.Head);
            if (max > 0)
                return max;

            return CharacterBody.BaseCondition;
        }
    }
}
