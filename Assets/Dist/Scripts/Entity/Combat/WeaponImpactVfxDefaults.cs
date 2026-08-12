// ============================================================
// WeaponImpactVfxDefaults — Hit 특성 키(bash/cut/bullet) hit/miss/tracer
// ============================================================

using System;
using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(
    fileName = "WeaponImpactVfxDefaults",
    menuName = "Dist/Combat/Weapon Impact Vfx Defaults")]
public sealed class WeaponImpactVfxDefaults : ScriptableObject
{
    [Serializable]
    public sealed class Entry
    {
        [HorizontalGroup("Row", Width = 120)]
        [LabelText("Hit")]
        public string impactTag = AttackImpactTags.Fallback;

        [HorizontalGroup("Row")]
        [HideLabel]
        public WeaponActionVfx vfx = new WeaponActionVfx();
    }

    [InfoBox(
        "Hit 특성 키(bash/cut/bullet) → hit/miss/tracer. 없으면 fallback. Recoil/Blocked(Reaction)과 다름.",
        InfoMessageType.None)]
    [ListDrawerSettings(ShowFoldout = true, ListElementLabelName = "impactTag")]
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
