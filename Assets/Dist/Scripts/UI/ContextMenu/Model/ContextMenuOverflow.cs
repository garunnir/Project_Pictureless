// ============================================================
// ContextMenuOverflow — 비활성 Leaf가 많으면 서브메뉴로 접음
// ============================================================

using System;
using System.Collections.Generic;

public static class ContextMenuOverflow
{
    public const string OverflowGroupId = "overflow-disabled";

    public static IReadOnlyList<ContextMenuEntry> Fold(IReadOnlyList<ContextMenuEntry> entries)
    {
        if (entries == null || entries.Count == 0)
            return entries ?? Array.Empty<ContextMenuEntry>();

        var kept = new List<ContextMenuEntry>(entries.Count);
        var disabled = new List<ContextMenuEntry>();

        for (int i = 0; i < entries.Count; i++)
        {
            ContextMenuEntry entry = entries[i];
            if (entry == null)
                continue;

            if (entry.HasChildren)
            {
                kept.Add(WithChildren(entry, Fold(entry.Children)));
                continue;
            }

            if (IsDisabledLeaf(entry))
                disabled.Add(entry);
            else
                kept.Add(entry);
        }

        if (disabled.Count >= ContextMenuStyle.DisabledLeafOverflowMin)
        {
            kept.Add(ContextMenuEntry.Group(
                OverflowGroupId,
                ContextMenuChromeLabels.FormatDisabledOverflow(disabled.Count),
                disabled));
        }
        else
        {
            for (int i = 0; i < disabled.Count; i++)
                kept.Add(disabled[i]);
        }

        return kept;
    }

    static bool IsDisabledLeaf(ContextMenuEntry entry)
    {
        if (entry == null || entry.HasChildren || entry.Action == null)
            return false;
        return entry.Action.GetDisabledReason() != null;
    }

    static ContextMenuEntry WithChildren(
        ContextMenuEntry source,
        IReadOnlyList<ContextMenuEntry> children)
    {
        return new ContextMenuEntry
        {
            Id = source.Id,
            Label = source.Label,
            Icon = source.Icon,
            Children = children,
            Action = source.Action
        };
    }
}
