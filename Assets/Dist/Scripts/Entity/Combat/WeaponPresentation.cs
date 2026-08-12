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
        public WeaponAction action = WeaponAction.Swing;
        [Tooltip("Attack 튜닝 (페이로드·핸들러). 비우면 액션 매핑 폴백.")]
        public WeaponAttack attack;
        public EffectSeed[] effectSeeds;
        public WeaponActionVfx vfx = new();
    }

    [SerializeField] Entry[] _entries = Array.Empty<Entry>();
    [SerializeField] WeaponActionMask _supportedActions;
    [Tooltip("기본 선택 행. 범위 밖이거나 빈 행이면 첫 유효 행.")]
    [SerializeField] int _defaultEntryIndex;
    [Tooltip("공유 CharacterAnimController 슬롯에 덮어쓸 클립 묶음. 비우면 캐릭터 기본 컨트롤러 유지.")]
    [SerializeField] AnimatorOverrideController _animatorOverride;

    public WeaponActionMask SupportedActions => _supportedActions;
    public Entry[] Entries => _entries;
    public AnimatorOverrideController AnimatorOverride => _animatorOverride;

    public int DefaultEntryIndex
    {
        get
        {
            int first = FirstValidEntryIndex();
            if (first < 0)
                return 0;
            if (_defaultEntryIndex < 0 ||
                _entries == null ||
                _defaultEntryIndex >= _entries.Length ||
                _entries[_defaultEntryIndex] == null)
                return first;
            return _defaultEntryIndex;
        }
    }

    void OnValidate()
    {
        RebuildSupportedActions();
        int first = FirstValidEntryIndex();
        if (first < 0)
            _defaultEntryIndex = 0;
        else if (_defaultEntryIndex < 0 ||
                 _defaultEntryIndex >= _entries.Length ||
                 _entries[_defaultEntryIndex] == null)
            _defaultEntryIndex = first;
    }

    int FirstValidEntryIndex()
    {
        if (_entries == null)
            return -1;
        for (int i = 0; i < _entries.Length; i++)
        {
            if (_entries[i] != null)
                return i;
        }

        return -1;
    }

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
            if (candidate == null ||
                WeaponActionUtil.Normalize(candidate.action) != WeaponActionUtil.Normalize(action))
                continue;
            entry = candidate;
            return true;
        }

        return false;
    }
}
