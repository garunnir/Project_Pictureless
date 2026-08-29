// ============================================================
// DiveTankService — DIVE_TANK use_action / dive_tank 아이템 판정
// ============================================================

using System;
using Garunnir.Runtime.Gameplay.Data;
using IsoTilemap;

public static class DiveTankService
{
    public static bool IsDiveTankItem(ItemData item)
    {
        if (item == null)
            return false;

        if (!string.IsNullOrEmpty(item.id)
            && item.id.Equals(MapSwimConsts.DiveTankItemId, StringComparison.OrdinalIgnoreCase))
            return true;

        UseActionData action = item.use_action;
        return action != null
            && !string.IsNullOrEmpty(action.type)
            && action.type.Equals(
                MapSwimConsts.DiveTankUseActionType,
                StringComparison.OrdinalIgnoreCase);
    }
}
