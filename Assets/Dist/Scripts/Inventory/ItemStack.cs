// ============================================================
// ItemStack — 아이템 + 수량 + (가방일 때) nested 컨테이너
// ============================================================

using System;
using Garunnir.Runtime.Gameplay.Item;

public sealed class ItemStack
{
    public ItemDefinitionSO Item { get; }
    public int Count { get; private set; }
    public InventoryContainer Nested { get; private set; }

    public ItemStack(ItemDefinitionSO item, int count)
    {
        Item = item ?? throw new ArgumentNullException(nameof(item));
        SetCount(count);
    }

    public float TotalWeight => Item.Weight * Count;
    public float TotalVolume => Item.Volume * Count;

    public void SetCount(int count)
    {
        if (count < 1)
            throw new ArgumentOutOfRangeException(nameof(count), "Count must be >= 1.");

        if (count > Item.MaxStack)
            count = Item.MaxStack;

        Count = count;
    }

    public bool TryEnsureNested(IContainerCapacityPolicy nestedPolicy)
    {
        if (!Item.IsContainer || Item.NestedContainerDefinition == null)
            return false;

        if (Nested != null)
            return true;

        Nested = InventoryContainer.Create(
            Item.NestedContainerDefinition,
            nestedPolicy ?? new FixedContainerCapacityPolicy());
        return true;
    }

    public void ClearNested()
    {
        Nested = null;
    }
}
