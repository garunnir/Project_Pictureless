// ============================================================
// PlayerCarryCapacityPolicy — 플레이어 몸통 동적 무게·부피 한도
// ============================================================

using System;

public sealed class PlayerCarryCapacityPolicy : IContainerCapacityPolicy
{
    static readonly FixedContainerCapacityPolicy Acceptance = new();

    readonly Func<float> _maxWeightProvider;
    readonly Func<float> _maxVolumeProvider;

    public PlayerCarryCapacityPolicy(Func<float> maxWeightProvider, Func<float> maxVolumeProvider)
    {
        _maxWeightProvider = maxWeightProvider ?? (() => 0f);
        _maxVolumeProvider = maxVolumeProvider ?? (() => 0f);
    }

    public float GetMaxWeight(InventoryContainer container) => _maxWeightProvider();

    public float GetMaxVolume(InventoryContainer container) => _maxVolumeProvider();

    public bool CanAccept(InventoryContainer target, ItemStack incoming) =>
        Acceptance.CanAccept(target, incoming);
}
