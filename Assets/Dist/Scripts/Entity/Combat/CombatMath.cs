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
            return WeaponActionMask.Bashing;

        WeaponActionMask mask = WeaponActionMask.None;
        if (item.bashing > 0)
            mask |= WeaponActionMask.Bashing;
        if (item.cutting > 0)
            mask |= WeaponActionMask.Cutting;
        if (item.gun != null)
            mask |= WeaponActionMask.Gun;

        return mask == WeaponActionMask.None
            ? WeaponActionMask.Bashing
            : mask;
    }

    public static string SkillId(ItemData item, WeaponAction action)
    {
        if (action == WeaponAction.Gun)
            return CombatSkillIds.Gun;

        if (item == null)
            return CombatSkillIds.Unarmed;

        return CombatSkillIds.Melee;
    }

    public static int PracticeXp(WeaponAction action) => PracticeXpPerAttack;

    public static float RangeMeters(ItemData item, WeaponAction action)
    {
        if (action == WeaponAction.Gun)
        {
            int range = item?.gun != null ? item.gun.range : 0;
            return range > 0 ? range : MeleeReachMeters;
        }

        return MeleeReachMeters;
    }

    public static int AttackMoves(ItemData item, WeaponAction action)
    {
        if (action == WeaponAction.Gun)
            return GunFireMoves;

        int weight = item != null ? Mathf.Max(0, item.weight_g) : 0;
        int volume = item != null ? Mathf.Max(0, item.volume_ml) : 0;
        return MeleeMovesBase
            + volume / MeleeMovesVolumeDiv
            + weight / MeleeMovesWeightDiv;
    }

    public static float AttackIntervalSeconds(ItemData item, WeaponAction action) =>
        AttackMoves(item, action) / MovesPerSecond;

    public static int Damage(
        ItemData item,
        WeaponAction action,
        int strength,
        int skillLevel)
    {
        if (action == WeaponAction.Gun)
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

        int baseDmg = action == WeaponAction.Cutting
            ? item.cutting
            : item.bashing;
        int strBonus = Mathf.Max(0, (strength - StrengthBaseline) / 2);
        return Mathf.Max(0, baseDmg + strBonus + skillLevel / 4);
    }

    public static float HitChance(
        ItemData item,
        WeaponAction action,
        int skillLevel,
        string aimedPartId)
    {
        float chance;
        if (action == WeaponAction.Gun)
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

    /// <summary>탄약/장전 시스템 전: loadedRounds는 Attacker 스텁. 자동 장전 없음.</summary>
    public static bool CanFireGun(ItemData item, int loadedRounds) =>
        item?.gun != null && loadedRounds > 0;
}
