// ============================================================
// PlayerCarryCapacityPolicy — 플레이어 몸통 동적 무게·부피 한도
// ============================================================
// 무게는 soft(과적 허용). 부피만 hard block.
// ============================================================

using System;

public sealed class PlayerCarryCapacityPolicy : IContainerCapacityPolicy
{
    readonly Func<float> _maxWeightProvider;
    readonly Func<float> _maxVolumeProvider;

    public PlayerCarryCapacityPolicy(Func<float> maxWeightProvider, Func<float> maxVolumeProvider)
    {
        _maxWeightProvider = maxWeightProvider ?? (() => 0f);
        _maxVolumeProvider = maxVolumeProvider ?? (() => 0f);
    }

    public bool EnforcesHardWeightLimit => false;
    public bool EnforcesHardVolumeLimit => true;

    public float GetMaxWeight(InventoryContainer container) => _maxWeightProvider();

    public float GetMaxVolume(InventoryContainer container) => _maxVolumeProvider();

    public bool CanAccept(InventoryContainer target, ItemStack incoming)
    {
        if (target == null || incoming?.Item == null)
            return false;

        return FixedContainerCapacityPolicy.HasVolumeRoom(target, incoming);
    }
}
