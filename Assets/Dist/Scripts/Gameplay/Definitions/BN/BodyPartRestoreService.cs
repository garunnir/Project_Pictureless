// ============================================================
// BodyPartRestoreService — 절단 복원 + MED heal condition
// ============================================================

namespace Garunnir.Runtime.Gameplay.Data
{
    /// <summary>
    /// 절단 복원은 <see cref="ICharacterBody.TryAttach"/>, heal은 부상 감소 또는 condition.
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

        /// <summary><see cref="TryHeal"/>가 성공할 여지가 있는지. <see cref="BodyHealApply.CanApplyTo"/>와 동일 기준.</summary>
        public static bool CanHeal(ICharacterBody body, string partId, int amount)
        {
            if (body == null || amount <= 0 || string.IsNullOrEmpty(partId))
                return false;

            string main = BodyPartIds.GetMainConditionPart(partId) ?? partId;
            if (string.IsNullOrEmpty(main) || !body.Has(main))
                return false;

            if (BodyInjury.IsOrganicCondition(body, main, out _))
            {
                if (!body.TryGet(main, out BodyPartNode node) || node == null)
                    return false;
                return BodyInjury.SumTissue(node) > 0;
            }

            int cur = body.GetConditionCur(main);
            int max = body.GetConditionMax(main);
            return max > 0 && cur < max;
        }

        /// <summary>MED heal: 유기 부위는 부상 심각도를 줄인다. 의체는 condition.</summary>
        public static bool TryHeal(ICharacterBody body, string partId, int amount)
        {
            if (body == null || amount <= 0 || string.IsNullOrEmpty(partId))
                return false;

            string main = BodyPartIds.GetMainConditionPart(partId) ?? partId;
            if (string.IsNullOrEmpty(main) || !body.Has(main))
                return false;

            if (BodyInjury.IsOrganicCondition(body, main, out _))
            {
                BodyInjury.Reconcile(body, main);
                bool reduced = BodyInjury.ReduceTissue(body, main, amount);
                BodyInjury.SyncPart(body, main);
                return reduced;
            }

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
