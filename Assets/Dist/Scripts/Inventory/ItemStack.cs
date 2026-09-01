// ============================================================
// ItemStack — 아이템 + 수량 + (가방일 때) nested 컨테이너
// ============================================================

using System;
using Garunnir.Runtime.Gameplay.Data;

public sealed class ItemStack
{
    public ItemInstance Instance { get; }
    public ItemData Item => Instance.Item;
    public string ItemId => Item?.id;
    public int Count { get; private set; }
    public InventoryContainer Nested { get; private set; }
    /// <summary>총에 끼운 탄창. Nested 가방이 아님.</summary>
    public ItemStack LoadedMagazine { get; private set; }
    public int DamageLevel => Instance.DamageLevel;

    public ItemStack(ItemData item, int count)
        : this(item, count, 0)
    {
    }

    public ItemStack(ItemData item, int count, int damageLevel)
    {
        Instance = new ItemInstance(item, damageLevel);
        SetCount(count);
    }

    public ItemStack(ItemInstance instance, int count)
    {
        Instance = instance ?? throw new ArgumentNullException(nameof(instance));
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
            if (LoadedMagazine != null)
                total += LoadedMagazine.TotalWeight;
            return total;
        }
    }

    /// <summary>외형만 — 내용물 부피는 가방 안에 있어 부모 용량에 합산하지 않음.</summary>
    public float TotalVolume => Item.Volume * Count;

    public void SetCount(int count)
    {
        if (count < 1)
            throw new ArgumentOutOfRangeException(nameof(count), "Count must be >= 1.");

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

    public bool TryAttachMagazine(ItemStack magazine)
    {
        if (magazine?.Item?.magazine == null || LoadedMagazine != null)
            return false;
        LoadedMagazine = magazine;
        return true;
    }

    public ItemStack DetachMagazine()
    {
        ItemStack removed = LoadedMagazine;
        LoadedMagazine = null;
        return removed;
    }

    internal void AssignNested(InventoryContainer nested) => Nested = nested;

    public void ClearNested()
    {
        Nested = null;
    }
}
