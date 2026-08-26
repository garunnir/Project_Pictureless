// ============================================================
// FishTrapContextAction — 셀 타겟팅 후 통발 DeployTrap
// ============================================================

using IsoTilemap;

public sealed class FishTrapContextAction : IContextMenuAction
{
    readonly ItemStack _stack;
    readonly InventoryContainer _container;

    public FishTrapContextAction(ItemStack stack, InventoryContainer container)
    {
        _stack = stack;
        _container = container;
    }

    public string GetDisabledReason() =>
        MapFishService.CanDeployTrap(_stack, _container) ? null : FishTrapContextLabels.DeployBlocked;

    public void Execute() =>
        FishCellTargetFlow.BeginDeployTrap(_stack, _container);
}
