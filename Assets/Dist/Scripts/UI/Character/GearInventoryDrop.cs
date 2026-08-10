// ============================================================
// GearInventoryDrop — 인벤 Item 드래그 → Wear/Wield 드롭 적용
// ============================================================

public static class GearInventoryDrop
{
    public static bool TryWearFromActiveDrag()
    {
        if (!TryResolveItemDrag(out InventoryContainer source, out ItemStack stack))
            return false;

        if (!GearHandleRules.IsWearable(stack.Item))
            return false;

        CharacterGearService gear = PlayerGearHost.Active?.Service;
        if (gear == null)
            return false;

        if (!gear.TryBeginWear(stack, source))
            return false;

        ConsumeActiveDrag();
        return true;
    }

    public static bool TryWieldFromActiveDrag(WieldSlotId slot)
    {
        if (!TryResolveItemDrag(out InventoryContainer source, out ItemStack stack))
            return false;

        CharacterGearService gear = PlayerGearHost.Active?.Service;
        if (gear == null)
            return false;

        WieldHand hand = GearHandleRules.IsTwoHandWeapon(stack.Item)
            ? WieldHand.TwoHand
            : slot == WieldSlotId.Left
                ? WieldHand.Left
                : WieldHand.Right;

        if (!gear.TryBeginWield(stack, source, hand))
            return false;

        ConsumeActiveDrag();
        return true;
    }

    static bool TryResolveItemDrag(out InventoryContainer source, out ItemStack stack)
    {
        source = null;
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

        source = payload.SourceContainer;
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
