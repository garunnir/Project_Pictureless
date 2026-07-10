// ============================================================
// SmallItemRegistry — 셀 소속 소형 아이템 역인덱스 (월드 오브젝트)
// ============================================================

using System.Collections.Generic;
using UnityEngine;

public static class SmallItemRegistry
{
    static readonly Dictionary<Vector3Int, List<SmallItemObject>> ItemsByCell = new();

    public static void Register(SmallItemObject item)
    {
        if (item == null)
            return;

        Vector3Int cell = item.OwnerCell;
        if (!ItemsByCell.TryGetValue(cell, out List<SmallItemObject> list))
        {
            list = new List<SmallItemObject>();
            ItemsByCell[cell] = list;
        }

        if (!list.Contains(item))
            list.Add(item);
    }

    public static void Unregister(SmallItemObject item)
    {
        if (item == null)
            return;

        Vector3Int cell = item.OwnerCell;
        if (!ItemsByCell.TryGetValue(cell, out List<SmallItemObject> list))
            return;

        list.Remove(item);
        if (list.Count == 0)
            ItemsByCell.Remove(cell);
    }

    public static void CollectInCells(IReadOnlyList<Vector3Int> cells, List<SmallItemObject> into)
    {
        into.Clear();
        if (cells == null || cells.Count == 0)
            return;

        for (int i = 0; i < cells.Count; i++)
        {
            Vector3Int cell = cells[i];
            if (!ItemsByCell.TryGetValue(cell, out List<SmallItemObject> list))
                continue;

            for (int j = 0; j < list.Count; j++)
            {
                SmallItemObject item = list[j];
                if (item != null && !into.Contains(item))
                    into.Add(item);
            }
        }
    }

    public static void TryGetAtCell(Vector3Int cell, List<SmallItemObject> into)
    {
        into.Clear();
        if (!ItemsByCell.TryGetValue(cell, out List<SmallItemObject> list))
            return;

        for (int i = 0; i < list.Count; i++)
        {
            SmallItemObject item = list[i];
            if (item != null)
                into.Add(item);
        }
    }
}
