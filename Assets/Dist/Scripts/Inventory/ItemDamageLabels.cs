// ============================================================
// ItemDamageLabels — ItemStack.DamageLevel UI 표기 SSOT
// ============================================================

public static class ItemDamageLabels
{
    // BN damage_level 0~4 근사 형용사 (0 = 무손상, Loc 키 없음)
    const string KeyPrefix = "ItemDamage.";

    static readonly string[] Fallbacks =
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

        int index = damageLevel < Fallbacks.Length ? damageLevel : Fallbacks.Length - 1;
        string fallback = Fallbacks[index];
        if (string.IsNullOrEmpty(fallback))
            return baseName;

        string label = Loc.Get(KeyPrefix + index, fallback);
        return string.IsNullOrEmpty(label) ? baseName : $"{baseName} ({label})";
    }
}
