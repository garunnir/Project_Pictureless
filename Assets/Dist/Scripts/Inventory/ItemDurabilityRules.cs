// ============================================================
// ItemDurabilityRules — ItemData.has_durability UI 표시 SSOT
// ============================================================

using Garunnir.Runtime.Gameplay.Data;

public static class ItemDurabilityRules
{
    public static bool ShouldShowDurability(ItemData item, int damageLevel)
    {
        if (item == null)
            return false;

        if (item.has_durability)
            return true;

        // 구버전 JSON(has_durability 미 bake): 손상된 스택만 표시
        return damageLevel > 0;
    }
}
