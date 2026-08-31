// ============================================================
// IItemStackDisplayEquivalence — UI 리스트 표시/그룹 동일성 SSOT
// ============================================================

/// <summary>
/// AddItem merge와 분리된 표시용 동일성. UI는 이 계약만 사용한다.
/// </summary>
public interface IItemStackDisplayEquivalence
{
    /// <summary>Dictionary/GroupBy용. null 스택은 default 키.</summary>
    ItemMergeKey GetDisplayKey(ItemStack stack);

    /// <summary>같은 display key면 동일 그룹.</summary>
    bool AreDisplayEquivalent(ItemStack a, ItemStack b);

    /// <summary>리스트에서 한 행으로 묶지 않을 스택 (중첩 가방 등).</summary>
    bool IsAtomicRow(ItemStack stack);
}
