// ============================================================
// InventoryDragDrop — 활성 드래그 페이로드를 대상 컨테이너로 적용
// ============================================================

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

        bool moved;
        if (payload.Kind == InventoryDragKind.ContainerContents)
            moved = session.MoveStacksSequentiallyUntilFull(
                payload.SourceContainer, target, payload.Stacks) > 0;
        else
            moved = session.MoveStacks(payload.SourceContainer, target, payload.Stacks);

        if (!moved)
            return false;

        if (payload.Kind == InventoryDragKind.Item)
            payload.SourceSelection?.Clear();

        return true;
    }
}
