// ============================================================
// ContextMenuModel — 우클릭 1회 결과(루트 Entry 목록)
// ============================================================

using System.Collections.Generic;

public sealed class ContextMenuModel
{
    public IReadOnlyList<ContextMenuEntry> Roots { get; }

    public ContextMenuModel(IReadOnlyList<ContextMenuEntry> roots)
    {
        Roots = roots ?? System.Array.Empty<ContextMenuEntry>();
    }

    public bool IsEmpty => Roots.Count == 0;
}
