// ============================================================
// ItemMergeKey — 병합 비교용 얕은 스냅샷. 정책은 ItemMergePolicy
// ============================================================

using System;
using System.Collections.Generic;
using Garunnir.Runtime.Gameplay.Data;

/// <summary>
/// Nested를 열지 않는다. HasMagazine은 한 단 유무만.
/// </summary>
public readonly struct ItemMergeKey
{
    public readonly string KindId;
    public readonly int DamageLevel;
    public readonly int ChamberRounds;
    public readonly string ChamberAmmoId;
    public readonly bool HasMagazine;
    public readonly int ToolCharges;

    public ItemMergeKey(
        string kindId,
        int damageLevel,
        int chamberRounds,
        bool hasMagazine,
        string chamberAmmoId = null,
        int toolCharges = 0)
    {
        KindId = kindId ?? string.Empty;
        DamageLevel = Math.Max(0, damageLevel);
        ChamberRounds = Math.Max(0, chamberRounds);
        ChamberAmmoId = chamberAmmoId ?? string.Empty;
        HasMagazine = hasMagazine;
        ToolCharges = Math.Max(0, toolCharges);
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
            HasMagazineShallow(stack),
            instance.ChamberAmmoId,
            instance.ToolCharges);
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

    static bool HasMagazineShallow(ItemStack stack)
    {
        InventoryContainer nested = stack.Nested;
        if (nested == null)
            return false;

        IReadOnlyList<ItemStack> stacks = nested.Stacks;
        for (int i = 0; i < stacks.Count; i++)
        {
            if (stacks[i]?.Item?.magazine != null)
                return true;
        }

        return false;
    }
}
