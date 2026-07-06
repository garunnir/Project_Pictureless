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
