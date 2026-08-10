// ============================================================
// InventoryDragDrop — 활성 드래그 페이로드를 대상 컨테이너로 적용
// ============================================================

using System;
using System.Collections.Generic;

public static class InventoryDragDrop
{
    public static bool TryApplyTo(InventorySession session, InventoryContainer target)
    {
        if (session == null || target == null)
            return false;

        if (!InventoryDragState.TryGetActive(out InventoryDragPayload payload))
            return false;

        if (payload.SourceContainer == null || payload.Stacks == null || payload.Stacks.Count == 0)
            return false;

        // ContainerTab SourceContainer is the parent holding the bag stack — same as the open
        // player-body list target is a valid no-op (MoveStacks from==to). Do not early-out only
        // for Item / ContainerContents where source==target means "drop onto self".
        if (payload.Kind != InventoryDragKind.ContainerTab &&
            payload.SourceContainer == target)
            return false;

        if (payload.Kind == InventoryDragKind.ContainerContents)
        {
            InventoryTimedMoveHost timed = InventoryTimedMoveHost.Active;
            if (timed != null)
            {
                if (timed.IsBusy)
                    return false;
                return timed.TryBeginSequentialUntilFull(
                    session,
                    payload.SourceContainer,
                    target,
                    payload.Stacks);
            }

            return session.MoveStacksSequentiallyUntilFull(
                payload.SourceContainer, target, payload.Stacks) > 0;
        }

        if (payload.Kind == InventoryDragKind.Item)
        {
            return TryMoveItemStacks(
                session,
                payload.SourceContainer,
                target,
                payload.Stacks,
                payload.ClearSelection);
        }

        return TryMoveItemStacks(
            session,
            payload.SourceContainer,
            target,
            payload.Stacks,
            null);
    }

    public static bool TryMoveItemStacks(
        InventorySession session,
        InventoryContainer from,
        InventoryContainer to,
        IReadOnlyList<ItemStack> stacks,
        Action clearSelection = null)
    {
        if (session == null || from == null || to == null || stacks == null || stacks.Count == 0)
            return false;

        if (from == to)
            return false;

        InventoryTimedMoveHost timed = InventoryTimedMoveHost.Active;
        if (timed != null)
        {
            if (timed.IsBusy)
                return false;

            return timed.TryBeginMove(
                session,
                from,
                to,
                stacks,
                () => clearSelection?.Invoke());
        }

        if (!session.MoveStacks(from, to, stacks))
            return false;

        clearSelection?.Invoke();
        return true;
    }

    public static bool TryQuickTransferBetweenWindows(
        InventorySession session,
        UIInventoryListWindow primaryWindow,
        UIInventoryListWindow lootWindow,
        UIItemListView sourceListView,
        ItemStack stack,
        InventoryContainer sourceContainer)
    {
        if (session == null || stack == null || sourceContainer == null || sourceListView == null)
            return false;

        if (primaryWindow == null || lootWindow == null)
            return false;

        if (!primaryWindow.IsVisible || !lootWindow.IsVisible)
            return false;

        UIInventoryListWindow peerWindow;
        if (sourceListView == primaryWindow.ListView)
            peerWindow = lootWindow;
        else if (sourceListView == lootWindow.ListView)
            peerWindow = primaryWindow;
        else
            return false;

        InventoryContainer target = peerWindow.SelectedContainer;
        if (target == null || sourceContainer == target)
            return false;

        InventoryListSelection selection = sourceListView.Selection;
        if (selection == null)
            return false;

        if (!selection.IsSelected(stack))
            selection.SetSingle(stack);

        IReadOnlyList<ItemStack> stacks = selection.GetSelectedStacks();
        return TryMoveItemStacks(session, sourceContainer, target, stacks, () => selection.Clear());
    }
}
