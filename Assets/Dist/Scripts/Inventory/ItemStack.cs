// ============================================================
// ItemStack — 아이템 + 수량 + (가방일 때) nested 컨테이너
// ============================================================

using System;
using Garunnir.Runtime.Gameplay.Data;

public sealed class ItemStack
{
    public ItemData Item { get; }
    public string ItemId => Item?.id;
    public int Count { get; private set; }
    public InventoryContainer Nested { get; private set; }
    public int DamageLevel { get; private set; }

    public ItemStack(ItemData item, int count)
    {
        Item = item ?? throw new ArgumentNullException(nameof(item));
        DamageLevel = 0;
        SetCount(count);
    }

    public ItemStack(ItemData item, int count, int damageLevel)
    {
        Item = item ?? throw new ArgumentNullException(nameof(item));
        DamageLevel = Math.Max(0, damageLevel);
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
        if (!Item.is_container || string.IsNullOrEmpty(Item.container_id))
            return false;

        if (Nested != null)
            return true;

        ContainerData containerDef = GameplayData.GetContainer(Item.container_id);
        if (containerDef == null)
            return false;

        Nested = InventoryContainer.Create(
            containerDef,
            nestedPolicy ?? new FixedContainerCapacityPolicy());
        return true;
    }

    public void ClearNested()
    {
        Nested = null;
    }
}
