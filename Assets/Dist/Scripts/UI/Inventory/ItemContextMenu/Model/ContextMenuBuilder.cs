// ============================================================
// ContextMenuBuilder — Catalog Contributor 순회 → Model
// ============================================================

using System.Collections.Generic;

public static class ContextMenuBuilder
{
    public static ContextMenuModel Build(
        ItemStack stack,
        InventoryContainer container,
        InventorySession session,
        IReadOnlyList<IContextMenuContributor> contributors)
    {
        var roots = new List<ContextMenuEntry>();
        if (stack?.Item == null || contributors == null)
            return new ContextMenuModel(roots);

        for (int i = 0; i < contributors.Count; i++)
        {
            IContextMenuContributor contributor = contributors[i];
            contributor?.Contribute(stack, container, session, roots);
        }

        return new ContextMenuModel(roots);
    }
}
