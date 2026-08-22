// ============================================================
// ConsumeContextAction — 인벤 먹기/마시기/사용 실행
// ============================================================

public sealed class ConsumeContextAction : IContextMenuAction
{
    readonly ItemStack _stack;
    readonly InventoryContainer _container;

    public ConsumeContextAction(ItemStack stack, InventoryContainer container)
    {
        _stack = stack;
        _container = container;
    }

    public string GetDisabledReason()
    {
        if (!ConsumeService.CanConsume(_stack, _container))
            return ItemContextMenuLabels.ConsumeBlocked;

        string hand = CharacterHandWork.GetBlockedReason(
            _stack,
            _container,
            CharacterHandWork.DefaultHand(_stack));
        if (hand != null)
            return hand;

        return null;
    }

    public void Execute()
    {
        ConsumeService.TryBegin(_stack, _container);
    }
}
