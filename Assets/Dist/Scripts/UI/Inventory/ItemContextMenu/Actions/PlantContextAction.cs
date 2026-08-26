// ============================================================
// PlantContextAction — 셀 타겟팅 후 심기
// ============================================================

public sealed class PlantContextAction : IContextMenuAction
{
    readonly ItemStack _stack;
    readonly InventoryContainer _container;

    public PlantContextAction(ItemStack stack, InventoryContainer container)
    {
        _stack = stack;
        _container = container;
    }

    public string GetDisabledReason() =>
        MapPlantService.CanPlant(_stack, _container) ? null : ItemContextMenuLabels.PlantBlocked;

    public void Execute() =>
        FarmCellTargetFlow.BeginPlant(_stack, _container);
}
