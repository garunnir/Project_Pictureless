// ============================================================
// SetHandActionContextAction / UnwieldSlotContextAction — 들기 슬롯 RMB
// ============================================================

using Garunnir.Runtime.Gameplay.Data;

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
        if (_request?.Gear == null || string.IsNullOrEmpty(_request.ItemId))
            return "missing";
        if (_action == null)
            return null;

        ItemData item = _request.Gear.Wield?.Get(_request.Slot)?.Item;
        if (item == null)
            return "missing";

        WeaponActionMask mask = CombatMath.AvailableModes(item);
        return (mask & WeaponActionUtil.ToMask(_action.Value)) == 0
            ? CharacterGearLabels.BlockedInvalid
            : null;
    }

    public void Execute()
    {
        if (_request?.Gear == null || !string.IsNullOrEmpty(GetDisabledReason()))
            return;

        _request.Gear.TrySetHandAction(_request.ItemId, _action);
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
