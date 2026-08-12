// ============================================================
// WeaponActionVfxResolver — 무기 Entry VFX + Pipeline 동사 행 coalesce
// ============================================================

using UnityEngine;

public static class WeaponActionVfxResolver
{
    /// <summary>
    /// 무기 Presentation Entry 슬롯을 우선하고, null은 Pipeline 동사 행 VFX로 채운다.
    /// </summary>
    public static WeaponActionVfx Resolve(
        WeaponPresentation presentation,
        WeaponAction action,
        ArmAnimSlotCatalog pipeline)
    {
        WeaponActionVfx weapon = null;
        if (presentation != null &&
            presentation.TryGetEntry(action, out WeaponPresentation.Entry entry))
            weapon = entry?.vfx;

        WeaponActionVfx verb = null;
        if (pipeline != null)
            pipeline.TryGetVerbVfx(action, out verb);

        if (weapon == null && verb == null)
            return null;

        return new WeaponActionVfx
        {
            actionVfx = Coalesce(weapon?.actionVfx, verb?.actionVfx),
            tracerVfx = Coalesce(weapon?.tracerVfx, verb?.tracerVfx),
            hitVfx = Coalesce(weapon?.hitVfx, verb?.hitVfx),
            missVfx = Coalesce(weapon?.missVfx, verb?.missVfx)
        };
    }

    /// <summary>
    /// Attack SO 슬롯을 우선하고, null은 임팩트 태그 폴백으로 채운다.
    /// 태그가 비면 fallback 태그를 쓴다 (무음 금지).
    /// </summary>
    public static WeaponActionVfx ResolveImpact(
        WeaponAttack attack,
        WeaponImpactVfxDefaults defaults,
        string impactTag)
    {
        WeaponActionVfx weapon = attack != null ? attack.AttackVfx : null;
        string tag = impactTag;
        if (string.IsNullOrEmpty(tag) && attack != null)
            tag = attack.ImpactTag;
        if (string.IsNullOrEmpty(tag) && attack != null)
            tag = attack.FallbackImpactTag;
        if (string.IsNullOrEmpty(tag))
            tag = AttackImpactTags.Fallback;

        WeaponActionVfx tagVfx = null;
        if (defaults != null && !defaults.TryGetVfx(tag, out tagVfx))
            defaults.TryGetVfx(AttackImpactTags.Fallback, out tagVfx);

        if (weapon == null && tagVfx == null)
            return null;

        return new WeaponActionVfx
        {
            actionVfx = null,
            tracerVfx = Coalesce(weapon?.tracerVfx, tagVfx?.tracerVfx),
            hitVfx = Coalesce(weapon?.hitVfx, tagVfx?.hitVfx),
            missVfx = Coalesce(weapon?.missVfx, tagVfx?.missVfx)
        };
    }

    /// <summary>Pipeline Impact Kind 행 VFX.</summary>
    public static WeaponActionVfx ResolveImpactKind(
        ArmAnimSlotCatalog pipeline,
        ArmImpactKind kind)
    {
        if (pipeline == null || !pipeline.TryGetImpactVfx(kind, out WeaponActionVfx vfx))
            return null;
        return vfx;
    }

    static GameObject Coalesce(GameObject preferred, GameObject fallback) =>
        preferred != null ? preferred : fallback;
}
