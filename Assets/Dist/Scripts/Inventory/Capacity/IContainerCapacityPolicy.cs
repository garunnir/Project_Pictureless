// ============================================================
// IContainerCapacityPolicy — 컨테이너 수용 한도 정책 (무게·부피)
// ============================================================

public interface IContainerCapacityPolicy
{
    float GetMaxWeight(InventoryContainer container);
    float GetMaxVolume(InventoryContainer container);

    /// <summary>true면 Session 이동이 max weight를 하드 거절한다.</summary>
    bool EnforcesHardWeightLimit { get; }

    /// <summary>true면 Session 이동이 max volume을 하드 거절한다.</summary>
    bool EnforcesHardVolumeLimit { get; }

    bool CanAccept(InventoryContainer target, ItemStack incoming);
}
