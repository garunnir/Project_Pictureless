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
    /// 덮는 조각마다 coverage 주사. 맞은 조각만 thickness+resist 합.
    /// ArmorPen(ammo.pierce)을 합흡수에서 뺌. 하한 0.
    /// </summary>
    public static int MitigateDamage(
        EquipmentWearState wear,
        string aimedPartId,
        int rawDamage,
        string damageTag,
        int armorPen = 0)
    {
        if (rawDamage <= 0 || wear == null)
            return Mathf.Max(0, rawDamage);

        string partId = string.IsNullOrEmpty(aimedPartId)
            ? BodyPartIds.Torso
            : aimedPartId;

        int absorb = 0;
        bool anyCover = false;
        var worn = wear.Worn;
        for (int i = 0; i < worn.Count; i++)
        {
            ItemStack stack = worn[i];
            ItemData item = stack?.Item;
            if (item == null || !GearHandleRules.CoversPart(item, partId))
                continue;

            ArmorDetailData armor = item.armor;
            if (armor == null)
                continue;

            anyCover = true;
            float engageChance = ArmorEngageChance(armor.coverage);
            if (engageChance <= 0f || Random.value >= engageChance)
                continue;

            int resist = MaxMaterialResist(item, damageTag);
            absorb += ArmorAbsorb(armor.material_thickness, resist);
        }

        if (!anyCover)
            return rawDamage;

        absorb = Mathf.Max(0, absorb - Mathf.Max(0, armorPen));
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
    /// Covering worn pieces의 materials 중 해당 채널 resist 최댓값.
    /// materials/MaterialData 없으면 0.
    /// </summary>
    public static int MaterialResistForPart(
        EquipmentWearState wear,
        string partId,
        string damageTag)
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

            int piece = MaxMaterialResist(item, damageTag);
            if (piece > best)
                best = piece;
        }

        return best;
    }

    static int MaxMaterialResist(ItemData item, string damageTag)
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

            int resist = ResistForTag(material, damageTag);
            if (resist > best)
                best = resist;
        }

        return best;
    }

    static int ResistForTag(MaterialData material, string damageTag)
    {
        if (string.Equals(damageTag, AttackDamageTags.Cut, System.StringComparison.Ordinal))
            return material.cut_resist;
        if (string.Equals(damageTag, AttackDamageTags.Bullet, System.StringComparison.Ordinal))
            return material.bullet_resist;
        return material.bash_resist;
    }
}
