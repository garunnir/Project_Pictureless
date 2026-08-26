// ============================================================
// FishRodContextAction — 셀 타겟팅 후 낚시 Cast
// ============================================================

using IsoTilemap;

public sealed class FishRodContextAction : IContextMenuAction
{
    readonly ItemStack _stack;
    readonly InventoryContainer _container;

    public FishRodContextAction(ItemStack stack, InventoryContainer container)
    {
        _stack = stack;
        _container = container;
    }

    public string GetDisabledReason() =>
        MapFishService.CanCast(_stack, _container) ? null : ItemContextMenuLabels.FishBlocked;

    public void Execute() =>
        FishCellTargetFlow.BeginCast(_stack, _container);
}
