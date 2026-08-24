// ============================================================
// PlayerItemAccess — 몸통 인벤 + 들기 슬롯 소유·차감 SSOT
// ============================================================

using System;
using System.Collections.Generic;

public static class PlayerItemAccess
{
    public delegate void StackVisitor(ItemStack stack, InventoryContainer container);

    public static bool OwnsInBodyOrWield(ItemStack stack, InventoryContainer container)
    {
        if (container != null && container.ContainsStackReference(stack))
            return true;

        CharacterGearService gear = PlayerGearHost.Active?.Service;
        return gear != null && gear.Wield.Contains(stack);
    }

    public static int TryTakeOne(ItemStack stack, InventoryContainer container)
    {
        if (container != null && container.ContainsStackReference(stack))
            return container.TryTakeFromStack(stack, 1);

        CharacterGearService gear = PlayerGearHost.Active?.Service;
        return gear != null ? gear.TryTakeFromWielded(stack, 1) : 0;
    }

    public static void VisitBodyAndWield(StackVisitor visitor)
    {
        if (visitor == null)
            return;

        InventoryContainer bodyContainer = PlayerInventoryRuntime.Active?.Host?.Container;
        if (bodyContainer != null)
        {
            IReadOnlyList<ItemStack> stacks = bodyContainer.Stacks;
            for (int i = 0; i < stacks.Count; i++)
                visitor(stacks[i], bodyContainer);
        }

        CharacterGearService gear = PlayerGearHost.Active?.Service;
        if (gear == null)
            return;

        VisitWieldStack(gear.Wield.Left, visitor);
        if (!ReferenceEquals(gear.Wield.Left, gear.Wield.Right))
            VisitWieldStack(gear.Wield.Right, visitor);
    }

    static void VisitWieldStack(ItemStack stack, StackVisitor visitor)
    {
        if (stack?.Item == null || stack.Count < 1)
            return;

        visitor(stack, null);
    }
}
