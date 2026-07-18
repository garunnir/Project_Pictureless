// ============================================================
// DoorToggleContextContributor — 문 토글 액션 1개
// ============================================================

using System.Collections.Generic;
using Interactions;

public sealed class DoorToggleContextContributor : ITileObjectContextMenuContributor
{
    public void Contribute(TileObjectInteractionTarget target, List<ContextMenuEntry> roots)
    {
        if (target == null || roots == null)
            return;

        DoorInteractable door = target.GetComponentInParent<DoorInteractable>();
        if (door == null)
            door = target.GetComponentInChildren<DoorInteractable>(true);

        if (door == null)
            return;

        roots.Add(ContextMenuEntry.Leaf(
            "toggle-door",
            door.ToggleActionLabel,
            new DoorToggleContextAction(door)));
    }
}
