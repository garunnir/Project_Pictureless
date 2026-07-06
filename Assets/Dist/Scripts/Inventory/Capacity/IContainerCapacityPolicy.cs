// ============================================================
// IContainerCapacityPolicy — 컨테이너 수용 한도 정책 (무게·부피)
// ============================================================

public interface IContainerCapacityPolicy
{
    float GetMaxWeight(InventoryContainer container);
    float GetMaxVolume(InventoryContainer container);
    bool CanAccept(InventoryContainer target, ItemStack incoming);
}
