// ============================================================
// WeaponPresentation — 동작 목록(가용·Attack·연출) + AnimatorOverride
// ============================================================

using System;
using Garunnir.Runtime.Gameplay.Data;
using Sirenix.OdinInspector;
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
        [LabelText("동작")]
        public WeaponAction action = WeaponAction.Swing;

        [InlineEditor(InlineEditorObjectFieldModes.Foldout)]
        [Tooltip(
            "이 동작의 레시피(핸들러·cue·발사체 등). Catalog가 아니라 이 Presentation 줄에 꽂습니다.")]
        [LabelText("Attack")]
        public WeaponAttack attack;

        public EffectSeed[] effectSeeds;
        public WeaponActionVfx vfx = new();
    }

    [InfoBox(
        "이 무기가 할 수 있는 동작 목록입니다. 각 줄의 Attack이 “어떻게 때리는지” 레시피입니다.\n" +
        "Catalog 무기 바인딩에서 이 파일을 여러 id가 공유할 수 있습니다.",
        InfoMessageType.None)]
    [LabelText("동작 줄")]
    [ListDrawerSettings(ShowFoldout = true, ListElementLabelName = "action")]
    [SerializeField] Entry[] _entries = Array.Empty<Entry>();

    [ReadOnly]
    [LabelText("Supported (자동)")]
    [SerializeField] WeaponActionMask _supportedActions;

    [Tooltip("기본 선택 행. 범위 밖이거나 빈 행이면 첫 유효 행.")]
    [LabelText("기본 줄 인덱스")]
    [SerializeField] int _defaultEntryIndex;

    [InlineEditor(InlineEditorObjectFieldModes.Foldout)]
    [Tooltip("공유 CharacterAnimController 슬롯에 덮어쓸 클립 묶음. 비우면 캐릭터 기본 컨트롤러 유지.")]
    [LabelText("Animator Override")]
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
