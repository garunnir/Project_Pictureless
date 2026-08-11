// ============================================================
// ArmAnimSlotResolver — 라이브러리 클립을 thin SM 키에 투영
// ============================================================

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 현재 WeaponAction으로 라이브러리 Hold/Aim/Attack 클립을 thin 슬롯에 주입한다.
/// 비전용은 해당 손 라이브러리 base. Aim/Attack 라이브러리 없으면 Hold thin.
/// </summary>
public static class ArmAnimSlotResolver
{
    /// <summary>
    /// weapon/base 위에 라이브러리 클립을 thin에 투영한 runtime override를 만든다.
    /// </summary>
    public static AnimatorOverrideController BuildResolvedOverride(
        RuntimeAnimatorController baseOrWeapon,
        ArmAnimSlotCatalog catalog,
        WeaponAction actionL,
        WeaponAction actionR,
        WeaponAction action2H)
    {
        if (baseOrWeapon == null || catalog == null)
            return null;

        RuntimeAnimatorController root = baseOrWeapon;
        AnimatorOverrideController weaponOvr = baseOrWeapon as AnimatorOverrideController;
        if (weaponOvr != null && weaponOvr.runtimeAnimatorController != null)
            root = weaponOvr.runtimeAnimatorController;

        var resolved = new AnimatorOverrideController(root);
        if (weaponOvr != null)
            CopyOverrides(weaponOvr, resolved);

        ProjectThinKeys(resolved, catalog, actionL, actionR, action2H);
        return resolved;
    }

    /// <summary>
    /// Presentation 고정 상태에서 Action만 바뀔 때 thin Aim/Attack 키만 갱신.
    /// </summary>
    public static void RemapThinKeys(
        AnimatorOverrideController resolved,
        ArmAnimSlotCatalog catalog,
        WeaponAction actionL,
        WeaponAction actionR,
        WeaponAction action2H)
    {
        if (resolved == null || catalog == null)
            return;

        ProjectThinKeys(resolved, catalog, actionL, actionR, action2H);
    }

    static void ProjectThinKeys(
        AnimatorOverrideController resolved,
        ArmAnimSlotCatalog catalog,
        WeaponAction actionL,
        WeaponAction actionR,
        WeaponAction action2H)
    {
        ArmAnimSlotCatalog.ActionLibraryEntry libL = catalog.FindAction(actionL);
        ArmAnimSlotCatalog.ActionLibraryEntry libR = catalog.FindAction(actionR);
        ArmAnimSlotCatalog.ActionLibraryEntry lib2H = catalog.FindAction(action2H);

        ProjectPose(resolved, catalog.HoldThin, libL?.hold, libR?.hold, lib2H?.hold, null);
        ProjectPose(resolved, catalog.AimThin, libL?.aim, libR?.aim, lib2H?.aim, catalog.HoldThin);
        ProjectPose(resolved, catalog.AttackThin, libL?.attack, libR?.attack, lib2H?.attack, catalog.HoldThin);
    }

    static void ProjectPose(
        AnimatorOverrideController resolved,
        ArmAnimSlotCatalog.HandClips thin,
        ArmAnimSlotCatalog.HandClips libL,
        ArmAnimSlotCatalog.HandClips libR,
        ArmAnimSlotCatalog.HandClips lib2H,
        ArmAnimSlotCatalog.HandClips poseFallback)
    {
        if (thin == null)
            return;

        if (thin.leftBase != null)
            resolved[thin.leftBase] = PickLibClip(resolved, libL, WieldHand.Left, poseFallback);
        if (thin.rightBase != null)
            resolved[thin.rightBase] = PickLibClip(resolved, libR, WieldHand.Right, poseFallback);
        if (thin.twoHandBase != null)
            resolved[thin.twoHandBase] = PickLibClip(resolved, lib2H, WieldHand.TwoHand, poseFallback);
    }

    static AnimationClip PickLibClip(
        AnimatorOverrideController resolved,
        ArmAnimSlotCatalog.HandClips lib,
        WieldHand hand,
        ArmAnimSlotCatalog.HandClips poseFallback)
    {
        AnimationClip baseClip = null;
        if (lib != null)
        {
            if (hand == WieldHand.Left) baseClip = lib.leftBase;
            else if (hand == WieldHand.Right) baseClip = lib.rightBase;
            else baseClip = lib.twoHandBase;
        }

        AnimationClip clip = EffectiveClip(baseClip, resolved);
        if (clip != null)
            return clip;

        if (poseFallback == null)
            return null;
        if (hand == WieldHand.Left) return EffectiveClip(poseFallback.leftBase, resolved);
        if (hand == WieldHand.Right) return EffectiveClip(poseFallback.rightBase, resolved);
        return EffectiveClip(poseFallback.twoHandBase, resolved);
    }

    public static AnimationClip EffectiveClip(AnimationClip baseClip, AnimatorOverrideController overrideController)
    {
        if (baseClip == null)
            return null;
        if (overrideController == null)
            return baseClip;
        AnimationClip mapped = overrideController[baseClip];
        return mapped != null ? mapped : baseClip;
    }

    static void CopyOverrides(AnimatorOverrideController from, AnimatorOverrideController to)
    {
        var pairs = new List<KeyValuePair<AnimationClip, AnimationClip>>();
        from.GetOverrides(pairs);
        to.ApplyOverrides(pairs);
    }
}
