// ============================================================
// ItemDamageLabels — ItemStack.DamageLevel UI 표기 SSOT
// ============================================================

public static class ItemDamageLabels
{
    // BN damage_level 0~4 근사 형용사 (0 = 무손상, Loc 키 없음)
    const string KeyPrefix = "ItemDamage.";
    const string KeyNameFormat = "ItemDamage.NameFormat";
    const int MaxDamageLevel = 4;

    public static string FormatName(string baseName, int damageLevel)
    {
        if (string.IsNullOrEmpty(baseName) || damageLevel <= 0)
            return baseName;

        int index = damageLevel <= MaxDamageLevel ? damageLevel : MaxDamageLevel;
        string label = Loc.Get(KeyPrefix + index);
        return Loc.Format(KeyNameFormat, baseName, label);
    }
}
