// ============================================================
// ItemMergeKeyDisplayEquivalence — ItemMergeKey 기반 UI 그룹 동일성
// ============================================================

/// <summary>
/// 표시 묶기는 <see cref="ItemMergeKey.From"/> 키 equality만 사용한다.
/// MaxStack·CanMerge·HasStackSpace는 사용하지 않는다.
/// </summary>
public sealed class ItemMergeKeyDisplayEquivalence : IItemStackDisplayEquivalence
{
    public static readonly ItemMergeKeyDisplayEquivalence Instance = new ItemMergeKeyDisplayEquivalence();

    public ItemMergeKey GetDisplayKey(ItemStack stack) => ItemMergeKey.From(stack);

    public bool AreDisplayEquivalent(ItemStack a, ItemStack b) =>
        a != null
        && b != null
        && GetDisplayKey(a).Equals(GetDisplayKey(b));

    public bool IsAtomicRow(ItemStack stack) => stack?.CanHaveNested == true;
}
