// ============================================================
// CombatMath — ItemData·스탯 기반 데미지/공속/명중 가산 파이프
// ============================================================

using Garunnir.Runtime.Gameplay.Data;
using UnityEngine;

/// <summary>
/// 전투 수치 SSOT. CharacterAttacker는 입력·쿨·타겟만 담당하고 식은 여기만.
/// mutation/무술 등은 같은 파이프에 가산만 얹는다 (클래스 상속 금지).
/// </summary>
public static class CombatMath
{
    /// <summary>BN moves → 실시간 초. 100 moves = 1초.</summary>
    public const float MovesPerSecond = 100f;

    /// <summary>근접 기본 사거리 (월드 미터). ItemData에 melee range 필드 없음.</summary>
    public const float MeleeReachMeters = 1.25f;

    const int UnarmedBaseDamage = 1;
    const int StrengthBaseline = 8;
    const float MeleeHitBase = 0.55f;
    const float ToHitPerPoint = 0.04f;
    const float SkillHitPerLevel = 0.01f;
    const float GunDispersionScale = 1000f;
    const int MeleeMovesBase = 65;
    const int MeleeMovesVolumeDiv = 250;
    const int MeleeMovesWeightDiv = 60;
    const int GunFireMoves = 100;
    const int PracticeXpPerAttack = 4;

    public static WeaponActionMask AvailableModes(ItemData item)
    {
        if (item == null)
            return WeaponActionMask.Swing;

        WeaponActionMask mask = WeaponActionMask.None;
        if (item.bashing > 0 || item.cutting > 0)
            mask |= WeaponActionMask.Swing;
        if (item.gun != null)
            mask |= WeaponActionMask.Trigger;

        return mask == WeaponActionMask.None
            ? WeaponActionMask.Swing
            : mask;
    }

    public static string SkillId(ItemData item, WeaponAction action)
    {
        if (WeaponActionUtil.Normalize(action) == WeaponAction.Trigger)
            return CombatSkillIds.Gun;

        if (item == null)
            return CombatSkillIds.Unarmed;

        return CombatSkillIds.Melee;
    }

    public static int PracticeXp(WeaponAction action) => PracticeXpPerAttack;

    public static float RangeMeters(ItemData item, WeaponAction action)
    {
        if (WeaponActionUtil.Normalize(action) == WeaponAction.Trigger)
        {
            int range = item?.gun != null ? item.gun.range : 0;
            return range > 0 ? range : MeleeReachMeters;
        }

        return MeleeReachMeters;
    }

    public static int AttackMoves(ItemData item, WeaponAction action)
    {
        if (WeaponActionUtil.Normalize(action) == WeaponAction.Trigger)
            return GunFireMoves;

        int weight = item != null ? Mathf.Max(0, item.weight_g) : 0;
        int volume = item != null ? Mathf.Max(0, item.volume_ml) : 0;
        return MeleeMovesBase
            + volume / MeleeMovesVolumeDiv
            + weight / MeleeMovesWeightDiv;
    }

    public static float AttackIntervalSeconds(ItemData item, WeaponAction action) =>
        AttackMoves(item, action) / MovesPerSecond;

    [System.Obsolete("Use Damage(item, attack, ...) or DamageForTag. Action enum is not bash/cut SSOT.")]
    public static int Damage(
        ItemData item,
        WeaponAction action,
        int strength,
        int skillLevel) =>
        DamageForTag(item, AttackDamageTags.DefaultFor(action), strength, skillLevel);

    public static int Damage(
        ItemData item,
        WeaponAttack attack,
        WeaponAction action,
        int strength,
        int skillLevel)
    {
        string tag = attack != null
            ? attack.DamageTag
            : AttackDamageTags.DefaultFor(action);
        return DamageForTag(item, tag, strength, skillLevel);
    }

    /// <summary>Attack SO 페이로드 태그 기준 피해. Action enum으로 bash/cut을 고르지 않음.</summary>
    public static int DamageForTag(
        ItemData item,
        string damageTag,
        int strength,
        int skillLevel)
    {
        if (string.Equals(damageTag, AttackDamageTags.Bullet, System.StringComparison.Ordinal))
        {
            int gunDmg = item?.gun != null ? item.gun.ranged_damage : 0;
            return Mathf.Max(0, gunDmg + skillLevel / 2);
        }

        if (item == null)
        {
            return Mathf.Max(
                UnarmedBaseDamage,
                strength / 2 + skillLevel / 2);
        }

        int baseDmg = string.Equals(damageTag, AttackDamageTags.Cut, System.StringComparison.Ordinal)
            ? item.cutting
            : item.bashing;
        int strBonus = Mathf.Max(0, (strength - StrengthBaseline) / 2);
        return Mathf.Max(0, baseDmg + strBonus + skillLevel / 4);
    }

    public static string SkillIdForTag(ItemData item, string damageTag)
    {
        if (string.Equals(damageTag, AttackDamageTags.Bullet, System.StringComparison.Ordinal))
            return CombatSkillIds.Gun;

        if (item == null)
            return CombatSkillIds.Unarmed;

        return CombatSkillIds.Melee;
    }

    public static float HitChance(
        ItemData item,
        WeaponAction action,
        int skillLevel,
        string aimedPartId)
    {
        float chance;
        if (WeaponActionUtil.Normalize(action) == WeaponAction.Trigger)
        {
            int dispersion = item?.gun != null ? item.gun.dispersion : 300;
            chance = Mathf.Clamp01(1f - dispersion / GunDispersionScale)
                + skillLevel * SkillHitPerLevel;
        }
        else
        {
            int toHit = item != null ? item.to_hit : 0;
            chance = MeleeHitBase
                + toHit * ToHitPerPoint
                + skillLevel * SkillHitPerLevel;
        }

        return Mathf.Clamp01(chance * BodyPartHitDifficulty.Get(aimedPartId));
    }

    /// <summary>약실에 1발 이상. 메거진 보급·자동 장전은 WeaponChamber.</summary>
    public static bool CanFireGun(ItemData item, ItemInstance instance) =>
        item?.gun != null && instance != null && instance.ChamberRounds > 0;
}
