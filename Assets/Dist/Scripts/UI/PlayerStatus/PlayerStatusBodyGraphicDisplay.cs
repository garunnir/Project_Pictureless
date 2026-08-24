// ============================================================
// PlayerStatusBodyGraphicDisplay — 창·HUD 피격도 부위 색·밴디지 SSOT
// ============================================================

using System.Collections.Generic;
using Garunnir.Runtime.Gameplay.Data;
using UnityEngine;

public static class PlayerStatusBodyGraphicDisplay
{
    const int TempComfortDisplayScale = 100;
    static readonly List<BodyPartEffect> EffectScratch = new(16);

    public static void Apply(
        IReadOnlyList<UIPlayerStatusBodyPartGraphic> graphics,
        ICharacterBody body,
        CharacterWindowTab tab)
    {
        if (graphics == null)
            return;

        EquipmentWearState wear = PlayerGearHost.Active?.Wear;
        BodyTemp bodyTemp = PlayerGearHost.Active?.BodyTemperature;
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
            }
            else if (tab == CharacterWindowTab.BodyTemp)
            {
                ApplyBodyTempGraphic(graphic, bodyTemp, partId, present);
            }
            else
            {
                int cur = present ? body.GetConditionCur(partId) : 0;
                int max = present ? body.GetConditionMax(partId) : 0;
                graphic.SetDisplay(cur, max, present);
            }

            graphic.SetBandaged(
                present && BodyHealApply.HasBandagedUnder(body, partId, EffectScratch),
                present ? BodyHealApply.BandageDirty01Under(body, partId, EffectScratch) : 0f);
        }
    }

    static void ApplyBodyTempGraphic(
        UIPlayerStatusBodyPartGraphic graphic,
        BodyTemp bodyTemp,
        string partId,
        bool present)
    {
        if (!present)
        {
            graphic.SetDisplay(0, TempComfortDisplayScale, false);
            return;
        }

        float tempC = BodyTemp.ComfortBodyTempC;
        if (bodyTemp == null || !bodyTemp.TryGetPartTempC(partId, out tempC))
            tempC = BodyTemp.ComfortBodyTempC;

        float deviation = Mathf.Abs(tempC - BodyTemp.ComfortBodyTempC);
        float maxDev = Mathf.Max(
            BodyTemp.ComfortBodyTempC - BodyTemp.ExtremityTempMinC,
            BodyTemp.ExtremityTempMaxC - BodyTemp.ComfortBodyTempC);
        float closeness = maxDev > 0f
            ? 1f - Mathf.Clamp01(deviation / maxDev)
            : 1f;
        int current = Mathf.RoundToInt(closeness * TempComfortDisplayScale);
        graphic.SetDisplay(current, TempComfortDisplayScale, true);
    }
}
