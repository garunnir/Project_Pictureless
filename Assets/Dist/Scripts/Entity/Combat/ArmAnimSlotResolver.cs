// ============================================================
// ArmAnimSlotResolver — Pipeline 라이브러리 → thin 투영 (컨트롤러는 동작 모름)
// ============================================================

using UnityEngine;

/// <summary>
/// AnimVerb 클립은 Pipeline(Catalog)에만 있다. SM thin에 투영한다.
/// 무기 Override는 thin 덮어쓰기(분류 아님). 레거시 라이브러리 키 매핑도 읽는다.
/// </summary>
public static class ArmAnimSlotResolver
{
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
        ProjectThinKeys(resolved, weaponOvr, catalog, actionL, actionR, action2H);
        return resolved;
    }

    public static void RemapThinKeys(
        AnimatorOverrideController resolved,
        RuntimeAnimatorController weaponSource,
        ArmAnimSlotCatalog catalog,
        WeaponAction actionL,
        WeaponAction actionR,
        WeaponAction action2H)
    {
        if (resolved == null || catalog == null)
            return;

        var weaponOvr = weaponSource as AnimatorOverrideController;
        ProjectThinKeys(resolved, weaponOvr, catalog, actionL, actionR, action2H);
    }

    /// <summary>구 API — weaponSource 없이 Remap (thin Override 유지 불가).</summary>
    public static void RemapThinKeys(
        AnimatorOverrideController resolved,
        ArmAnimSlotCatalog catalog,
        WeaponAction actionL,
        WeaponAction actionR,
        WeaponAction action2H)
    {
        RemapThinKeys(resolved, null, catalog, actionL, actionR, action2H);
    }

    static void ProjectThinKeys(
        AnimatorOverrideController resolved,
        AnimatorOverrideController weaponOvr,
        ArmAnimSlotCatalog catalog,
        WeaponAction actionL,
        WeaponAction actionR,
        WeaponAction action2H)
    {
        ArmAnimSlotCatalog.ActionLibraryEntry libL = catalog.FindAction(actionL);
        ArmAnimSlotCatalog.ActionLibraryEntry libR = catalog.FindAction(actionR);
        ArmAnimSlotCatalog.ActionLibraryEntry lib2H = catalog.FindAction(action2H);

        ProjectPose(resolved, weaponOvr, catalog.HoldThin, libL?.hold, libR?.hold, lib2H?.hold, null);
        ProjectPose(
            resolved, weaponOvr, catalog.AimThin, libL?.aim, libR?.aim, lib2H?.aim, catalog.HoldThin);
        ProjectPose(
            resolved,
            weaponOvr,
            catalog.AttackThin,
            libL?.attack,
            libR?.attack,
            lib2H?.attack,
            catalog.HoldThin);
    }

    static void ProjectPose(
        AnimatorOverrideController resolved,
        AnimatorOverrideController weaponOvr,
        ArmAnimSlotCatalog.HandClips thin,
        ArmAnimSlotCatalog.HandClips libL,
        ArmAnimSlotCatalog.HandClips libR,
        ArmAnimSlotCatalog.HandClips lib2H,
        ArmAnimSlotCatalog.HandClips poseFallback)
    {
        if (thin == null)
            return;

        if (thin.leftBase != null)
            resolved[thin.leftBase] = ResolveClip(
                weaponOvr, thin.leftBase, LibHand(libL, WieldHand.Left), poseFallback, WieldHand.Left);
        if (thin.rightBase != null)
            resolved[thin.rightBase] = ResolveClip(
                weaponOvr, thin.rightBase, LibHand(libR, WieldHand.Right), poseFallback, WieldHand.Right);
        if (thin.twoHandBase != null)
            resolved[thin.twoHandBase] = ResolveClip(
                weaponOvr,
                thin.twoHandBase,
                LibHand(lib2H, WieldHand.TwoHand),
                poseFallback,
                WieldHand.TwoHand);
    }

    static AnimationClip LibHand(ArmAnimSlotCatalog.HandClips lib, WieldHand hand)
    {
        if (lib == null)
            return null;
        if (hand == WieldHand.Left)
            return lib.leftBase;
        if (hand == WieldHand.Right)
            return lib.rightBase;
        return lib.twoHandBase;
    }

    static AnimationClip ResolveClip(
        AnimatorOverrideController weaponOvr,
        AnimationClip thinClip,
        AnimationClip libClip,
        ArmAnimSlotCatalog.HandClips poseFallback,
        WieldHand hand)
    {
        AnimationClip fromThin = ReadNonIdentity(weaponOvr, thinClip);
        if (fromThin != null)
            return fromThin;

        AnimationClip fromLib = ReadNonIdentity(weaponOvr, libClip);
        if (fromLib != null)
            return fromLib;

        if (libClip != null)
            return libClip;

        if (poseFallback == null)
            return thinClip;
        AnimationClip fallback =
            hand == WieldHand.Left ? poseFallback.leftBase :
            hand == WieldHand.Right ? poseFallback.rightBase :
            poseFallback.twoHandBase;
        return ReadNonIdentity(weaponOvr, fallback) ?? fallback ?? thinClip;
    }

    static AnimationClip ReadNonIdentity(AnimatorOverrideController ovr, AnimationClip original)
    {
        if (ovr == null || original == null)
            return null;
        AnimationClip mapped = ovr[original];
        if (mapped == null || mapped == original)
            return null;
        return mapped;
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
}
