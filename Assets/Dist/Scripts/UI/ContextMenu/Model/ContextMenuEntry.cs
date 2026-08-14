// ============================================================
// ContextMenuEntry — 컨텍스트 메뉴 한 줄(그룹 또는 리프)
// ============================================================

using System.Collections.Generic;
using UnityEngine;

public sealed class ContextMenuEntry
{
    public string Id;
    public string Label;
    public Sprite Icon;
    public IReadOnlyList<ContextMenuEntry> Children;
    public IContextMenuAction Action;

    public bool HasChildren => Children != null && Children.Count > 0;

    public static ContextMenuEntry Group(string id, string label, IReadOnlyList<ContextMenuEntry> children)
    {
        return new ContextMenuEntry
        {
            Id = id,
            Label = label,
            Icon = null,
            Children = children,
            Action = null
        };
    }

    public static ContextMenuEntry Leaf(string id, string label, IContextMenuAction action)
    {
        return Leaf(id, label, action, icon: null);
    }

    public static ContextMenuEntry Leaf(string id, string label, IContextMenuAction action, Sprite icon)
    {
        return new ContextMenuEntry
        {
            Id = id,
            Label = label,
            Icon = icon,
            Children = null,
            Action = action
        };
    }
}
