// ============================================================
// WearStatsAggregator — 착용 아이템 armor 스탯 합산 (들기와 분리)
// ============================================================

using Garunnir.Runtime.Gameplay.Data;

/// <summary>
/// Wear-only armor aggregation SSOT. Combat consume = WearCombatDefense (Phase D).
/// Env/wetness consume = WearEnvExposure (Phase E). BodyTemp consume = TotalWarmth (Phase F).
/// Coverage per part = max; enc/warmth/max_enc/env/thickness = sum; power_armor = any.
/// </summary>
public static class WearStatsAggregator
{
    public readonly struct WearArmorTotals
    {
        public readonly int TotalEncumbrance;
        public readonly int TotalWarmth;
        public readonly int TotalMaxEncumbrance;
        public readonly int TotalEnvironmentalProtection;
        public readonly int TotalMaterialThickness;
        public readonly int MaxCoverage;
        public readonly bool AnyPowerArmor;

        public WearArmorTotals(
            int totalEncumbrance,
            int totalWarmth,
            int totalMaxEncumbrance,
            int totalEnvironmentalProtection,
            int totalMaterialThickness,
            int maxCoverage,
            bool anyPowerArmor)
        {
            TotalEncumbrance = totalEncumbrance;
            TotalWarmth = totalWarmth;
            TotalMaxEncumbrance = totalMaxEncumbrance;
            TotalEnvironmentalProtection = totalEnvironmentalProtection;
            TotalMaterialThickness = totalMaterialThickness;
            MaxCoverage = maxCoverage;
            AnyPowerArmor = anyPowerArmor;
        }
    }

    public readonly struct WearPartArmorStats
    {
        public readonly int Encumbrance;
        public readonly int Warmth;
        public readonly int Coverage;
        public readonly int MaxEncumbrance;
        public readonly int EnvironmentalProtection;
        public readonly int MaterialThickness;
        public readonly bool PowerArmor;

        public WearPartArmorStats(
            int encumbrance,
            int warmth,
            int coverage,
            int maxEncumbrance,
            int environmentalProtection,
            int materialThickness,
            bool powerArmor)
        {
            Encumbrance = encumbrance;
            Warmth = warmth;
            Coverage = coverage;
            MaxEncumbrance = maxEncumbrance;
            EnvironmentalProtection = environmentalProtection;
            MaterialThickness = materialThickness;
            PowerArmor = powerArmor;
        }
    }

    public static WearArmorTotals Aggregate(EquipmentWearState wear)
    {
        int enc = 0;
        int warm = 0;
        int maxEnc = 0;
        int env = 0;
        int thick = 0;
        int maxCoverage = 0;
        bool power = false;

        if (wear != null)
        {
            var worn = wear.Worn;
            for (int i = 0; i < worn.Count; i++)
            {
                ArmorDetailData armor = worn[i]?.Item?.armor;
                if (armor == null)
                    continue;

                enc += armor.encumbrance;
                warm += armor.warmth;
                maxEnc += armor.max_encumbrance;
                env += armor.environmental_protection;
                thick += armor.material_thickness;
                if (armor.coverage > maxCoverage)
                    maxCoverage = armor.coverage;
                if (armor.power_armor)
                    power = true;
            }
        }

        return new WearArmorTotals(enc, warm, maxEnc, env, thick, maxCoverage, power);
    }

    public static void Aggregate(
        EquipmentWearState wear,
        out int totalEncumbrance,
        out int totalWarmth)
    {
        WearArmorTotals totals = Aggregate(wear);
        totalEncumbrance = totals.TotalEncumbrance;
        totalWarmth = totals.TotalWarmth;
    }

    public static WearPartArmorStats ForPart(EquipmentWearState wear, string partId)
    {
        int enc = 0;
        int warm = 0;
        int coverage = 0;
        int maxEnc = 0;
        int env = 0;
        int thick = 0;
        bool power = false;

        if (wear != null && !string.IsNullOrEmpty(partId))
        {
            var worn = wear.Worn;
            for (int i = 0; i < worn.Count; i++)
            {
                ItemStack stack = worn[i];
                if (!GearHandleRules.CoversPart(stack?.Item, partId))
                    continue;

                ArmorDetailData armor = stack.Item.armor;
                if (armor == null)
                    continue;

                enc += armor.encumbrance;
                warm += armor.warmth;
                maxEnc += armor.max_encumbrance;
                env += armor.environmental_protection;
                thick += armor.material_thickness;
                if (armor.coverage > coverage)
                    coverage = armor.coverage;
                if (armor.power_armor)
                    power = true;
            }
        }

        return new WearPartArmorStats(enc, warm, coverage, maxEnc, env, thick, power);
    }

    public static int EncumbranceForPart(EquipmentWearState wear, string partId) =>
        ForPart(wear, partId).Encumbrance;

    public static int WarmthForPart(EquipmentWearState wear, string partId) =>
        ForPart(wear, partId).Warmth;

    public static int CoverageForPart(EquipmentWearState wear, string partId) =>
        ForPart(wear, partId).Coverage;

    public static int MaxEncumbranceForPart(EquipmentWearState wear, string partId) =>
        ForPart(wear, partId).MaxEncumbrance;

    public static int EnvironmentalProtectionForPart(EquipmentWearState wear, string partId) =>
        ForPart(wear, partId).EnvironmentalProtection;

    public static int MaterialThicknessForPart(EquipmentWearState wear, string partId) =>
        ForPart(wear, partId).MaterialThickness;

    public static bool PowerArmorForPart(EquipmentWearState wear, string partId) =>
        ForPart(wear, partId).PowerArmor;
}
