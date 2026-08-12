// ============================================================
// WeaponActionVfxDefaults — WeaponAction 태그별 기본 VFX (폴백 SSOT)
// ============================================================

using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "WeaponActionVfxDefaults",
    menuName = "Dist/Combat/Weapon Action Vfx Defaults")]
public sealed class WeaponActionVfxDefaults : ScriptableObject
{
    [Serializable]
    public sealed class Entry
    {
        public WeaponAction action = WeaponAction.Swing;
        public WeaponActionVfx vfx = new();
    }

    [SerializeField] Entry[] _entries = Array.Empty<Entry>();

    public Entry[] Entries => _entries;

    public bool TryGetVfx(WeaponAction action, out WeaponActionVfx vfx)
    {
        vfx = null;
        if (_entries == null)
            return false;

        for (int i = 0; i < _entries.Length; i++)
        {
            Entry entry = _entries[i];
            if (entry == null || entry.vfx == null)
                continue;
            if (WeaponActionUtil.Normalize(entry.action) != WeaponActionUtil.Normalize(action))
                continue;
            vfx = entry.vfx;
            return true;
        }

        return false;
    }
}
