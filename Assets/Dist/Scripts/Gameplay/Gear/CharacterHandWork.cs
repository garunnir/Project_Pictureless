// ============================================================
// CharacterHandWork — 손 비움 → 들기 → 행위 (순차, ESC는 현재 단계까지)
// ============================================================
// flowchart LR
//   start[TryBegin]
//   stow[Unwield_to_body]
//   draw[Wield_from_source]
//   act[Act_duration]
//   apply[apply]
//   start --> stow --> draw --> act --> apply

using System;
using System.Collections.Generic;

/// <summary>
/// 손에 든 다른 스택은 body로 넣고, 대상을 든 뒤 actDuration 후 apply.
/// 완료된 단계는 유지. ESC=<see cref="CharacterActionHost.CancelAll"/> — 진행 중 단계는 apply 없이 중단, 큐 폐기.
/// </summary>
public static class CharacterHandWork
{
    static readonly List<ItemStack> OccupiedScratch = new(2);

    public static WieldHand DefaultHand(ItemStack stack)
    {
        if (stack?.Item != null && GearHandleRules.IsTwoHandWeapon(stack.Item))
            return WieldHand.TwoHand;
        return WieldHand.Right;
    }

    public static string GetBlockedReason(
        ItemStack stack,
        InventoryContainer source,
        WieldHand hand)
    {
        CharacterGearService gear = PlayerGearHost.Active?.Service;
        if (gear == null || stack?.Item == null)
            return CharacterGearLabels.BlockedInvalid;
        if (gear.ToolSession.IsActive)
            return CharacterGearLabels.BlockedToolSession;

        CollectOccupiedExcept(gear.Wield, stack, OccupiedScratch);
        for (int i = 0; i < OccupiedScratch.Count; i++)
        {
            if (!gear.CanDepositToBody(OccupiedScratch[i]))
                return CharacterGearLabels.BlockedNoStowRoom;
        }

        if (gear.Wield.Contains(stack))
            return null;

        if (source == null || !source.ContainsStackReference(stack))
            return CharacterGearLabels.BlockedInvalid;

        return gear.GetWieldBlockedReason(stack, hand);
    }

    public static bool TryBegin(
        ItemStack stack,
        InventoryContainer source,
        WieldHand hand,
        float actDurationSeconds,
        Action apply)
    {
        if (apply == null || GetBlockedReason(stack, source, hand) != null)
            return false;

        CharacterGearService gear = PlayerGearHost.Active?.Service;
        if (gear == null)
            return false;

        CollectOccupiedExcept(gear.Wield, stack, OccupiedScratch);
        for (int i = 0; i < OccupiedScratch.Count; i++)
            gear.TryBeginUnwield(OccupiedScratch[i], toFloor: false);

        if (!gear.Wield.Contains(stack))
            gear.TryBeginWield(stack, source, hand);

        InventoryTimedMoveHost move = InventoryTimedMoveHost.Active;
        if (move != null)
            return move.TryBegin(actDurationSeconds, apply, stack);

        return gear.TryBeginDomainTimed(
            stack,
            GearTimedAction.Kind.InventoryTransfer,
            actDurationSeconds,
            apply);
    }

    static void CollectOccupiedExcept(
        WieldSlots wield,
        ItemStack keep,
        List<ItemStack> dest)
    {
        dest.Clear();
        if (wield == null)
            return;

        AddUniqueExcept(dest, wield.Left, keep);
        AddUniqueExcept(dest, wield.Right, keep);
    }

    static void AddUniqueExcept(List<ItemStack> dest, ItemStack stack, ItemStack keep)
    {
        if (stack == null || ReferenceEquals(stack, keep))
            return;

        for (int i = 0; i < dest.Count; i++)
        {
            if (ReferenceEquals(dest[i], stack))
                return;
        }

        dest.Add(stack);
    }
}
