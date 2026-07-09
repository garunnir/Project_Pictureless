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
    public event Action StacksChanged;

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
        PurgeNestedFromSidebar();

        for (int i = 0; i < _sidebarContainers.Count; i++)
            EnsureNestedStacks(_sidebarContainers[i]);
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
        float pendingWeight = 0f;
        float pendingVolume = 0f;
        float maxWeight = to.CapacityPolicy.GetMaxWeight(to);
        float maxVolume = to.CapacityPolicy.GetMaxVolume(to);

        for (int i = 0; i < stacks.Count; i++)
        {
            ItemStack stack = stacks[i];
            if (stack == null || !from.MutableStacks.Contains(stack))
                return false;

            if (!to.CapacityPolicy.CanAccept(to, stack))
                return false;

            float nextWeight = to.GetTotalWeight() + pendingWeight + stack.TotalWeight;
            float nextVolume = to.GetTotalVolume() + pendingVolume + stack.TotalVolume;
            if (nextWeight > maxWeight + 0.0001f || nextVolume > maxVolume + 0.0001f)
                return false;

            pending.Add(stack);
            pendingWeight += stack.TotalWeight;
            pendingVolume += stack.TotalVolume;
        }

        for (int i = 0; i < pending.Count; i++)
            TransferStack(from, to, pending[i]);

        RefreshNestedContainers();
        NotifyStacksChanged();
        return true;
    }

    public void NotifyExternalStacksChanged()
    {
        RefreshNestedContainers();
        NotifyStacksChanged();
    }

    public bool TryGetContainerItemStack(
        InventoryContainer nestedContainer,
        out InventoryContainer parentContainer,
        out ItemStack containerStack)
    {
        parentContainer = null;
        containerStack = null;

        if (nestedContainer == null)
            return false;

        for (int i = 0; i < _sidebarContainers.Count; i++)
        {
            InventoryContainer candidate = _sidebarContainers[i];
            if (candidate == null)
                continue;

            for (int s = 0; s < candidate.Stacks.Count; s++)
            {
                ItemStack stack = candidate.Stacks[s];
                if (stack?.Item == null || !stack.Item.IsContainer)
                    continue;

                if (stack.Nested == nestedContainer)
                {
                    parentContainer = candidate;
                    containerStack = stack;
                    return true;
                }
            }
        }

        return false;
    }

    void NotifySidebarChanged() => SidebarChanged?.Invoke();

    void NotifyStacksChanged() => StacksChanged?.Invoke();

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

    void PurgeNestedFromSidebar()
    {
        var nestedIds = new HashSet<string>();
        for (int i = 0; i < _sidebarContainers.Count; i++)
            CollectNestedIds(_sidebarContainers[i], nestedIds);

        var removeIds = new List<string>();
        foreach (string nestedId in nestedIds)
        {
            if (_sidebarById.ContainsKey(nestedId))
                removeIds.Add(nestedId);
        }

        foreach (string legacyId in _managedNestedIds)
        {
            if (_sidebarById.ContainsKey(legacyId) && !removeIds.Contains(legacyId))
                removeIds.Add(legacyId);
        }

        for (int i = 0; i < removeIds.Count; i++)
            TryRemoveSidebarContainer(removeIds[i]);

        _managedNestedIds.Clear();
    }

    static void CollectNestedIds(InventoryContainer container, HashSet<string> nestedIds)
    {
        if (container == null)
            return;

        for (int i = 0; i < container.Stacks.Count; i++)
        {
            ItemStack stack = container.Stacks[i];
            if (stack?.Item == null || !stack.Item.IsContainer)
                continue;

            if (stack.Nested != null)
                nestedIds.Add(stack.Nested.InstanceId);
        }
    }

    void EnsureNestedStacks(InventoryContainer container)
    {
        if (container == null)
            return;

        for (int i = 0; i < container.Stacks.Count; i++)
        {
            ItemStack stack = container.Stacks[i];
            if (stack?.Item == null || !stack.Item.IsContainer)
                continue;

            stack.TryEnsureNested(_nestedContainerPolicy);
        }
    }
}
