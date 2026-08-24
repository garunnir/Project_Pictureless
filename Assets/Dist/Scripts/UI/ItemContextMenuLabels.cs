// ============================================================
// ItemContextMenuLabels — 인벤 컨텍스트 메뉴 표시 문구 SSOT
// ============================================================

public static class ItemContextMenuLabels
{
    const string KeyCraft = "ItemContextMenu.Craft";
    const string KeyUncraft = "ItemContextMenu.Uncraft";
    const string KeyEat = "ItemContextMenu.Eat";
    const string KeyDrink = "ItemContextMenu.Drink";
    const string KeyUse = "ItemContextMenu.Use";
    const string KeyConsumeBlocked = "ItemContextMenu.ConsumeBlocked";
    const string KeyMiscGroup = "ItemContextMenu.MiscGroup";
    const string KeyUncraftPrefix = "ItemContextMenu.UncraftPrefix";
    const string KeyCraftBlocked = "ItemContextMenu.CraftBlocked";
    const string KeyUncraftBlocked = "ItemContextMenu.UncraftBlocked";
    const string KeyUnknownResult = "ItemContextMenu.UnknownResult";
    const string KeyPlant = "ItemContextMenu.Plant";
    const string KeyPlantBlocked = "ItemContextMenu.PlantBlocked";
    const string KeyTill = "ItemContextMenu.Till";
    const string KeyTillBlocked = "ItemContextMenu.TillBlocked";
    const string KeyFertilize = "ItemContextMenu.Fertilize";
    const string KeyFertilizeBlocked = "ItemContextMenu.FertilizeBlocked";
    const string KeyUnwrap = "ItemContextMenu.Unwrap";

    public static string Craft => Loc.Get(KeyCraft);
    public static string Uncraft => Loc.Get(KeyUncraft);
    public static string Eat => Loc.Get(KeyEat);
    public static string Drink => Loc.Get(KeyDrink);
    public static string Use => Loc.Get(KeyUse);
    public static string ConsumeBlocked => Loc.Get(KeyConsumeBlocked);
    public static string MiscGroup => Loc.Get(KeyMiscGroup);
    public static string UncraftPrefix => Loc.Get(KeyUncraftPrefix);
    public static string CraftBlocked => Loc.Get(KeyCraftBlocked);
    public static string UncraftBlocked => Loc.Get(KeyUncraftBlocked);
    public static string UnknownResult => Loc.Get(KeyUnknownResult);
    public static string Plant => Loc.TryGet(KeyPlant, out string plant) ? plant : "심기";
    public static string PlantBlocked =>
        Loc.TryGet(KeyPlantBlocked, out string blocked) ? blocked : "심을 수 없음";
    public static string Till =>
        Loc.TryGet(KeyTill, out string till) ? till : "경작";
    public static string TillBlocked =>
        Loc.TryGet(KeyTillBlocked, out string tillBlocked) ? tillBlocked : "경작할 수 없음";
    public static string Fertilize =>
        Loc.TryGet(KeyFertilize, out string fertilize) ? fertilize : "비료";
    public static string FertilizeBlocked =>
        Loc.TryGet(KeyFertilizeBlocked, out string fertilizeBlocked) ? fertilizeBlocked : "비료를 줄 수 없음";
    public static string Unwrap =>
        Loc.TryGet(KeyUnwrap, out string unwrap) ? unwrap : "붕대 벗기";
    public static string SubmenuChevron => ContextMenuChromeLabels.SubmenuChevron;
}
