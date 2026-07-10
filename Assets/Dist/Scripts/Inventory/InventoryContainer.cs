// ============================================================
// InventoryContainer — 유일한 런타임 컨테이너 타입 (플레이어·상자·가방 동일)
// ============================================================

using System;
using System.Collections.Generic;
using Garunnir.Runtime.Gameplay.Item;

public sealed class InventoryContainer
{
    readonly List<ItemStack> _stacks = new();

    public string InstanceId { get; }
    public ContainerDefinitionSO Definition { get; }
    public IReadOnlyList<ItemStack> Stacks => _stacks;
    public IContainerCapacityPolicy CapacityPolicy { get; }

    InventoryContainer(
        string instanceId,
        ContainerDefinitionSO definition,
        IContainerCapacityPolicy capacityPolicy)
    {
        InstanceId = instanceId ?? throw new ArgumentNullException(nameof(instanceId));
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        CapacityPolicy = capacityPolicy ?? throw new ArgumentNullException(nameof(capacityPolicy));
    }

    public static InventoryContainer Create(
        ContainerDefinitionSO definition,
        IContainerCapacityPolicy capacityPolicy,
        string instanceId = null)
    {
        return new InventoryContainer(
            instanceId ?? Guid.NewGuid().ToString("N"),
            definition,
            capacityPolicy);
    }

    internal List<ItemStack> MutableStacks => _stacks;

    public bool ContainsStackReference(ItemStack stack) =>
        stack != null && _stacks.Contains(stack);

    public bool TryAddStackReference(ItemStack stack)
    {
        if (stack?.Item == null || _stacks.Contains(stack))
            return false;

        _stacks.Add(stack);
        return true;
    }

    public bool TryRemoveStackReference(ItemStack stack)
    {
        if (stack == null)
            return false;

        return _stacks.Remove(stack);
    }

    public void ClearStackReferences() => _stacks.Clear();

    public int AddItem(ItemDefinitionSO item, int count)
    {
        if (item == null || count <= 0)
            return 0;

        int remaining = count;
        for (int i = 0; i < _stacks.Count && remaining > 0; i++)
        {
            ItemStack existing = _stacks[i];
            if (existing.Item != item)
                continue;

            int space = item.MaxStack - existing.Count;
            if (space <= 0)
                continue;

            int merged = Math.Min(space, remaining);
            existing.SetCount(existing.Count + merged);
            remaining -= merged;
        }

        while (remaining > 0)
        {
            int chunk = Math.Min(item.MaxStack, remaining);
            _stacks.Add(new ItemStack(item, chunk));
            remaining -= chunk;
        }

        return count;
    }

    public float GetTotalWeight()
    {
        float total = 0f;
        for (int i = 0; i < _stacks.Count; i++)
            total += _stacks[i].TotalWeight;

        return total;
    }

    public float GetTotalVolume()
    {
        float total = 0f;
        for (int i = 0; i < _stacks.Count; i++)
            total += _stacks[i].TotalVolume;

        return total;
    }
}
