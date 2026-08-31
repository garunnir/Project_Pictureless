// ============================================================
// InventoryListDisplayGroup — 집계 리스트 한 행의 표시 묶음 SSOT
// ============================================================

using System.Collections.Generic;

/// <summary>
/// 동일 표시 키로 묶인 스택들의 합산 표시값. <see cref="RepresentativeStack"/>은 아이콘·이름·선택 대표.
/// </summary>
public sealed class InventoryListDisplayGroup
{
    readonly List<(InventoryContainer owner, ItemStack stack)> _sources = new();

    public ItemStack RepresentativeStack { get; }
    public int DisplayCount { get; private set; }
    public float DisplayWeight { get; private set; }
    public float DisplayVolume { get; private set; }
    public IReadOnlyList<(InventoryContainer owner, ItemStack stack)> Sources => _sources;

    InventoryListDisplayGroup(ItemStack representativeStack)
    {
        RepresentativeStack = representativeStack;
    }

    public static InventoryListDisplayGroup FromSingle(
        InventoryContainer owner,
        ItemStack stack)
    {
        var group = new InventoryListDisplayGroup(stack);
        group.AddSource(owner, stack);
        return group;
    }

    internal static InventoryListDisplayGroup CreateRepresentative(ItemStack representativeStack) =>
        new InventoryListDisplayGroup(representativeStack);

    internal void AddSource(InventoryContainer owner, ItemStack stack)
    {
        if (stack == null)
            return;

        _sources.Add((owner, stack));
        DisplayCount += stack.Count;
        DisplayWeight += stack.TotalWeight;
        DisplayVolume += stack.TotalVolume;
    }
}
