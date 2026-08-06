// ============================================================
// WearCombatDefense — Wear coverage/thickness/resist 피해 완화 + WearEnc 명중 배율
// ============================================================

using Garunnir.Runtime.Gameplay.Data;
using UnityEngine;

/// <summary>
/// Phase D combat consume SSOT. Coverage/thickness from WearStatsAggregator;
/// material resist from ItemData.materials when present. Docs: docs/equipment/GEAR.md
/// </summary>
public static class WearCombatDefense
{
    /// <summary>coverage 정수 → 확률 (BN %).</summary>
    public const float CoveragePercentScale = 100f;

    /// <summary>ArmorEngage 시 thickness 1당 흡수량.</summary>
    public const float ThicknessAbsorbPerUnit = 1f;

    /// <summary>ArmorEngage 시 material resist 1당 흡수량 (데이터 있을 때만).</summary>
    public const float MaterialResistAbsorbPerUnit = 1f;

    /// <summary>착용 enc 1포인트당 공격자 HitChance 감소.</summary>
    public const float WearEncHitPenaltyPerPoint = 0.01f;

    /// <summary>WearEncAccuracyFactor 최대 감소량 (0.35 = 최대 −35%).</summary>
    public const float WearEncHitPenaltyCap = 0.35f;

    /// <summary>
    /// WearEncAccuracyFactor = 1 − min(enc × WearEncHitPenaltyPerPoint, WearEncHitPenaltyCap).
    /// </summary>
    public static float WearEncAccuracyFactor(int totalWearEncumbrance)
    {
        if (totalWearEncumbrance <= 0)
            return 1f;

        float penalty = totalWearEncumbrance * WearEncHitPenaltyPerPoint;
        if (penalty > WearEncHitPenaltyCap)
            penalty = WearEncHitPenaltyCap;
        return Mathf.Clamp01(1f - penalty);
    }

    /// <summary>
    /// 명중 후 부위 방어. 부위 비면 torso.
    /// ArmorEngageChance = coverage/100; 미관여면 raw.
    /// ArmorAbsorb = thickness×ThicknessAbsorbPerUnit + resist×MaterialResistAbsorbPerUnit.
    /// MitigatedDamage = engage ? max(0, raw − ArmorAbsorb) : raw.
    /// </summary>
    public static int MitigateDamage(
        EquipmentWearState wear,
        string aimedPartId,
        int rawDamage,
        WeaponAction action)
    {
        if (rawDamage <= 0 || wear == null)
            return Mathf.Max(0, rawDamage);

        string partId = string.IsNullOrEmpty(aimedPartId)
            ? BodyPartIds.Torso
            : aimedPartId;

        WearStatsAggregator.WearPartArmorStats stats =
            WearStatsAggregator.ForPart(wear, partId);

        if (stats.Coverage <= 0 && stats.MaterialThickness <= 0)
            return rawDamage;

        float engageChance = ArmorEngageChance(stats.Coverage);
        if (engageChance <= 0f || Random.value >= engageChance)
            return rawDamage;

        int resist = MaterialResistForPart(wear, partId, action);
        int absorb = ArmorAbsorb(stats.MaterialThickness, resist);
        return Mathf.Max(0, rawDamage - absorb);
    }

    public static float ArmorEngageChance(int coverage) =>
        Mathf.Clamp01(coverage / CoveragePercentScale);

    public static int ArmorAbsorb(int materialThickness, int materialResist)
    {
        float absorb = Mathf.Max(0, materialThickness) * ThicknessAbsorbPerUnit
            + Mathf.Max(0, materialResist) * MaterialResistAbsorbPerUnit;
        return Mathf.Max(0, Mathf.RoundToInt(absorb));
    }

    /// <summary>
    /// Covering worn pieces의 materials 중 해당 액션 resist 최댓값.
    /// materials/MaterialData 없으면 0.
    /// </summary>
    public static int MaterialResistForPart(
        EquipmentWearState wear,
        string partId,
        WeaponAction action)
    {
        if (wear == null || string.IsNullOrEmpty(partId))
            return 0;

        int best = 0;
        var worn = wear.Worn;
        for (int i = 0; i < worn.Count; i++)
        {
            ItemStack stack = worn[i];
            ItemData item = stack?.Item;
            if (item == null || !GearHandleRules.CoversPart(item, partId))
                continue;

            int piece = MaxMaterialResist(item, action);
            if (piece > best)
                best = piece;
        }

        return best;
    }

    static int MaxMaterialResist(ItemData item, WeaponAction action)
    {
        if (item?.materials == null || item.materials.Count == 0)
            return 0;

        int best = 0;
        for (int i = 0; i < item.materials.Count; i++)
        {
            string materialId = item.materials[i];
            if (string.IsNullOrEmpty(materialId))
                continue;

            MaterialData material = GameplayData.GetMaterial(materialId);
            if (material == null)
                continue;

            int resist = ResistForAction(material, action);
            if (resist > best)
                best = resist;
        }

        return best;
    }

    static int ResistForAction(MaterialData material, WeaponAction action)
    {
        switch (action)
        {
            case WeaponAction.Cutting:
                return material.cut_resist;
            case WeaponAction.Gun:
                return material.bullet_resist;
            default:
                return material.bash_resist;
        }
    }
}
