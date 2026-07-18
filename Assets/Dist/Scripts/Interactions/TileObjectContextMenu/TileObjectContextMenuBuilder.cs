// ============================================================
// TileObjectContextMenuBuilder — Catalog Contributor 순회 → Model
// ============================================================

using System.Collections.Generic;

public static class TileObjectContextMenuBuilder
{
    public static ContextMenuModel Build(
        TileObjectInteractionTarget target,
        IReadOnlyList<ITileObjectContextMenuContributor> contributors)
    {
        var roots = new List<ContextMenuEntry>();
        if (target == null || contributors == null)
            return new ContextMenuModel(roots);

        for (int i = 0; i < contributors.Count; i++)
            contributors[i]?.Contribute(target, roots);

        return new ContextMenuModel(roots);
    }
}
