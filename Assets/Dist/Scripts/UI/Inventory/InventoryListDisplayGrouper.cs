// ============================================================
// InventoryListDisplayGrouper — 컨테이너 스택 → 표시 그룹 변환
// ============================================================

using System;
using System.Collections.Generic;
using Garunnir.Runtime.Gameplay.Data;

public static class InventoryListDisplayGrouper
{
    public static List<InventoryListDisplayGroup> Group(
        IReadOnlyList<ItemStack> stacks,
        IItemStackDisplayEquivalence equivalence,
        Func<ItemStack, InventoryContainer> resolveOwner)
    {
        var groups = new List<InventoryListDisplayGroup>();
        if (stacks == null || stacks.Count == 0 || equivalence == null)
            return groups;

        var mergeKeyToBuilder = new Dictionary<ItemMergeKey, InventoryListDisplayGroup>();

        for (int i = 0; i < stacks.Count; i++)
        {
            ItemStack stack = stacks[i];
            if (stack?.Item == null)
                continue;

            InventoryContainer owner = resolveOwner != null ? resolveOwner(stack) : null;

            if (equivalence.IsAtomicRow(stack))
            {
                groups.Add(InventoryListDisplayGroup.FromSingle(owner, stack));
                continue;
            }

            ItemMergeKey key = equivalence.GetDisplayKey(stack);
            if (!mergeKeyToBuilder.TryGetValue(key, out InventoryListDisplayGroup group))
            {
                group = InventoryListDisplayGroup.CreateRepresentative(stack);
                mergeKeyToBuilder.Add(key, group);
                groups.Add(group);
            }

            group.AddSource(owner, stack);
        }

        return groups;
    }
}

// ============================================================
// InventoryListDisplayGroupComparer — 집계 행 정렬 (합산 표시값 사용)
// ============================================================

public sealed class InventoryListDisplayGroupComparer : IComparer<InventoryListDisplayGroup>
{
    readonly ItemListSortKey _key;
    readonly bool _ascending;

    public InventoryListDisplayGroupComparer(ItemListSortKey key, bool ascending)
    {
        _key = key;
        _ascending = ascending;
    }

    public int Compare(InventoryListDisplayGroup a, InventoryListDisplayGroup b)
    {
        if (ReferenceEquals(a, b))
            return 0;
        if (a == null)
            return 1;
        if (b == null)
            return -1;

        int cmp = CompareKey(a, b);
        if (cmp == 0)
            cmp = CompareRepresentativeId(a.RepresentativeStack, b.RepresentativeStack);

        return _ascending ? cmp : -cmp;
    }

    int CompareKey(InventoryListDisplayGroup a, InventoryListDisplayGroup b)
    {
        ItemStack repA = a.RepresentativeStack;
        ItemStack repB = b.RepresentativeStack;

        switch (_key)
        {
            case ItemListSortKey.Category:
                return string.Compare(
                    InventoryWindowLabels.GetItemCategory(repA?.Item?.category),
                    InventoryWindowLabels.GetItemCategory(repB?.Item?.category),
                    StringComparison.OrdinalIgnoreCase);
            case ItemListSortKey.Name:
                return string.Compare(
                    GetDisplayName(repA?.Item),
                    GetDisplayName(repB?.Item),
                    StringComparison.OrdinalIgnoreCase);
            case ItemListSortKey.Count:
                return a.DisplayCount.CompareTo(b.DisplayCount);
            case ItemListSortKey.Weight:
                return a.DisplayWeight.CompareTo(b.DisplayWeight);
            case ItemListSortKey.Volume:
                return a.DisplayVolume.CompareTo(b.DisplayVolume);
            default:
                return 0;
        }
    }

    static int CompareRepresentativeId(ItemStack a, ItemStack b)
    {
        string idA = a?.Item != null ? a.Item.id : string.Empty;
        string idB = b?.Item != null ? b.Item.id : string.Empty;
        return string.Compare(idA, idB, StringComparison.Ordinal);
    }

    static string GetDisplayName(ItemData item)
    {
        if (item == null)
            return string.Empty;

        return UITextPresenter.GetItemName(item) ?? string.Empty;
    }
}
