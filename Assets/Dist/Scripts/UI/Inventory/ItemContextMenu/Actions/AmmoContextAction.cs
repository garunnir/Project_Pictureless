// ============================================================
// AmmoContextAction — 삽탄·장착·분리·탄 빼기 실행
// ============================================================

public sealed class AmmoLoadContextAction : IContextMenuAction
{
    readonly ItemStack _ammo;
    readonly ItemStack _target;
    readonly InventorySession _session;

    public AmmoLoadContextAction(ItemStack ammo, ItemStack target, InventorySession session)
    {
        _ammo = ammo;
        _target = target;
        _session = session;
    }

    public string GetDisabledReason() => WeaponAmmoService.GetLoadBlockedReason(_ammo, _target);

    public void Execute() => WeaponAmmoService.TryBeginLoad(_ammo, _target, _session);
}

public sealed class AmmoAttachContextAction : IContextMenuAction
{
    readonly ItemStack _magazine;
    readonly ItemStack _gun;
    readonly InventorySession _session;

    public AmmoAttachContextAction(ItemStack magazine, ItemStack gun, InventorySession session)
    {
        _magazine = magazine;
        _gun = gun;
        _session = session;
    }

    public string GetDisabledReason() => WeaponAmmoService.GetAttachBlockedReason(_magazine, _gun);

    public void Execute() => WeaponAmmoService.TryBeginAttach(_magazine, _gun, _session);
}

public sealed class AmmoDetachContextAction : IContextMenuAction
{
    readonly ItemStack _gun;
    readonly InventorySession _session;

    public AmmoDetachContextAction(ItemStack gun, InventorySession session)
    {
        _gun = gun;
        _session = session;
    }

    public string GetDisabledReason() => WeaponAmmoService.GetDetachBlockedReason(_gun);

    public void Execute() => WeaponAmmoService.TryBeginDetach(_gun, _session);
}

public sealed class AmmoUnloadContextAction : IContextMenuAction
{
    readonly ItemStack _magazine;
    readonly InventorySession _session;

    public AmmoUnloadContextAction(ItemStack magazine, InventorySession session)
    {
        _magazine = magazine;
        _session = session;
    }

    public string GetDisabledReason() => WeaponAmmoService.GetUnloadBlockedReason(_magazine, _session);

    public void Execute() => WeaponAmmoService.TryBeginUnload(_magazine, _session);
}
