// ============================================================
// WeaponAmmoLabels — 삽탄·장착 문구 SSOT
// ============================================================

public static class WeaponAmmoLabels
{
    const string KeyLoad = "ItemContextMenu.LoadAmmo";
    const string KeyAttach = "ItemContextMenu.AttachMag";
    const string KeySwap = "ItemContextMenu.SwapMag";
    const string KeyDetach = "ItemContextMenu.DetachMag";
    const string KeyUnload = "ItemContextMenu.UnloadAmmo";
    const string KeyBlocked = "ItemContextMenu.AmmoBlocked";
    const string KeyBusy = "ItemContextMenu.AmmoBusy";
    const string KeyFull = "ItemContextMenu.AmmoFull";
    const string KeyWrong = "ItemContextMenu.AmmoWrong";
    const string KeyNoRoom = "ItemContextMenu.AmmoNoRoom";

    public static string Load => Loc.Get(KeyLoad);
    public static string Attach => Loc.Get(KeyAttach);
    public static string Swap => Loc.Get(KeySwap);
    public static string Detach => Loc.Get(KeyDetach);
    public static string Unload => Loc.Get(KeyUnload);
    public static string Blocked => Loc.Get(KeyBlocked);
    public static string Busy => Loc.Get(KeyBusy);
    public static string Full => Loc.Get(KeyFull);
    public static string Wrong => Loc.Get(KeyWrong);
    public static string NoRoom => Loc.Get(KeyNoRoom);
}
