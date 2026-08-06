// ============================================================
// GearWearContextAction / GearWieldContextAction — 착용·들기 실행
// ============================================================

public sealed class GearWearContextAction : IContextMenuAction
{
    readonly ItemStack _stack;
    readonly InventoryContainer _container;

    public GearWearContextAction(ItemStack stack, InventoryContainer container)
    {
        _stack = stack;
        _container = container;
    }

    public string GetDisabledReason()
    {
        CharacterGearService gear = PlayerGearHost.Active?.Service;
        if (gear == null)
            return CharacterGearLabels.BlockedInvalid;
        return gear.GetWearBlockedReason(_stack);
    }

    public void Execute()
    {
        PlayerGearHost.Active?.Service?.TryBeginWear(_stack, _container);
    }
}

public sealed class GearWieldContextAction : IContextMenuAction
{
    readonly ItemStack _stack;
    readonly InventoryContainer _container;
    readonly WieldHand _hand;

    public GearWieldContextAction(ItemStack stack, InventoryContainer container, WieldHand hand)
    {
        _stack = stack;
        _container = container;
        _hand = hand;
    }

    public string GetDisabledReason()
    {
        CharacterGearService gear = PlayerGearHost.Active?.Service;
        if (gear == null)
            return CharacterGearLabels.BlockedInvalid;
        return gear.GetWieldBlockedReason(_stack, _hand);
    }

    public void Execute()
    {
        PlayerGearHost.Active?.Service?.TryBeginWield(_stack, _container, _hand);
    }
}
