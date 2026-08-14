// ============================================================
// WeaponPresentation — 동작 목록(가용·Attack·연출) + AnimatorOverride
// ============================================================

using System;
using System.Collections.Generic;
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
        [LabelText("동작 (Leaf)")]
        [ValueDropdown(nameof(LeafDropdown))]
        [Tooltip("Family 있으면 Melee/…·Trigger/… 로 묶임. Raise는 평면.")]
        public WeaponAction action = WeaponAction.Swing;

        [InlineEditor(InlineEditorObjectFieldModes.Foldout)]
        [Tooltip(
            "이 동작의 레시피(핸들러·cue·발사체 등). Catalog가 아니라 이 Presentation 줄에 꽂습니다.")]
        [LabelText("Attack")]
        public WeaponAttack attack;

        [LabelText("Hold(아이들)")]
        [Tooltip(
            "끄면 비조준·비Attack일 때 해당 손 팔 overlay weight 0 (몸 Locomotion Idle만). " +
            "Aim/Attack 재생 중에는 overlay를 켠다.")]
        public bool useHold = true;

        public EffectSeed[] effectSeeds;
        public WeaponActionVfx vfx = new();

        static IEnumerable<ValueDropdownItem<WeaponAction>> LeafDropdown()
        {
            WeaponAction[] all = WeaponActionUtil.All;
            for (int i = 0; i < all.Length; i++)
            {
                WeaponAction leaf = all[i];
                yield return new ValueDropdownItem<WeaponAction>(
                    WeaponActionUtil.DropdownPath(leaf),
                    leaf);
            }
        }
    }

    [InfoBox(
        "Leaf = 선택·시전 단위(실체). Family(Melee/Trigger)는 에디터·UI 묶음만.\n" +
        "기본 동사 폴백은 ArmAnimSlotCatalog에 Leaf마다 행. Entry 비면 그 행 사용.\n" +
        "Hold(아이들)=Entry.useHold. Override=thin 클립 덮어쓰기(분류 아님).\n" +
        "클립 배속은 Override Inspector에서 할당한 클립 옆 Speed (슬롯 속도 아님).",
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
    [Tooltip(
        "thin Hold/Aim/Attack 클립 덮어쓰기(분류 아님). 컨트롤러는 동작 모름. 비우면 Pipeline→thin.")]
    [LabelText("Animator Override")]
    [SerializeField] AnimatorOverrideController _animatorOverride;

    [HideInInspector]
    [SerializeField] WeaponAnimClipSpeeds _animClipSpeeds;

    public WeaponActionMask SupportedActions => _supportedActions;
    public Entry[] Entries => _entries;
    public AnimatorOverrideController AnimatorOverride => _animatorOverride;
    public WeaponAnimClipSpeeds AnimClipSpeeds => _animClipSpeeds;

    public void SetAnimClipSpeeds(WeaponAnimClipSpeeds speeds) => _animClipSpeeds = speeds;

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
        MigrateLegacyTriggerLeaves();
        RebuildSupportedActions();
        int first = FirstValidEntryIndex();
        if (first < 0)
            _defaultEntryIndex = 0;
        else if (_defaultEntryIndex < 0 ||
                 _defaultEntryIndex >= _entries.Length ||
                 _entries[_defaultEntryIndex] == null)
            _defaultEntryIndex = first;
#if UNITY_EDITOR
        TryWireClipSpeedsFromOverride();
#endif
    }

#if UNITY_EDITOR
    void TryWireClipSpeedsFromOverride()
    {
        if (_animClipSpeeds != null || _animatorOverride == null)
            return;
        string path = UnityEditor.AssetDatabase.GetAssetPath(_animatorOverride);
        if (string.IsNullOrEmpty(path))
            return;
        UnityEngine.Object[] assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path);
        for (int i = 0; i < assets.Length; i++)
        {
            if (assets[i] is WeaponAnimClipSpeeds speeds)
            {
                _animClipSpeeds = speeds;
                return;
            }
        }
    }
#endif

    void MigrateLegacyTriggerLeaves()
    {
        if (_entries == null)
            return;
        for (int i = 0; i < _entries.Length; i++)
        {
            Entry entry = _entries[i];
            if (entry == null)
                continue;
            if (entry.action == WeaponAction.Trigger)
                entry.action = WeaponAction.Semi;
        }
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

    public void SetEntries(Entry[] entries)
    {
        _entries = entries ?? System.Array.Empty<Entry>();
        RebuildSupportedActions();
    }

    public bool TryGetEntry(WeaponAction action, out Entry entry)
    {
        entry = null;
        if (_entries == null)
            return false;

        WeaponAction want = WeaponActionUtil.Normalize(action);
        for (int i = 0; i < _entries.Length; i++)
        {
            Entry candidate = _entries[i];
            if (candidate == null ||
                WeaponActionUtil.Normalize(candidate.action) != want)
                continue;
            entry = candidate;
            return true;
        }

        return false;
    }

    /// <summary>Entry 없거나 useHold면 true. 비조준·비Attack 팔 overlay 게이트.</summary>
    public bool UsesHold(WeaponAction action)
    {
        if (!TryGetEntry(action, out Entry entry) || entry == null)
            return true;
        return entry.useHold;
    }
}
