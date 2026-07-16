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

    public static string Craft => Loc.Get(KeyCraft, "제작");
    public static string Uncraft => Loc.Get(KeyUncraft, "분해");
    public static string MiscGroup => Loc.Get(KeyMiscGroup, "기타");
    public static string UncraftPrefix => Loc.Get(KeyUncraftPrefix, "분해: ");
    public static string CraftBlocked => Loc.Get(KeyCraftBlocked, "재료·도구·스킬 부족");
    public static string UncraftBlocked => Loc.Get(KeyUncraftBlocked, "분해 불가");
    public static string UnknownResult => Loc.Get(KeyUnknownResult, "?");
    public const string SubmenuChevron = "▶";
}
