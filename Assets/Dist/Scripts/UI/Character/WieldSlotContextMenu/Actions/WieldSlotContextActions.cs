// ============================================================
// SetHandActionContextAction / UnwieldSlotContextAction — 들기 슬롯 RMB
// ============================================================

public sealed class SetHandActionContextAction : IContextMenuAction
{
    readonly WieldSlotContextRequest _request;
    readonly WeaponAction? _action;

    public SetHandActionContextAction(WieldSlotContextRequest request, WeaponAction? action)
    {
        _request = request;
        _action = action;
    }

    public string GetDisabledReason()
    {
        if (_request?.Gear == null)
            return "missing";
        if (_action == null)
            return null;

        ItemStack stack = _request.Gear.Wield?.Get(_request.Slot);
        if (stack?.Item == null)
            return "missing";

        WeaponPresentation presentation = WeaponActionRows.Resolve(
            _request.Gear.PresentationCatalog,
            stack);
        WeaponActionMask mask = WeaponActionRows.Available(presentation);
        return (mask & WeaponActionUtil.ToMask(_action.Value)) == 0
            ? CharacterGearLabels.BlockedInvalid
            : null;
    }

    public void Execute()
    {
        if (_request?.Gear == null || !string.IsNullOrEmpty(GetDisabledReason()))
            return;

        ItemStack stack = _request.Gear.Wield?.Get(_request.Slot);
        _request.Gear.TrySetHandAction(stack, _action);
        _request.OnChanged?.Invoke();
    }
}

public sealed class UnwieldSlotContextAction : IContextMenuAction
{
    readonly WieldSlotContextRequest _request;
    readonly bool _toFloor;

    public UnwieldSlotContextAction(WieldSlotContextRequest request, bool toFloor)
    {
        _request = request;
        _toFloor = toFloor;
    }

    public string GetDisabledReason() => _request?.Gear == null ? "missing" : null;

    public void Execute()
    {
        if (_request?.Gear == null)
            return;

        _request.Gear.TryBeginUnwieldSlot(_request.Slot, _toFloor);
        _request.OnChanged?.Invoke();
    }
}
