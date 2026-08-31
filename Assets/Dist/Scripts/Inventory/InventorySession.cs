// ============================================================
// InventorySession — 사이드바 등록·스택 이동 단일 진실원
// ============================================================

using System;
using System.Collections.Generic;

public sealed class InventorySession
{
    readonly List<InventoryContainer> _sidebarContainers = new();
    readonly Dictionary<string, InventoryContainer> _sidebarById = new();
    readonly HashSet<string> _managedNestedIds = new();
    readonly FixedContainerCapacityPolicy _nestedContainerPolicy = new();

    readonly List<InventoryContainer> _pendingChangedContainers = new();
    bool _pendingSidebarAffected;

    public event Action SidebarChanged;
    public event Action<InventoryStacksChangeSet> StacksChanged;

    public IReadOnlyList<InventoryContainer> GetSidebarContainers() => _sidebarContainers;

    public bool TryFindOwner(ItemStack stack, out InventoryContainer owner)
    {
        owner = null;
        if (stack == null)
            return false;

        for (int i = 0; i < _sidebarContainers.Count; i++)
        {
            InventoryContainer candidate = _sidebarContainers[i];
            if (candidate == null || !candidate.ContainsStackReference(stack))
                continue;
            owner = candidate;
            return true;
        }

        return false;
    }

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

        for (int i = 0; i < stacks.Count; i++)
        {
            if (!CanPlaceStackInContainer(stacks[i], to))
                return false;
        }

        var pending = new List<ItemStack>(stacks.Count);
        float pendingWeight = 0f;
        float pendingVolume = 0f;
        float maxWeight = to.CapacityPolicy.GetMaxWeight(to);
        float maxVolume = to.CapacityPolicy.GetMaxVolume(to);
        bool hardWeight = to.CapacityPolicy.EnforcesHardWeightLimit;
        bool hardVolume = to.CapacityPolicy.EnforcesHardVolumeLimit;

        for (int i = 0; i < stacks.Count; i++)
        {
            ItemStack stack = stacks[i];
            if (stack == null || !from.MutableStacks.Contains(stack))
                return false;

            if (!to.CapacityPolicy.CanAccept(to, stack))
                return false;

            float nextWeight = to.GetTotalWeight() + pendingWeight + stack.TotalWeight;
            float nextVolume = to.GetTotalVolume() + pendingVolume + stack.TotalVolume;
            if (ExceedsHardCapacity(
                    hardWeight,
                    hardVolume,
                    nextWeight,
                    nextVolume,
                    maxWeight,
                    maxVolume))
                return false;

            pending.Add(stack);
            pendingWeight += stack.TotalWeight;
            pendingVolume += stack.TotalVolume;
        }

        for (int i = 0; i < pending.Count; i++)
            TransferStack(from, to, pending[i]);

        from.NotifyContentsChanged();
        to.NotifyContentsChanged();
        MarkContainerChanged(from);
        MarkContainerChanged(to);
        RefreshNestedContainers();
        MarkSidebarAffected();
        NotifyStacksChanged();
        return true;
    }

    /// <summary>
    /// 스택에서 count개만 옮긴다. Nested·탄창 부착 스택은 분할 불가(전체 이동만).
    /// </summary>
    public bool MoveStackCount(
        InventoryContainer from,
        InventoryContainer to,
        ItemStack stack,
        int count)
    {
        if (from == null || to == null || stack == null || count <= 0 || from == to)
            return false;

        if (!from.MutableStacks.Contains(stack))
            return false;

        if (MustTransferStackWhole(stack))
            return count >= stack.Count && MoveStacks(from, to, new[] { stack });

        int moveCount = count < stack.Count ? count : stack.Count;
        if (moveCount <= 0)
            return false;

        if (!CanPlaceStackInContainer(stack, to))
            return false;

        float maxWeight = to.CapacityPolicy.GetMaxWeight(to);
        float maxVolume = to.CapacityPolicy.GetMaxVolume(to);
        bool hardWeight = to.CapacityPolicy.EnforcesHardWeightLimit;
        bool hardVolume = to.CapacityPolicy.EnforcesHardVolumeLimit;
        float addWeight = stack.Item.Weight * moveCount;
        float addVolume = stack.Item.Volume * moveCount;

        for (int i = 0; i < to.MutableStacks.Count; i++)
        {
            ItemStack existing = to.MutableStacks[i];
            if (!ItemMergePolicy.CanMerge(existing, stack))
                continue;

            float nextWeight = to.GetTotalWeight() + addWeight;
            float nextVolume = to.GetTotalVolume() + addVolume;
            if (ExceedsHardCapacity(
                    hardWeight,
                    hardVolume,
                    nextWeight,
                    nextVolume,
                    maxWeight,
                    maxVolume))
                return false;

            if (from.TryTakeFromStack(stack, moveCount) < moveCount)
                return false;

            existing.SetCount(existing.Count + moveCount);
            FinalizeMove(from, to);
            return true;
        }

        var incoming = new ItemStack(stack.Item, moveCount, stack.DamageLevel);
        CopyMergeInstanceState(stack.Instance, incoming.Instance);

        if (!to.CapacityPolicy.CanAccept(to, incoming))
            return false;

        float nextWeightNew = to.GetTotalWeight() + incoming.TotalWeight;
        float nextVolumeNew = to.GetTotalVolume() + incoming.TotalVolume;
        if (ExceedsHardCapacity(
                hardWeight,
                hardVolume,
                nextWeightNew,
                nextVolumeNew,
                maxWeight,
                maxVolume))
            return false;

        if (from.TryTakeFromStack(stack, moveCount) < moveCount)
            return false;

        to.MutableStacks.Add(incoming);
        FinalizeMove(from, to);
        return true;
    }

    public static bool MustTransferStackWhole(ItemStack stack) =>
        stack == null ||
        stack.Count <= 1 ||
        stack.Nested != null ||
        stack.LoadedMagazine != null;

    void FinalizeMove(InventoryContainer from, InventoryContainer to)
    {
        from.NotifyContentsChanged();
        to.NotifyContentsChanged();
        MarkContainerChanged(from);
        MarkContainerChanged(to);
        RefreshNestedContainers();
        MarkSidebarAffected();
        NotifyStacksChanged();
    }

    static void CopyMergeInstanceState(ItemInstance source, ItemInstance destination)
    {
        if (source == null || destination == null)
            return;

        if (source.IsCooked)
            destination.StampCooked(true);

        if (source.IsHot)
            destination.StampHot(source.HotUntilWorldMinute);

        if (source.IsRotten)
            destination.SetRotten(true);

        if (source.CreatedWorldMinute != ItemInstance.UnsetCreatedWorldMinute)
            destination.SetCreatedWorldMinute(source.CreatedWorldMinute);
    }

    /// <summary>
    /// 스택을 앞에서부터 하나씩 옮긴다. 다음 스택을 넣을 수 없으면 중단하고 이미 옮긴 분은 유지한다.
    /// </summary>
    public int MoveStacksSequentiallyUntilFull(
        InventoryContainer from,
        InventoryContainer to,
        IReadOnlyList<ItemStack> stacks)
    {
        if (from == null || to == null || stacks == null || stacks.Count == 0)
            return 0;

        if (from == to)
            return 0;

        int moved = 0;
        for (int i = 0; i < stacks.Count; i++)
        {
            ItemStack stack = stacks[i];
            if (stack == null || !from.MutableStacks.Contains(stack))
                break;

            if (MustTransferStackWhole(stack))
            {
                if (!MoveStackCount(from, to, stack, stack.Count))
                    break;

                moved++;
                continue;
            }

            int units = stack.Count;
            for (int unit = 0; unit < units; unit++)
            {
                if (!from.MutableStacks.Contains(stack) || stack.Count <= 0)
                    break;

                if (!MoveStackCount(from, to, stack, 1))
                    break;

                moved++;
            }
        }

        return moved;
    }

    public void NotifyExternalStacksChanged(params InventoryContainer[] containers)
    {
        RefreshNestedContainers();

        if (containers == null || containers.Length == 0)
        {
            for (int i = 0; i < _sidebarContainers.Count; i++)
                MarkContainerChanged(_sidebarContainers[i]);
        }
        else
        {
            for (int i = 0; i < containers.Length; i++)
                MarkContainerChanged(containers[i]);
        }

        MarkSidebarAffected();
        NotifyStacksChanged();
    }

    static bool ExceedsHardCapacity(
        bool hardWeight,
        bool hardVolume,
        float nextWeight,
        float nextVolume,
        float maxWeight,
        float maxVolume)
    {
        const float epsilon = FixedContainerCapacityPolicy.Epsilon;
        if (hardWeight && nextWeight > maxWeight + epsilon)
            return true;
        if (hardVolume && nextVolume > maxVolume + epsilon)
            return true;
        return false;
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
                if (stack?.Item == null || !stack.CanHaveNested)
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

    /// <summary>착용 포켓 등 사이드바 유도 레이아웃만 바뀌었을 때 UI Sync용.</summary>
    public void NotifySidebarLayoutChanged() => NotifySidebarChanged();

    void NotifySidebarChanged() => SidebarChanged?.Invoke();

    void MarkContainerChanged(InventoryContainer container)
    {
        if (container == null)
            return;

        for (int i = 0; i < _pendingChangedContainers.Count; i++)
        {
            if (_pendingChangedContainers[i] == container)
                return;
        }

        _pendingChangedContainers.Add(container);
    }

    void MarkSidebarAffected() => _pendingSidebarAffected = true;

    void NotifyStacksChanged()
    {
        if (_pendingChangedContainers.Count == 0 && !_pendingSidebarAffected)
            return;

        InventoryStacksChangeSet changeSet = InventoryStacksChangeSet.Create(
            _pendingChangedContainers,
            _pendingSidebarAffected);

        _pendingChangedContainers.Clear();
        _pendingSidebarAffected = false;

        StacksChanged?.Invoke(changeSet);
    }

    static bool CanPlaceStackInContainer(ItemStack stack, InventoryContainer target)
    {
        if (stack?.Item == null || target == null)
            return false;

        if (!stack.CanHaveNested || stack.Nested == null)
            return true;

        return !IsContainerWithinHierarchy(target, stack.Nested);
    }

    static bool IsContainerWithinHierarchy(InventoryContainer inner, InventoryContainer outer)
    {
        if (inner == null || outer == null)
            return false;

        if (inner == outer)
            return true;

        for (int i = 0; i < outer.Stacks.Count; i++)
        {
            ItemStack stack = outer.Stacks[i];
            if (stack?.Item == null || !stack.CanHaveNested || stack.Nested == null)
                continue;

            if (stack.Nested == inner || IsContainerWithinHierarchy(inner, stack.Nested))
                return true;
        }

        return false;
    }

    static void TransferStack(InventoryContainer from, InventoryContainer to, ItemStack stack)
    {
        from.MutableStacks.Remove(stack);

        for (int i = 0; i < to.MutableStacks.Count; i++)
        {
            ItemStack existing = to.MutableStacks[i];
            if (!ItemMergePolicy.CanMerge(existing, stack))
                continue;

            existing.SetCount(existing.Count + stack.Count);
            return;
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
            if (stack?.Item == null || !stack.CanHaveNested)
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
            if (stack?.Item == null || !stack.CanHaveNested)
                continue;

            stack.TryEnsureNested(_nestedContainerPolicy);
        }
    }
}
