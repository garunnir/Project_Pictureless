// ============================================================
// InventorySession — 사이드바 등록·스택 이동 단일 진실원
// ============================================================

using System;
using System.Collections.Generic;
using Garunnir.Runtime.Gameplay.Item;

public sealed class InventorySession
{
    readonly List<InventoryContainer> _sidebarContainers = new();
    readonly Dictionary<string, InventoryContainer> _sidebarById = new();
    readonly HashSet<string> _managedNestedIds = new();
    readonly FixedContainerCapacityPolicy _nestedContainerPolicy = new();

    public event Action SidebarChanged;

    public IReadOnlyList<InventoryContainer> GetSidebarContainers() => _sidebarContainers;

    public bool TryAddSidebarContainer(InventoryContainer container)
    {
        if (container == null)
            return false;

        if (_sidebarById.ContainsKey(container.InstanceId))
            return false;

        _sidebarContainers.Add(container);
        _sidebarById[container.InstanceId] = container;
        NotifySidebarChanged();
        return true;
    }

    public bool TryRemoveSidebarContainer(string instanceId)
    {
        if (string.IsNullOrEmpty(instanceId))
            return false;

        if (!_sidebarById.TryGetValue(instanceId, out InventoryContainer container))
            return false;

        _sidebarById.Remove(instanceId);
        _sidebarContainers.Remove(container);
        _managedNestedIds.Remove(instanceId);
        NotifySidebarChanged();
        return true;
    }

    public void RefreshNestedContainers()
    {
        var desiredNestedIds = new HashSet<string>();

        for (int i = 0; i < _sidebarContainers.Count; i++)
            RegisterNestedFromContainer(_sidebarContainers[i], desiredNestedIds);

        var staleNestedIds = new List<string>();
        foreach (string nestedId in _managedNestedIds)
        {
            if (!desiredNestedIds.Contains(nestedId))
                staleNestedIds.Add(nestedId);
        }

        for (int i = 0; i < staleNestedIds.Count; i++)
            TryRemoveSidebarContainer(staleNestedIds[i]);
    }

    public bool MoveStacks(
        InventoryContainer from,
        InventoryContainer to,
        IReadOnlyList<ItemStack> stacks)
    {
        if (from == null || to == null || stacks == null || stacks.Count == 0)
            return false;

        if (from == to)
            return false;

        var pending = new List<ItemStack>(stacks.Count);
        for (int i = 0; i < stacks.Count; i++)
        {
            ItemStack stack = stacks[i];
            if (stack == null || !from.MutableStacks.Contains(stack))
                return false;

            if (!to.CapacityPolicy.CanAccept(to, stack))
                return false;

            pending.Add(stack);
        }

        for (int i = 0; i < pending.Count; i++)
            TransferStack(from, to, pending[i]);

        RefreshNestedContainers();
        return true;
    }

    void NotifySidebarChanged() => SidebarChanged?.Invoke();

    static void TransferStack(InventoryContainer from, InventoryContainer to, ItemStack stack)
    {
        from.MutableStacks.Remove(stack);

        for (int i = 0; i < to.MutableStacks.Count; i++)
        {
            ItemStack existing = to.MutableStacks[i];
            if (existing.Item != stack.Item)
                continue;

            int merged = existing.Count + stack.Count;
            if (merged <= stack.Item.MaxStack)
            {
                existing.SetCount(merged);
                return;
            }
        }

        to.MutableStacks.Add(stack);
    }

    void RegisterNestedFromContainer(InventoryContainer container, HashSet<string> desiredNestedIds)
    {
        for (int i = 0; i < container.Stacks.Count; i++)
        {
            ItemStack stack = container.Stacks[i];
            if (!stack.Item.IsContainer)
                continue;

            if (!stack.TryEnsureNested(_nestedContainerPolicy))
                continue;

            desiredNestedIds.Add(stack.Nested.InstanceId);
            TryAddSidebarContainer(stack.Nested);
            _managedNestedIds.Add(stack.Nested.InstanceId);
        }
    }
}
