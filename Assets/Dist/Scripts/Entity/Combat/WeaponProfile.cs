// ============================================================
// WeaponProfile — 무기별 지원 액션·수치 SO
// ============================================================

using System;
using Garunnir.Runtime.Gameplay.Data;
using UnityEngine;

[CreateAssetMenu(
    fileName = "WeaponProfile",
    menuName = "Dist/Combat/Weapon Profile")]
public sealed class WeaponProfile : ScriptableObject
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
        public WeaponResolveMode resolveMode = WeaponResolveMode.MeleeReach;
        [Min(0f)] public float range = 1.2f;
        [Min(0f)] public float cooldownSeconds = 0.8f;
        [Min(0)] public int damage = 8;
        [Range(0f, 1f)] public float accuracy = 0.7f;
        public string skillId = CombatSkillIds.Swing;
        [Min(0)] public int minimumSkillLevel;
        public float accuracyPerSkillLevel = 0.01f;
        [Min(0)] public int practiceXp = 4;
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
