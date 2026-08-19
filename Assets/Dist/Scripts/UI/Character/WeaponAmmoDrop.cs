// ============================================================
// WeaponAmmoDrop — 인벤 Item 드래그 → 삽탄·장착/교체 (첫 스택만)
// ============================================================

public static class WeaponAmmoDrop
{
    public static bool TryApplyTo(ItemStack target, InventorySession session)
    {
        if (!TryResolveItemDrag(out ItemStack dragged))
            return false;

        if (!WeaponAmmoService.TryApplyDrop(dragged, target, session))
            return false;

        ConsumeActiveDrag();
        return true;
    }

    static bool TryResolveItemDrag(out ItemStack stack)
    {
        stack = null;

        if (!InventoryDragState.TryGetActive(out InventoryDragPayload payload))
            return false;

        if (payload.Kind != InventoryDragKind.Item)
            return false;

        if (payload.SourceContainer == null ||
            payload.Stacks == null ||
            payload.Stacks.Count == 0)
            return false;

        ItemStack first = payload.Stacks[0];
        if (first?.Item == null)
            return false;

        stack = first;
        return true;
    }

    static void ConsumeActiveDrag()
    {
        if (InventoryDragState.TryGetActive(out InventoryDragPayload payload))
            payload.ClearSelection?.Invoke();

        InventoryDragState.MarkConsumed();
    }
}
