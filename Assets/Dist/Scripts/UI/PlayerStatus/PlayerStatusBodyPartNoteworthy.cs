// ============================================================
// PlayerStatusBodyPartNoteworthy — Status 부위 호버에 띄울 이상 여부 SSOT
// ============================================================

using System.Collections.Generic;
using Garunnir.Runtime.Gameplay.Data;

public static class PlayerStatusBodyPartNoteworthy
{
    public static bool HasUnder(ICharacterBody body, string partId)
    {
        if (body == null || string.IsNullOrEmpty(partId))
            return false;

        if (!body.Has(partId))
            return true;

        if (!body.TryGet(partId, out BodyPartNode node))
            return false;

        return SubtreeHas(body, node);
    }

    public static bool IsSelf(BodyPartNode node)
    {
        if (node == null)
            return false;

        if (node.Kind == BodyPartKind.Prosthetic)
            return true;

        if (node.HasCondition && node.ConditionCur < node.ConditionMax)
            return true;

        return node.Effects.Count > 0;
    }

    public static void CollectMissingExpectedChildren(
        ICharacterBody body,
        string parentId,
        List<string> dest)
    {
        if (body == null || dest == null || string.IsNullOrEmpty(parentId))
            return;

        string[] severable = BodyPartIds.SeverableParts;
        for (int i = 0; i < severable.Length; i++)
        {
            string id = severable[i];
            if (BodyPartIds.GetSocketParentId(id) != parentId)
                continue;
            if (!body.Has(id))
                dest.Add(id);
        }

        string[] organs = BodyPartIds.VitalOrgans;
        for (int i = 0; i < organs.Length; i++)
        {
            string id = organs[i];
            if (BodyPartIds.GetOrganParentId(id) != parentId)
                continue;
            if (!body.Has(id))
                dest.Add(id);
        }
    }

    static readonly List<string> MissingScratch = new(8);

    static bool SubtreeHas(ICharacterBody body, BodyPartNode node)
    {
        if (IsSelf(node))
            return true;

        MissingScratch.Clear();
        CollectMissingExpectedChildren(body, node.PartId, MissingScratch);
        if (MissingScratch.Count > 0)
            return true;

        IReadOnlyList<BodyPartNode> children = node.Children;
        for (int i = 0; i < children.Count; i++)
        {
            if (SubtreeHas(body, children[i]))
                return true;
        }

        return false;
    }
}
