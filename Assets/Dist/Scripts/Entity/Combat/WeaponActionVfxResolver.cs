// ============================================================
// WeaponActionVfxResolver — 무기 Entry VFX + 태그 기본 슬롯 coalesce
// ============================================================

using UnityEngine;

public static class WeaponActionVfxResolver
{
    /// <summary>
    /// 무기 Presentation Entry 슬롯을 우선하고, null은 태그 기본으로 채운다.
    /// Entry가 없으면 태그 기본만 사용. 둘 다 없으면 null.
    /// </summary>
    public static WeaponActionVfx Resolve(
        WeaponPresentation presentation,
        WeaponAction action,
        WeaponActionVfxDefaults defaults)
    {
        WeaponActionVfx weapon = null;
        if (presentation != null &&
            presentation.TryGetEntry(action, out WeaponPresentation.Entry entry))
            weapon = entry?.vfx;

        WeaponActionVfx tag = null;
        if (defaults != null)
            defaults.TryGetVfx(action, out tag);

        if (weapon == null && tag == null)
            return null;

        return new WeaponActionVfx
        {
            actionVfx = Coalesce(weapon?.actionVfx, tag?.actionVfx),
            tracerVfx = Coalesce(weapon?.tracerVfx, tag?.tracerVfx),
            hitVfx = Coalesce(weapon?.hitVfx, tag?.hitVfx),
            missVfx = Coalesce(weapon?.missVfx, tag?.missVfx)
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

    static GameObject Coalesce(GameObject preferred, GameObject fallback) =>
        preferred != null ? preferred : fallback;
}
