// ============================================================
// ItemListStackComparer — 아이템 스택 뷰 전용 비교 (컨테이너 순서 불변)
// ============================================================

using System;
using System.Collections.Generic;
using Garunnir.Runtime.Gameplay.Data;

public sealed class ItemListStackComparer : IComparer<ItemStack>
{
    readonly ItemListSortKey _key;
    readonly bool _ascending;

    public ItemListStackComparer(ItemListSortKey key, bool ascending)
    {
        _key = key;
        _ascending = ascending;
    }

    public int Compare(ItemStack a, ItemStack b)
    {
        if (ReferenceEquals(a, b))
            return 0;
        if (a == null)
            return 1;
        if (b == null)
            return -1;

        int cmp = CompareKey(a, b);
        if (cmp == 0)
            cmp = CompareId(a, b);

        return _ascending ? cmp : -cmp;
    }

    int CompareKey(ItemStack a, ItemStack b)
    {
        switch (_key)
        {
            case ItemListSortKey.Category:
                return string.Compare(
                    InventoryWindowLabels.GetItemCategory(a.Item?.category),
                    InventoryWindowLabels.GetItemCategory(b.Item?.category),
                    StringComparison.OrdinalIgnoreCase);
            case ItemListSortKey.Name:
                return string.Compare(
                    GetDisplayName(a.Item),
                    GetDisplayName(b.Item),
                    StringComparison.OrdinalIgnoreCase);
            case ItemListSortKey.Count:
                return a.Count.CompareTo(b.Count);
            case ItemListSortKey.Weight:
                return a.TotalWeight.CompareTo(b.TotalWeight);
            case ItemListSortKey.Volume:
                return a.TotalVolume.CompareTo(b.TotalVolume);
            default:
                return 0;
        }
    }

    static int CompareId(ItemStack a, ItemStack b)
    {
        string idA = a.Item != null ? a.Item.id : string.Empty;
        string idB = b.Item != null ? b.Item.id : string.Empty;
        return string.Compare(idA, idB, StringComparison.Ordinal);
    }

    static string GetDisplayName(ItemData item)
    {
        if (item == null)
            return string.Empty;

        return UITextPresenter.GetItemName(item) ?? string.Empty;
    }
}
