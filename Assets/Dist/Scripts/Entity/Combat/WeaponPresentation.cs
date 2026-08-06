// ============================================================
// WeaponPresentation — 무기별 연출 SO (VFX·애니·효과 시드)
// ============================================================

using System;
using Garunnir.Runtime.Gameplay.Data;
using UnityEngine;

[CreateAssetMenu(
    fileName = "WeaponPresentation",
    menuName = "Dist/Combat/Weapon Presentation")]
public sealed class WeaponPresentation : ScriptableObject
{
    [Serializable]
    public sealed class EffectSeed
    {
        public string effectId = BodyPartEffectIds.Bleed;
        public int intensity = 1;
        public float remainingSeconds = 8f;
    }

    [Serializable]
    public sealed class Entry
    {
        public WeaponAction action = WeaponAction.Bashing;
        public EffectSeed[] effectSeeds;
        public WeaponActionVfx vfx = new();
    }

    [SerializeField] Entry[] _entries = Array.Empty<Entry>();
    [SerializeField] WeaponActionMask _supportedActions;
    [Tooltip("공유 CharacterAnimController 슬롯에 덮어쓸 클립 묶음. 비우면 캐릭터 기본 컨트롤러 유지.")]
    [SerializeField] AnimatorOverrideController _animatorOverride;

    public WeaponActionMask SupportedActions => _supportedActions;
    public Entry[] Entries => _entries;
    public AnimatorOverrideController AnimatorOverride => _animatorOverride;

    void OnValidate() => RebuildSupportedActions();

    public void RebuildSupportedActions()
    {
        WeaponActionMask mask = WeaponActionMask.None;
        if (_entries == null)
        {
            _supportedActions = mask;
            return;
        }

        for (int i = 0; i < _entries.Length; i++)
        {
            Entry entry = _entries[i];
            if (entry == null)
                continue;
            mask |= WeaponActionUtil.ToMask(entry.action);
        }

        _supportedActions = mask;
    }

    public bool TryGetEntry(WeaponAction action, out Entry entry)
    {
        entry = null;
        if (_entries == null)
            return false;

        for (int i = 0; i < _entries.Length; i++)
        {
            Entry candidate = _entries[i];
            if (candidate == null || candidate.action != action)
                continue;
            entry = candidate;
            return true;
        }

        return false;
    }
}
