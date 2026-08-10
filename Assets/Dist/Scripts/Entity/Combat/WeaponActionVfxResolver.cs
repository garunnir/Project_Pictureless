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

    static GameObject Coalesce(GameObject preferred, GameObject fallback) =>
        preferred != null ? preferred : fallback;
}
