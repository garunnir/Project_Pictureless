// ============================================================
// PlayerStatusBodyGraphicDisplay — 창·HUD 피격도 부위 색 SSOT
// ============================================================

using System.Collections.Generic;
using Garunnir.Runtime.Gameplay.Data;
using UnityEngine;

public static class PlayerStatusBodyGraphicDisplay
{
    public static void Apply(
        IReadOnlyList<UIPlayerStatusBodyPartGraphic> graphics,
        ICharacterBody body,
        CharacterWindowTab tab)
    {
        if (graphics == null)
            return;

        EquipmentWearState wear = PlayerGearHost.Active?.Wear;
        for (int i = 0; i < graphics.Count; i++)
        {
            UIPlayerStatusBodyPartGraphic graphic = graphics[i];
            if (graphic == null)
                continue;

            string partId = graphic.PartId;
            if (string.IsNullOrEmpty(partId))
                continue;

            bool present = body != null && body.Has(partId);
            if (tab == CharacterWindowTab.Encumbrance)
            {
                int enc = WearStatsAggregator.EncumbranceForPart(wear, partId);
                graphic.SetDisplay(enc, Mathf.Max(enc, 1), present);
                continue;
            }

            if (tab == CharacterWindowTab.BodyTemp)
            {
                int warm = WearStatsAggregator.WarmthForPart(wear, partId);
                graphic.SetDisplay(warm, Mathf.Max(warm, 1), present);
                continue;
            }

            int cur = present ? body.GetConditionCur(partId) : 0;
            int max = present ? body.GetConditionMax(partId) : 0;
            graphic.SetDisplay(cur, max, present);
        }
    }
}
