// ============================================================
// FixedContainerCapacityPolicy — SO 고정 무게·부피 한도 (상자·냉장고·가방)
// ============================================================

public sealed class FixedContainerCapacityPolicy : IContainerCapacityPolicy
{
    public const float Epsilon = 0.0001f;

    public bool EnforcesHardWeightLimit => true;
    public bool EnforcesHardVolumeLimit => true;

    public float GetMaxWeight(InventoryContainer container)
    {
        if (container?.Definition == null)
            return 0f;

        return container.Definition.MaxWeight;
    }

    public float GetMaxVolume(InventoryContainer container)
    {
        if (container?.Definition == null)
            return 0f;

        return container.Definition.MaxVolume;
    }

    public bool CanAccept(InventoryContainer target, ItemStack incoming)
    {
        if (target == null || incoming?.Item == null)
            return false;

        return HasWeightRoom(target, incoming) && HasVolumeRoom(target, incoming);
    }

    public static bool HasWeightRoom(InventoryContainer target, ItemStack incoming)
    {
        float maxWeight = target.CapacityPolicy.GetMaxWeight(target);
        return target.GetTotalWeight() + incoming.TotalWeight <= maxWeight + Epsilon;
    }

    public static bool HasVolumeRoom(InventoryContainer target, ItemStack incoming)
    {
        float maxVolume = target.CapacityPolicy.GetMaxVolume(target);
        return target.GetTotalVolume() + incoming.TotalVolume <= maxVolume + Epsilon;
    }
}
