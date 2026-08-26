// ============================================================
// ItemMergeKey — 병합 비교용 얕은 스냅샷. 정책은 ItemMergePolicy
// ============================================================

using System;
using Garunnir.Runtime.Gameplay.Data;

/// <summary>
/// Nested를 열지 않는다. HasMagazine은 LoadedMagazine 유무.
/// </summary>
public readonly struct ItemMergeKey
{
    public readonly string KindId;
    public readonly int DamageLevel;
    public readonly int ChamberRounds;
    public readonly string ChamberAmmoId;
    public readonly bool HasMagazine;
    public readonly int SupplyRounds;
    public readonly string SupplyAmmoId;
    public readonly int ToolCharges;
    public readonly bool IsRotten;
    public readonly bool IsCooked;
    public readonly bool IsHot;

    public ItemMergeKey(
        string kindId,
        int damageLevel,
        int chamberRounds,
        bool hasMagazine,
        string chamberAmmoId = null,
        int toolCharges = 0,
        int supplyRounds = 0,
        string supplyAmmoId = null,
        bool isRotten = false,
        bool isCooked = false,
        bool isHot = false)
    {
        KindId = kindId ?? string.Empty;
        DamageLevel = Math.Max(0, damageLevel);
        ChamberRounds = Math.Max(0, chamberRounds);
        ChamberAmmoId = chamberAmmoId ?? string.Empty;
        HasMagazine = hasMagazine;
        SupplyRounds = Math.Max(0, supplyRounds);
        SupplyAmmoId = supplyAmmoId ?? string.Empty;
        ToolCharges = Math.Max(0, toolCharges);
        IsRotten = isRotten;
        IsCooked = isCooked;
        IsHot = isHot;
    }

    public static ItemMergeKey From(ItemStack stack)
    {
        if (stack?.Instance == null)
            return default;

        ItemInstance instance = stack.Instance;
        return new ItemMergeKey(
            stack.ItemId,
            instance.DamageLevel,
            instance.ChamberRounds,
            stack.LoadedMagazine != null,
            instance.ChamberAmmoId,
            instance.ToolCharges,
            instance.SupplyRounds,
            instance.SupplyAmmoId,
            instance.IsRotten,
            instance.IsCooked,
            instance.IsHot);
    }

    public static ItemMergeKey From(ItemData item, int damageLevel)
    {
        int toolCharges = 0;
        if (item?.tool != null)
            toolCharges = Math.Max(0, item.tool.initial_charges);

        return new ItemMergeKey(
            item != null ? item.id : string.Empty,
            damageLevel,
            chamberRounds: 0,
            hasMagazine: false,
            toolCharges: toolCharges);
    }
}
