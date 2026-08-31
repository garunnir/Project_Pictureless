// ============================================================
// InventoryDragState — 활성 아이템·컨테이너 탭 드래그 페이로드 (창 간 DnD SSOT)
// ============================================================

using System;
using System.Collections.Generic;

public enum InventoryDragKind
{
    Item,
    ContainerTab,
    /// <summary>고정 컨테이너 탭: 컨테이너 제외 내용물 전체 (순차 이동).</summary>
    ContainerContents,
    /// <summary>합산 탭 표시 그룹: 물리 스택별 실제 owner에서 꺼내기.</summary>
    AggregateDisplayGroup,
}

public sealed class InventoryDragPayload
{
    public InventoryDragKind Kind { get; internal set; } = InventoryDragKind.Item;
    public InventoryContainer SourceContainer { get; internal set; }
    public Action ClearSelection { get; internal set; }
    public IReadOnlyList<ItemStack> Stacks { get; internal set; }
    public IReadOnlyList<(InventoryContainer owner, ItemStack stack)> Sources { get; internal set; }
}

public static class InventoryDragState
{
    static InventoryDragPayload _active;
    static bool _consumed;

    public static bool IsDragging => _active != null;
    public static bool WasConsumed => _consumed;

    public static bool TryGetActive(out InventoryDragPayload payload)
    {
        payload = _active;
        return payload != null;
    }

    public static void Begin(
        InventoryContainer sourceContainer,
        IReadOnlyList<ItemStack> stacks,
        Action clearSelection = null)
    {
        if (sourceContainer == null || stacks == null || stacks.Count == 0)
            return;

        var snapshot = new List<ItemStack>(stacks.Count);
        for (int i = 0; i < stacks.Count; i++)
        {
            if (stacks[i] != null)
                snapshot.Add(stacks[i]);
        }

        if (snapshot.Count == 0)
            return;

        _consumed = false;
        _active = new InventoryDragPayload
        {
            Kind = InventoryDragKind.Item,
            SourceContainer = sourceContainer,
            ClearSelection = clearSelection,
            Stacks = snapshot,
        };
    }

    public static void BeginContainerTab(InventoryContainer parentContainer, ItemStack containerStack)
    {
        if (parentContainer == null || containerStack?.Item == null)
            return;

        _consumed = false;
        _active = new InventoryDragPayload
        {
            Kind = InventoryDragKind.ContainerTab,
            SourceContainer = parentContainer,
            ClearSelection = null,
            Stacks = new[] { containerStack },
        };
    }

    public static void BeginAggregateDisplayGroup(
        InventoryContainer displayContainer,
        IReadOnlyList<(InventoryContainer owner, ItemStack stack)> sources,
        Action clearSelection = null)
    {
        if (displayContainer == null || sources == null || sources.Count == 0)
            return;

        var snapshot = new List<(InventoryContainer owner, ItemStack stack)>(sources.Count);
        var ghostStacks = new List<ItemStack>();
        for (int i = 0; i < sources.Count; i++)
        {
            (InventoryContainer owner, ItemStack stack) = sources[i];
            if (owner == null || stack == null || LootAggregateHost.IsAggregateContainer(owner))
                continue;

            snapshot.Add((owner, stack));
            ghostStacks.Add(stack);
        }

        if (snapshot.Count == 0)
            return;

        _consumed = false;
        _active = new InventoryDragPayload
        {
            Kind = InventoryDragKind.AggregateDisplayGroup,
            SourceContainer = displayContainer,
            ClearSelection = clearSelection,
            Stacks = ghostStacks,
            Sources = snapshot,
        };
    }

    public static void BeginContainerContents(InventoryContainer sourceContainer)
    {
        if (sourceContainer?.Stacks == null || sourceContainer.Stacks.Count == 0)
            return;

        var snapshot = new List<ItemStack>(sourceContainer.Stacks.Count);
        for (int i = 0; i < sourceContainer.Stacks.Count; i++)
        {
            ItemStack stack = sourceContainer.Stacks[i];
            if (stack != null)
                snapshot.Add(stack);
        }

        if (snapshot.Count == 0)
            return;

        _consumed = false;
        _active = new InventoryDragPayload
        {
            Kind = InventoryDragKind.ContainerContents,
            SourceContainer = sourceContainer,
            ClearSelection = null,
            Stacks = snapshot,
        };
    }

    public static void MarkConsumed() => _consumed = true;

    public static void End()
    {
        _active = null;
        _consumed = false;
    }

    // End()는 UIInventoryController.FinalizeItemDrag / CleanupIfNoWindowsOpen 에서만 호출한다.
}
