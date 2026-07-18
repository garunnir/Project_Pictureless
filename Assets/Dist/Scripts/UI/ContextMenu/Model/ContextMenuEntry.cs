// ============================================================
// ContextMenuEntry — 컨텍스트 메뉴 한 줄(그룹 또는 리프)
// ============================================================

using System.Collections.Generic;

public sealed class ContextMenuEntry
{
    public string Id;
    public string Label;
    public IReadOnlyList<ContextMenuEntry> Children;
    public IContextMenuAction Action;

    public bool HasChildren => Children != null && Children.Count > 0;

    public static ContextMenuEntry Group(string id, string label, IReadOnlyList<ContextMenuEntry> children)
    {
        return new ContextMenuEntry
        {
            Id = id,
            Label = label,
            Children = children,
            Action = null
        };
    }

    public static ContextMenuEntry Leaf(string id, string label, IContextMenuAction action)
    {
        return new ContextMenuEntry
        {
            Id = id,
            Label = label,
            Children = null,
            Action = action
        };
    }
}
