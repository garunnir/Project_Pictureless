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

    /// <summary>껍데기 + Nested 내용물(재귀). 가방도 아이템 중량 SSOT.</summary>
    public float TotalWeight
    {
        get
        {
            float total = Item.Weight * Count;
            if (Nested != null)
                total += Nested.GetTotalWeight();
            return total;
        }
    }

    /// <summary>외형만 — 내용물 부피는 가방 안에 있어 부모 용량에 합산하지 않음.</summary>
    public float TotalVolume => Item.Volume * Count;

    public void SetCount(int count)
    {
        if (count < 1)
            throw new ArgumentOutOfRangeException(nameof(count), "Count must be >= 1.");

        if (count > Item.MaxStack)
            count = Item.MaxStack;

        Count = count;
    }

    public bool CanHaveNested => ArmorStorageNested.CanHaveNested(Item);

    public bool TryEnsureNested(IContainerCapacityPolicy nestedPolicy)
    {
        if (Nested != null)
            return true;

        if (Item.is_container && !string.IsNullOrEmpty(Item.container_id))
        {
            ContainerData containerDef = GameplayData.GetContainer(Item.container_id);
            if (containerDef == null)
                return false;

            Nested = InventoryContainer.Create(
                containerDef,
                nestedPolicy ?? new FixedContainerCapacityPolicy());
            return true;
        }

        return ArmorStorageNested.TryEnsure(this, nestedPolicy);
    }

    internal void AssignNested(InventoryContainer nested) => Nested = nested;

    public void ClearNested()
    {
        Nested = null;
    }
}
