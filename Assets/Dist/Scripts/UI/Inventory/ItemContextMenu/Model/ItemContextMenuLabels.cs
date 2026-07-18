// ============================================================
// ItemContextMenuLabels — 인벤 컨텍스트 메뉴 표시 문구 SSOT
// ============================================================

public static class ItemContextMenuLabels
{
    const string KeyCraft = "ItemContextMenu.Craft";
    const string KeyUncraft = "ItemContextMenu.Uncraft";
    const string KeyMiscGroup = "ItemContextMenu.MiscGroup";
    const string KeyUncraftPrefix = "ItemContextMenu.UncraftPrefix";
    const string KeyCraftBlocked = "ItemContextMenu.CraftBlocked";
    const string KeyUncraftBlocked = "ItemContextMenu.UncraftBlocked";
    const string KeyUnknownResult = "ItemContextMenu.UnknownResult";
    public static string Craft => Loc.Get(KeyCraft);
    public static string Uncraft => Loc.Get(KeyUncraft);
    public static string MiscGroup => Loc.Get(KeyMiscGroup);
    public static string UncraftPrefix => Loc.Get(KeyUncraftPrefix);
    public static string CraftBlocked => Loc.Get(KeyCraftBlocked);
    public static string UncraftBlocked => Loc.Get(KeyUncraftBlocked);
    public static string UnknownResult => Loc.Get(KeyUnknownResult);
    public static string SubmenuChevron => ContextMenuChromeLabels.SubmenuChevron;
}
