// ============================================================
// FishCellTargetFlow — 컨텍스트 메뉴 Execute → 낚시 타겟팅 세션 시작
// ============================================================

using UnityEngine;

public static class FishCellTargetFlow
{
    public static void BeginCast(ItemStack stack, InventoryContainer container) =>
        FishCellTargetSession.TryBegin(FishCellActionKind.Cast, stack, container);

    public static void BeginDeployTrap(ItemStack stack, InventoryContainer container) =>
        FishCellTargetSession.TryBegin(FishCellActionKind.DeployTrap, stack, container);

    public static void BeginCollectTrap(Vector3Int cell)
    {
        FishCellActionHost host = ResolveActionHost();
        host?.TryRun(FishCellActionKind.CollectTrap, cell, null, null);
    }

    static FishCellActionHost ResolveActionHost()
    {
        PlayerGearHost gear = PlayerGearHost.Active;
        if (gear != null)
        {
            if (!gear.TryGetComponent(out FishCellActionHost host))
                host = gear.gameObject.AddComponent<FishCellActionHost>();
            return host;
        }

        if (PlayerInventoryRuntime.Active?.Host != null &&
            PlayerInventoryRuntime.Active.Host.TryGetComponent(out FishCellActionHost inventoryHost))
            return inventoryHost;

        return null;
    }
}
