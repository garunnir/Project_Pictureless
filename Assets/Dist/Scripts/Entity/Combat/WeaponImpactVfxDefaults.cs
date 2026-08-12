// ============================================================
// WeaponImpactVfxDefaults — 임팩트 태그별 hit/miss/tracer 폴백
// ============================================================

using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "WeaponImpactVfxDefaults",
    menuName = "Dist/Combat/Weapon Impact Vfx Defaults")]
public sealed class WeaponImpactVfxDefaults : ScriptableObject
{
    [Serializable]
    public sealed class Entry
    {
        public string impactTag = AttackImpactTags.Fallback;
        public WeaponActionVfx vfx = new WeaponActionVfx();
    }

    [SerializeField] Entry[] _entries = Array.Empty<Entry>();

    public Entry[] Entries => _entries;

    public bool TryGetVfx(string impactTag, out WeaponActionVfx vfx)
    {
        vfx = null;
        if (_entries == null || string.IsNullOrEmpty(impactTag))
            return false;

        for (int i = 0; i < _entries.Length; i++)
        {
            Entry entry = _entries[i];
            if (entry == null ||
                entry.vfx == null ||
                !string.Equals(entry.impactTag, impactTag, StringComparison.Ordinal))
                continue;
            vfx = entry.vfx;
            return true;
        }

        return false;
    }
}
