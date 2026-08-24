// ============================================================
// ConsumeContextAction — 먹기/마시기/사용(부위 지정 heal 포함) 실행
// ============================================================

public sealed class ConsumeContextAction : IContextMenuAction
{
    readonly ItemStack _stack;
    readonly InventoryContainer _container;
    readonly string _partId;

    public ConsumeContextAction(ItemStack stack, InventoryContainer container)
        : this(stack, container, partId: null)
    {
    }

    public ConsumeContextAction(ItemStack stack, InventoryContainer container, string partId)
    {
        _stack = stack;
        _container = container;
        _partId = partId;
    }

    public string GetDisabledReason()
    {
        if (!ConsumeService.CanConsume(_stack, _container, _partId))
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
        ConsumeService.TryBegin(_stack, _container, _partId);
    }
}
