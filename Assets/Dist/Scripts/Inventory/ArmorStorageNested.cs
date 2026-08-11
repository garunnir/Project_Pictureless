// ============================================================
// ArmorStorageNested — armor.storage/pockets → Nested 컨테이너 (Dist.Inventory)
// ============================================================

using Garunnir.Runtime.Gameplay.Data;

/// <summary>
/// 착용/인벤 공용. Gear의 WornPocketRules가 Wear 목록·사이드바만 오케스트레이션.
/// </summary>
public static class ArmorStorageNested
{
    public const string PocketIdPrefix = "armor_pocket:";

    public static bool HasCapacity(ItemData item) => ResolveStorageVolumeMl(item) > 0;

    public static int ResolveStorageVolumeMl(ItemData item)
    {
        if (item?.armor == null)
            return 0;

        ArmorDetailData armor = item.armor;
        if (armor.storage > 0)
            return armor.storage;

        if (armor.pockets == null || armor.pockets.Count == 0)
            return 0;

        int sum = 0;
        for (int i = 0; i < armor.pockets.Count; i++)
        {
            ArmorPocketData pocket = armor.pockets[i];
            if (pocket != null && pocket.volume_ml > 0)
                sum += pocket.volume_ml;
        }

        return sum;
    }

    /// <summary>베이크된 pocket moves 중 최대. 0이면 access 0 (handling only).</summary>
    public static int PreferDrawMoves(ItemData item)
    {
        if (item?.armor?.pockets == null)
            return 0;

        int best = 0;
        for (int i = 0; i < item.armor.pockets.Count; i++)
        {
            ArmorPocketData pocket = item.armor.pockets[i];
            if (pocket == null || pocket.moves <= 0)
                continue;
            if (pocket.moves > best)
                best = pocket.moves;
        }

        return best;
    }

    public static bool CanHaveNested(ItemData item)
    {
        if (item == null)
            return false;

        if (item.is_container && !string.IsNullOrEmpty(item.container_id))
            return true;

        return HasCapacity(item);
    }

    public static bool TryEnsure(ItemStack stack, IContainerCapacityPolicy nestedPolicy)
    {
        if (stack?.Item == null)
            return false;

        if (stack.Nested != null)
            return true;

        if (stack.Item.is_container && !string.IsNullOrEmpty(stack.Item.container_id))
            return false;

        int volumeMl = ResolveStorageVolumeMl(stack.Item);
        if (volumeMl <= 0)
            return false;

        float volumeLiters = volumeMl / 1000f;
        float weightKg = volumeLiters;

        var definition = new ContainerData
        {
            id = PocketIdPrefix + stack.Item.id,
            name = ItemNameTable.Get(
                stack.Item.id,
                LocalizationBundle.Get()?.ActiveLanguage ?? DisplayLanguage.Ko),
            max_weight = weightKg,
            max_volume = volumeLiters,
            draw_moves = PreferDrawMoves(stack.Item),
        };

        stack.AssignNested(InventoryContainer.Create(
            definition,
            nestedPolicy ?? new FixedContainerCapacityPolicy()));
        return true;
    }

    public static bool IsArmorPocketContainer(InventoryContainer container) =>
        container?.Definition != null
        && !string.IsNullOrEmpty(container.Definition.id)
        && container.Definition.id.StartsWith(PocketIdPrefix, System.StringComparison.Ordinal);
}
