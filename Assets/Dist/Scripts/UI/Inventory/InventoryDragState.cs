// ============================================================
// InventoryDragState — 활성 아이템·컨테이너 탭 드래그 페이로드
// ============================================================

using System.Collections.Generic;

public enum InventoryDragKind
{
    Item,
    ContainerTab,
}

public sealed class InventoryDragPayload
{
    public InventoryDragKind Kind { get; internal set; } = InventoryDragKind.Item;
    public InventoryContainer SourceContainer { get; internal set; }
    public InventoryListSelection SourceSelection { get; internal set; }
    public IReadOnlyList<ItemStack> Stacks { get; internal set; }
}

public static class InventoryDragState
{
    static InventoryDragPayload _active;

    public static bool IsDragging => _active != null;

    public static bool TryGetActive(out InventoryDragPayload payload)
    {
        payload = _active;
        return payload != null;
    }

    public static void Begin(
        InventoryContainer sourceContainer,
        InventoryListSelection sourceSelection,
        IReadOnlyList<ItemStack> stacks)
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

        _active = new InventoryDragPayload
        {
            Kind = InventoryDragKind.Item,
            SourceContainer = sourceContainer,
            SourceSelection = sourceSelection,
            Stacks = snapshot,
        };
    }

    public static void BeginContainerTab(InventoryContainer parentContainer, ItemStack containerStack)
    {
        if (parentContainer == null || containerStack?.Item == null)
            return;

        _active = new InventoryDragPayload
        {
            Kind = InventoryDragKind.ContainerTab,
            SourceContainer = parentContainer,
            SourceSelection = null,
            Stacks = new[] { containerStack },
        };
    }

    public static void End() => _active = null;

    // End()는 UIInventoryController.FinalizeItemDrag / CleanupIfNoWindowsOpen 에서만 호출한다.
}
