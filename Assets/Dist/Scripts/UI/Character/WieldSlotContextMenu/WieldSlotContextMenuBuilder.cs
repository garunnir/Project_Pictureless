// ============================================================
// WieldSlotContextMenuBuilder — Catalog → Model / Host.TryShow
// ============================================================

using System;
using System.Collections.Generic;
using UnityEngine;

public static class WieldSlotContextMenuBuilder
{
    public static ContextMenuModel Build(WieldSlotContextRequest request)
    {
        var roots = new List<ContextMenuEntry>();
        if (request?.Gear == null || string.IsNullOrEmpty(request.ItemId))
            return new ContextMenuModel(roots);

        IReadOnlyList<IWieldSlotContextMenuContributor> contributors = WieldSlotContextMenuCatalog.All;
        for (int i = 0; i < contributors.Count; i++)
            contributors[i]?.Contribute(request, roots);

        return new ContextMenuModel(roots);
    }

    public static bool TryShow(
        CharacterGearService gear,
        string itemId,
        WieldSlotId slot,
        Vector2 screenPosition,
        Action onChanged)
    {
        ContextMenuModel model = Build(new WieldSlotContextRequest
        {
            Gear = gear,
            ItemId = itemId,
            Slot = slot,
            OnChanged = onChanged,
        });
        if (model.IsEmpty)
            return false;

        return UIContextMenuHost.TryShow(model, screenPosition);
    }
}
