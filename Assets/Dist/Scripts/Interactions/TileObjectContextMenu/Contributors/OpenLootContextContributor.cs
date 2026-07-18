// ============================================================
// OpenLootContextContributor — 컨테이너 루팅 액션 1개
// ============================================================

using System.Collections.Generic;

public sealed class OpenLootContextContributor : ITileObjectContextMenuContributor
{
    public void Contribute(TileObjectInteractionTarget target, List<ContextMenuEntry> roots)
    {
        if (target == null || roots == null)
            return;

        ContainerInteractable container = target.GetComponentInParent<ContainerInteractable>();
        if (container == null)
            container = target.GetComponentInChildren<ContainerInteractable>(true);

        if (container == null || container.Container == null)
            return;

        roots.Add(ContextMenuEntry.Leaf(
            "open-loot",
            container.OpenLootActionLabel,
            new OpenLootContextAction(container)));
    }
}
