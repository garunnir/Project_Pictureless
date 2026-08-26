// ============================================================
// HarvestContextLabels — 식물 수확 컨텍스트 메뉴 문구 SSOT
// ============================================================

public static class HarvestContextLabels
{
    const string KeyHarvest = "Interaction.Harvest";
    const string KeyHarvestNotReady = "Interaction.HarvestNotReady";
    const string KeyHarvestBlocked = "Interaction.HarvestBlocked";

    public static string Harvest =>
        Loc.TryGet(KeyHarvest, out string harvest) ? harvest : "수확";
    public static string HarvestNotReady =>
        Loc.TryGet(KeyHarvestNotReady, out string notReady) ? notReady : "아직 자라지 않음";
    public static string HarvestBlocked =>
        Loc.TryGet(KeyHarvestBlocked, out string blocked) ? blocked : "수확 불가";

    const string KeyTill = "Interaction.Till";
    const string KeyTillBlocked = "Interaction.TillBlocked";
    const string KeyFertilize = "Interaction.Fertilize";
    const string KeyFertilizeBlocked = "Interaction.FertilizeBlocked";

    public static string Till =>
        Loc.TryGet(KeyTill, out string till) ? till : "경작";
    public static string TillBlocked =>
        Loc.TryGet(KeyTillBlocked, out string tillBlocked) ? tillBlocked : "경작할 수 없음";
    public static string Fertilize =>
        Loc.TryGet(KeyFertilize, out string fertilize) ? fertilize : "비료";
    public static string FertilizeBlocked =>
        Loc.TryGet(KeyFertilizeBlocked, out string fertilizeBlocked) ? fertilizeBlocked : "비료를 줄 수 없음";

    const string KeyChop = "Interaction.Chop";
    const string KeyChopBlocked = "Interaction.ChopBlocked";

    public static string Chop =>
        Loc.TryGet(KeyChop, out string chop) ? chop : "벌목";
    public static string ChopBlocked =>
        Loc.TryGet(KeyChopBlocked, out string chopBlocked) ? chopBlocked : "벌목할 수 없음";
}
