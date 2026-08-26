// ============================================================
// FarmCellTargetFlow — 컨텍스트 메뉴 Execute → 타겟팅 세션 시작
// ============================================================

public static class FarmCellTargetFlow
{
    public static void BeginPlant(ItemStack stack, InventoryContainer container) =>
        FarmCellTargetSession.TryBegin(FarmCellActionKind.Plant, stack, container);

    public static void BeginTill(ItemStack stack, InventoryContainer container) =>
        FarmCellTargetSession.TryBegin(FarmCellActionKind.Till, stack, container);

    public static void BeginFertilize(ItemStack stack, InventoryContainer container) =>
        FarmCellTargetSession.TryBegin(FarmCellActionKind.Fertilize, stack, container);

    public static void BeginFertilizeTile() =>
        FarmCellTargetSession.TryBegin(FarmCellActionKind.Fertilize, null, null);

    public static void BeginTillTile() =>
        FarmCellTargetSession.TryBegin(FarmCellActionKind.Till, null, null);

    public static void BeginHarvest() =>
        FarmCellTargetSession.TryBegin(FarmCellActionKind.Harvest, null, null);
}
