// ============================================================
// PrimaryWieldResolver — DPS 최대 손 → SetWieldedItem / 최고 액션
// ============================================================

using Garunnir.Runtime.Gameplay.Data;
using UnityEngine;

public static class PrimaryWieldResolver
{
    static readonly WeaponAction[] ActionOrder =
    {
        WeaponAction.Bashing,
        WeaponAction.Cutting,
        WeaponAction.Gun
    };

    public struct HandScore
    {
        public WieldSlotId Slot;
        public ItemStack Stack;
        public WeaponAction? Action;
        public float Score;
        public bool IsOffHand;
    }

    /// <summary>
    /// Occupied 손별 score 최대. 동점 Right → Left.
    /// 양손은 스택 1개·액션 1개.
    /// </summary>
    public static bool TryResolvePrimary(
        WieldSlots slots,
        HandActionBinding bindings,
        ICharacterSkills skills,
        int loadedRounds,
        out HandScore primary,
        out HandScore secondary)
    {
        primary = default;
        secondary = default;
        if (slots == null)
            return false;

        if (slots.IsTwoHand)
        {
            ItemStack stack = slots.Left ?? slots.Right;
            if (stack?.Item == null)
                return false;

            WeaponAction? action = bindings?.EnsureInitialized(stack.Item, loadedRounds, skills);
            primary = new HandScore
            {
                Slot = WieldSlotId.Right,
                Stack = stack,
                Action = action,
                Score = ScoreHand(stack.Item, action, loadedRounds, skills, offHandFactor: 1f),
                IsOffHand = false
            };
            return action != null;
        }

        HandScore left = EvaluateSlot(
            WieldSlotId.Left, slots.Left, bindings, skills, loadedRounds);
        HandScore right = EvaluateSlot(
            WieldSlotId.Right, slots.Right, bindings, skills, loadedRounds);

        bool leftOk = left.Stack != null && left.Action != null;
        bool rightOk = right.Stack != null && right.Action != null;
        if (!leftOk && !rightOk)
            return false;

        if (leftOk && rightOk)
        {
            // 동점 Right 우선
            if (right.Score >= left.Score)
            {
                primary = right;
                secondary = left;
                secondary.IsOffHand = true;
                secondary.Score = ScoreHand(
                    secondary.Stack.Item,
                    secondary.Action,
                    loadedRounds,
                    skills,
                    OffHandFactor(skills, WieldHand.Left));
            }
            else
            {
                primary = left;
                secondary = right;
                secondary.IsOffHand = true;
                secondary.Score = ScoreHand(
                    secondary.Stack.Item,
                    secondary.Action,
                    loadedRounds,
                    skills,
                    OffHandFactor(skills, WieldHand.Right));
            }

            primary.IsOffHand = false;
            return true;
        }

        primary = rightOk ? right : left;
        primary.IsOffHand = false;
        return true;
    }

    public static WeaponAction? BestActionForItem(
        ItemData item,
        int loadedRounds,
        ICharacterSkills skills)
    {
        WeaponActionMask mask = CombatMath.AvailableModes(item);
        WeaponAction? best = null;
        float bestScore = -1f;

        for (int i = 0; i < ActionOrder.Length; i++)
        {
            WeaponAction action = ActionOrder[i];
            if ((mask & WeaponActionUtil.ToMask(action)) == 0)
                continue;
            if (action == WeaponAction.Gun && !CombatMath.CanFireGun(item, loadedRounds))
                continue;

            float score = Dps(item, action, loadedRounds, skills, 1f);
            if (score > bestScore)
            {
                bestScore = score;
                best = action;
            }
        }

        return best;
    }

    public static float OffHandFactor(ICharacterSkills skills, WieldHand hand)
    {
        if (skills == null)
            return GearConstants.OffHandDpsFactorMin;

        string skillId = HandProficiencyIds.ForHand(hand);
        return GearConstants.OffHandDpsFactor(skills.Level(skillId));
    }

    static HandScore EvaluateSlot(
        WieldSlotId slot,
        ItemStack stack,
        HandActionBinding bindings,
        ICharacterSkills skills,
        int loadedRounds)
    {
        if (stack?.Item == null)
            return default;

        WeaponAction? action = bindings?.EnsureInitialized(stack.Item, loadedRounds, skills);
        return new HandScore
        {
            Slot = slot,
            Stack = stack,
            Action = action,
            Score = ScoreHand(stack.Item, action, loadedRounds, skills, 1f),
            IsOffHand = false
        };
    }

    static float ScoreHand(
        ItemData item,
        WeaponAction? action,
        int loadedRounds,
        ICharacterSkills skills,
        float offHandFactor)
    {
        if (item == null || action == null)
            return 0f;
        return Dps(item, action.Value, loadedRounds, skills, offHandFactor);
    }

    static float Dps(
        ItemData item,
        WeaponAction action,
        int loadedRounds,
        ICharacterSkills skills,
        float offHandFactor)
    {
        if (action == WeaponAction.Gun && !CombatMath.CanFireGun(item, loadedRounds))
            return 0f;

        float interval = CombatMath.AttackIntervalSeconds(item, action);
        if (interval <= 0f)
            return 0f;

        int strength = skills != null ? skills.Level(AttributeIds.Str) : 8;
        string skillId = CombatMath.SkillId(item, action);
        int skillLevel = skills != null && !string.IsNullOrEmpty(skillId)
            ? skills.Level(skillId)
            : 0;
        int damage = CombatMath.Damage(item, action, strength, skillLevel);
        return damage / interval * Mathf.Max(0f, offHandFactor);
    }
}
