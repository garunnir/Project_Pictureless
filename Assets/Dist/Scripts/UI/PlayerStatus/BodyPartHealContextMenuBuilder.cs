// ============================================================
// BodyPartHealContextMenuBuilder — HUD/Status 실루엣 RMB 힐·벗기
// ============================================================

using System.Collections.Generic;
using UnityEngine;

public static class BodyPartHealContextMenuBuilder
{
    public static bool TryShow(string partId, Vector2 screenPosition)
    {
        ContextMenuModel model = Build(partId);
        if (model == null || model.IsEmpty)
            return false;
        return UIContextMenuHost.TryShow(model, screenPosition);
    }

    public static ContextMenuModel Build(string partId)
    {
        var roots = new List<ContextMenuEntry>();
        if (string.IsNullOrEmpty(partId))
            return new ContextMenuModel(roots);

        HealConsumeContextMenuEntries.AppendItemLeavesForPart(partId, roots);
        HealConsumeContextMenuEntries.AppendUnwrapLeaf(partId, roots);
        return new ContextMenuModel(roots);
    }
}
