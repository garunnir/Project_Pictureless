// ============================================================
// FertilizeContextAction — 발밑 식물에 비료 1회 적용
// ============================================================

public sealed class FertilizeContextAction : IContextMenuAction
{
    readonly ItemStack _stack;
    readonly InventoryContainer _container;

    public FertilizeContextAction(ItemStack stack, InventoryContainer container)
    {
        _stack = stack;
        _container = container;
    }

    public string GetDisabledReason() =>
        MapPlantService.CanFertilize(_stack, _container) ? null : ItemContextMenuLabels.FertilizeBlocked;

    public void Execute() =>
        MapPlantService.TryFertilize(_stack, _container);
}
