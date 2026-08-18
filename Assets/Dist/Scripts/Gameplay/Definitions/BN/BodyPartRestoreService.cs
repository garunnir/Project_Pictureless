// ============================================================
// BodyPartRestoreService — 절단 소켓에 Organic regen / Prosthetic 부착
// ============================================================

namespace Garunnir.Runtime.Gameplay.Data
{
    /// <summary>
    /// 런타임 복원은 <see cref="ICharacterBody.TryAttach"/>만 쓴다.
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
