// ============================================================
// ArmAnimSlotResolver — 라이브러리 전용/폴백 resolve 후 thin SM 키에 투영
// ============================================================

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 라이브러리 키(Aim{Action}/Attack{Action}/Hold)에서 전용·대칭 폴백을 풀고,
/// 현재 WeaponAction으로 thin Aim/Attack/Hold 슬롯에 주입한다.
/// TwoHand는 폴백 없음. Dominant(현재 Right)는 양쪽 비전용일 때 기준.
/// </summary>
public static class ArmAnimSlotResolver
{
    public static WieldHand DominantHand => WieldHand.Right;

    public struct ResolveResult
    {
        public AnimationClip Clip;
        public bool UsedFallback;
    }

    /// <summary>
    /// weapon/base 위에 라이브러리 resolve + thin 투영한 runtime override를 만든다.
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

        ApplyLibraryFallbacks(resolved, catalog, weaponOvr);
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

    static void ApplyLibraryFallbacks(
        AnimatorOverrideController resolved,
        ArmAnimSlotCatalog catalog,
        AnimatorOverrideController weaponOvr)
    {
        ApplyHandClipsFallback(resolved, catalog.Hold, weaponOvr);

        ArmAnimSlotCatalog.ActionLibraryEntry[] actions = catalog.Actions;
        if (actions == null)
            return;

        for (int i = 0; i < actions.Length; i++)
        {
            ArmAnimSlotCatalog.ActionLibraryEntry e = actions[i];
            if (e == null)
                continue;
            ApplyHandClipsFallback(resolved, e.aim, weaponOvr);
            ApplyHandClipsFallback(resolved, e.attack, weaponOvr);
        }
    }

    static void ApplyHandClipsFallback(
        AnimatorOverrideController resolved,
        ArmAnimSlotCatalog.HandClips clips,
        AnimatorOverrideController weaponOvr)
    {
        if (clips == null)
            return;

        ResolveResult left = ResolveForHand(
            WieldHand.Left, clips.leftBase, clips.rightBase, clips.leftFallback, clips.rightFallback, weaponOvr);
        ResolveResult right = ResolveForHand(
            WieldHand.Right, clips.leftBase, clips.rightBase, clips.leftFallback, clips.rightFallback, weaponOvr);

        if (clips.leftBase != null && left.Clip != null)
            resolved[clips.leftBase] = left.Clip;
        if (clips.rightBase != null && right.Clip != null)
            resolved[clips.rightBase] = right.Clip;

        if (clips.twoHandBase != null)
        {
            AnimationClip two = EffectiveClip(clips.twoHandBase, weaponOvr);
            if (two != null)
                resolved[clips.twoHandBase] = two;
        }
    }

    static void ProjectThinKeys(
        AnimatorOverrideController resolved,
        ArmAnimSlotCatalog catalog,
        WeaponAction actionL,
        WeaponAction actionR,
        WeaponAction action2H)
    {
        ProjectHold(resolved, catalog.Hold, catalog.Hold);

        ArmAnimSlotCatalog.ActionLibraryEntry libL = catalog.FindAction(actionL);
        ArmAnimSlotCatalog.ActionLibraryEntry libR = catalog.FindAction(actionR);
        ArmAnimSlotCatalog.ActionLibraryEntry lib2H = catalog.FindAction(action2H);

        ProjectPose(resolved, catalog.AimThin, libL?.aim, libR?.aim, lib2H?.aim, catalog.Hold);
        ProjectPose(resolved, catalog.AttackThin, libL?.attack, libR?.attack, lib2H?.attack, catalog.Hold);
    }

    static void ProjectHold(
        AnimatorOverrideController resolved,
        ArmAnimSlotCatalog.HandClips thin,
        ArmAnimSlotCatalog.HandClips holdLib)
    {
        if (thin == null || holdLib == null)
            return;

        if (thin.leftBase != null)
            resolved[thin.leftBase] = EffectiveClip(holdLib.leftBase, resolved);
        if (thin.rightBase != null)
            resolved[thin.rightBase] = EffectiveClip(holdLib.rightBase, resolved);
        if (thin.twoHandBase != null)
            resolved[thin.twoHandBase] = EffectiveClip(holdLib.twoHandBase, resolved);
    }

    static void ProjectPose(
        AnimatorOverrideController resolved,
        ArmAnimSlotCatalog.HandClips thin,
        ArmAnimSlotCatalog.HandClips libL,
        ArmAnimSlotCatalog.HandClips libR,
        ArmAnimSlotCatalog.HandClips lib2H,
        ArmAnimSlotCatalog.HandClips holdFallback)
    {
        if (thin == null)
            return;

        if (thin.leftBase != null)
            resolved[thin.leftBase] = PickLibClip(resolved, libL, WieldHand.Left, holdFallback);
        if (thin.rightBase != null)
            resolved[thin.rightBase] = PickLibClip(resolved, libR, WieldHand.Right, holdFallback);
        if (thin.twoHandBase != null)
            resolved[thin.twoHandBase] = PickLibClip(resolved, lib2H, WieldHand.TwoHand, holdFallback);
    }

    static AnimationClip PickLibClip(
        AnimatorOverrideController resolved,
        ArmAnimSlotCatalog.HandClips lib,
        WieldHand hand,
        ArmAnimSlotCatalog.HandClips holdFallback)
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

        if (holdFallback == null)
            return null;
        if (hand == WieldHand.Left) return EffectiveClip(holdFallback.leftBase, resolved);
        if (hand == WieldHand.Right) return EffectiveClip(holdFallback.rightBase, resolved);
        return EffectiveClip(holdFallback.twoHandBase, resolved);
    }

    public static ResolveResult ResolveForHand(
        WieldHand hand,
        AnimationClip leftBase,
        AnimationClip rightBase,
        AnimationClip leftFallback,
        AnimationClip rightFallback,
        AnimatorOverrideController overrideController)
    {
        if (hand != WieldHand.Left && hand != WieldHand.Right)
        {
            return new ResolveResult { Clip = null, UsedFallback = false };
        }

        AnimationClip ownBase = hand == WieldHand.Left ? leftBase : rightBase;
        AnimationClip otherBase = hand == WieldHand.Left ? rightBase : leftBase;
        AnimationClip ownFallback = hand == WieldHand.Left ? leftFallback : rightFallback;

        if (IsDedicated(ownBase, overrideController))
        {
            return new ResolveResult
            {
                Clip = EffectiveClip(ownBase, overrideController),
                UsedFallback = false
            };
        }

        if (IsDedicated(otherBase, overrideController))
        {
            AnimationClip fb = ownFallback != null
                ? ownFallback
                : EffectiveClip(otherBase, overrideController);
            return new ResolveResult
            {
                Clip = fb,
                UsedFallback = true
            };
        }

        if (hand == DominantHand)
        {
            return new ResolveResult
            {
                Clip = ownBase,
                UsedFallback = false
            };
        }

        AnimationClip dominantFallback = ownFallback != null
            ? ownFallback
            : (DominantHand == WieldHand.Right ? rightBase : leftBase);
        return new ResolveResult
        {
            Clip = dominantFallback != null ? dominantFallback : ownBase,
            UsedFallback = dominantFallback != null
        };
    }

    public static bool IsDedicated(AnimationClip baseClip, AnimatorOverrideController overrideController)
    {
        if (baseClip == null || overrideController == null)
            return false;

        AnimationClip mapped = overrideController[baseClip];
        if (mapped == null)
            return false;

        return !ReferenceEquals(mapped, baseClip);
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
