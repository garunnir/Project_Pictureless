// ============================================================
// ItemDamageLabels — ItemStack.DamageLevel UI 표기 SSOT
// ============================================================

public static class ItemDamageLabels
{
    // BN damage_level 0~4 근사 형용사
    static readonly string[] Labels =
    {
        null,
        "손상됨",
        "크게 손상됨",
        "심하게 손상됨",
        "거의 파괴됨",
    };

    public static string FormatName(string baseName, int damageLevel)
    {
        if (string.IsNullOrEmpty(baseName) || damageLevel <= 0)
            return baseName;

        int index = damageLevel < Labels.Length ? damageLevel : Labels.Length - 1;
        string label = Labels[index];
        return string.IsNullOrEmpty(label) ? baseName : $"{baseName} ({label})";
    }
}
