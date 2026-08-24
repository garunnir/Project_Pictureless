// ============================================================
// PlantContextAction — 발밑 PLANTABLE 셀에 씨앗 1개 소비·심기
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
        MapPlantService.TryPlant(_stack, _container);
}
