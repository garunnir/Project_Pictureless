// ============================================================
// TillContextAction — DIG 품질 도구로 발밑 PLOWABLE/DIGGABLE 셀 경작
// ============================================================

public sealed class TillContextAction : IContextMenuAction
{
    readonly ItemStack _stack;
    readonly InventoryContainer _container;

    public TillContextAction(ItemStack stack, InventoryContainer container)
    {
        _stack = stack;
        _container = container;
    }

    public string GetDisabledReason() =>
        MapPlantService.CanTill(_stack, _container) ? null : ItemContextMenuLabels.TillBlocked;

    public void Execute() =>
        FarmCellTargetFlow.BeginTill(_stack, _container);
}
