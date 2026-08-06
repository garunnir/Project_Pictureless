// ============================================================
// WornPocketRules — 착용 storage 포켓 → 인벤 사이드바 오케스트레이션
// ============================================================

using System.Collections.Generic;
using Garunnir.Runtime.Gameplay.Data;

/// <summary>
/// Nested 생성 SSOT는 ArmorStorageNested (Dist.Inventory). 여기선 Wear 목록·소유 조회.
/// </summary>
public static class WornPocketRules
{
    public static bool CanHaveNested(ItemData item) => ArmorStorageNested.CanHaveNested(item);

    public static bool HasStorageCapacity(ItemData item) => ArmorStorageNested.HasCapacity(item);

    public static int ResolveStorageVolumeMl(ItemData item) =>
        ArmorStorageNested.ResolveStorageVolumeMl(item);

    public static int PreferDrawMoves(ItemData item) => ArmorStorageNested.PreferDrawMoves(item);

    public static bool TryEnsurePocket(ItemStack stack, IContainerCapacityPolicy nestedPolicy) =>
        ArmorStorageNested.TryEnsure(stack, nestedPolicy);

    public static void EnsureWornPockets(
        EquipmentWearState wear,
        IContainerCapacityPolicy nestedPolicy)
    {
        if (wear == null)
            return;

        IReadOnlyList<ItemStack> worn = wear.Worn;
        for (int i = 0; i < worn.Count; i++)
        {
            ItemStack stack = worn[i];
            if (stack == null || !HasStorageCapacity(stack.Item))
                continue;
            stack.TryEnsureNested(nestedPolicy);
        }
    }

    public static bool TryFindOwnerStack(
        InventoryContainer nested,
        EquipmentWearState wear,
        out ItemStack owner)
    {
        owner = null;
        if (nested == null || wear == null)
            return false;

        IReadOnlyList<ItemStack> worn = wear.Worn;
        for (int i = 0; i < worn.Count; i++)
        {
            ItemStack stack = worn[i];
            if (stack?.Nested == nested)
            {
                owner = stack;
                return true;
            }
        }

        return false;
    }

    public static bool IsArmorPocketContainer(InventoryContainer container) =>
        ArmorStorageNested.IsArmorPocketContainer(container);
}
