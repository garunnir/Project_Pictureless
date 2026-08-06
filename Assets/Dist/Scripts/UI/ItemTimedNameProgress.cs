// ============================================================
// ItemTimedNameProgress — 딜레이 활성 스택 → 이름 바 조회 SSOT
// ============================================================

/// <summary>
/// 우선순위: InventoryTimedMove → Gear Timed → idle(내구도).
/// </summary>
public static class ItemTimedNameProgress
{
    public static bool TryGetProgress(ItemStack stack, out float progress01)
    {
        progress01 = 0f;
        if (stack == null)
            return false;

        InventoryTimedMoveHost move = InventoryTimedMoveHost.Active;
        if (move != null && move.IsStackActive(stack))
        {
            progress01 = move.Progress01;
            return true;
        }

        CharacterGearService gear = PlayerGearHost.Active?.Service;
        if (gear != null && gear.IsStackActive(stack))
        {
            progress01 = gear.Timed.Progress01;
            return true;
        }

        return false;
    }

    public static void Apply(ItemNameStatusBar bar, ItemStack stack)
    {
        if (bar == null)
            return;

        if (TryGetProgress(stack, out float progress01))
            bar.SetProgress01(progress01);
        else
            bar.SetDurability(stack);
    }
}
