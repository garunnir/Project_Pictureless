// ============================================================
// WeaponPresentationCatalog — 진입점 바인딩 (item → gun.skill → category → Unarmed)
// ============================================================

using System;
using Garunnir.Runtime.Gameplay.Data;
using Sirenix.OdinInspector;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(
    fileName = "WeaponPresentationCatalog",
    menuName = "Dist/Combat/Weapon Presentation Catalog")]
public sealed class WeaponPresentationCatalog : ScriptableObject
{
    const string FallbacksAssetPath =
        "Assets/Dist/SOData/Combat/Fallbacks/WeaponCombatFallbacks.asset";

    [Serializable]
    public sealed class Binding
    {
        [Tooltip("아이템 id, gun.skill, 또는 weapon_category id.")]
        [LabelText("Id")]
        public string id;

        [InlineEditor(InlineEditorObjectFieldModes.Foldout)]
        [Tooltip("이 id가 쓸 동작 목록. 여러 줄이 같은 Presentation 파일을 가리킬 수 있습니다.")]
        [LabelText("Presentation")]
        public WeaponPresentation presentation;
    }

    [InfoBox(
        "【진입점】 무기가 들릴 때 Presentation(Leaf 목록)을 고릅니다.\n" +
        "순서: 아이템 전용 → gun.skill → weapon_category → 맨손. Leaf·Attack·Override는 Presentation을 펼쳐 편집.\n" +
        "Fallbacks = AnimVerb Pipeline·Hit VFX·발사체 공용(거의 안 건드림). Semi/Burst/Auto는 Presentation Leaf.",
        InfoMessageType.None)]
    [SerializeField, HideInInspector] int _inspectorPad;

    [Title("Unarmed", "아이템·숙련·카테고리 모두 없을 때 (마지막 진입점)", horizontalLine: false)]
    [InlineEditor(InlineEditorObjectFieldModes.Foldout)]
    [Tooltip("아이템·gun.skill·카테고리 연결이 없을 때 쓰는 맨손 동작 목록입니다.")]
    [LabelText("맨손 Presentation")]
    [SerializeField] WeaponPresentation _unarmed;

    [ListDrawerSettings(ShowFoldout = true, ListElementLabelName = "id")]
    [Tooltip("특정 아이템 id에만 쓰는 동작 목록입니다. 찾을 때 가장 먼저 봅니다.")]
    [LabelText("By Item Id")]
    [SerializeField] Binding[] _byItemId = Array.Empty<Binding>();

    [ListDrawerSettings(ShowFoldout = true, ListElementLabelName = "id")]
    [Tooltip("ItemData.gun.skill에 맞춰 쓰는 동작 목록입니다. 아이템 전용이 없을 때 사용합니다.")]
    [LabelText("By Skill Id")]
    [SerializeField] Binding[] _bySkillId = Array.Empty<Binding>();

    [ListDrawerSettings(ShowFoldout = true, ListElementLabelName = "id")]
    [Tooltip("아이템의 weapon_category에 맞춰 쓰는 동작 목록입니다. 아이템·숙련이 없을 때 사용합니다.")]
    [LabelText("By Category Id")]
    [SerializeField] Binding[] _byCategoryId = Array.Empty<Binding>();

    [FoldoutGroup("폴백 (거의 안 건드림)", Expanded = false)]
    [InfoBox(
        "AnimVerb Pipeline·Hit VFX·발사체 공용. Leaf(fire-mode) 행은 여기 없음. 평소 접어두세요.",
        InfoMessageType.None)]
    [InlineEditor(InlineEditorObjectFieldModes.Foldout)]
    [LabelText("Fallbacks")]
    [SerializeField] WeaponCombatFallbacks _fallbacks;

    [FoldoutGroup("폴백 (거의 안 건드림)")]
    [Button("Fallbacks 에셋만 선택", ButtonSizes.Medium)]
    [EnableIf(nameof(_fallbacks))]
    void SelectFallbacksAsset()
    {
#if UNITY_EDITOR
        if (_fallbacks != null)
            Selection.activeObject = _fallbacks;
#endif
    }

    public WeaponPresentation Unarmed => _unarmed;
    public WeaponCombatFallbacks Fallbacks => _fallbacks;
    public Binding[] ByItemId => _byItemId;
    public Binding[] BySkillId => _bySkillId;
    public Binding[] ByCategoryId => _byCategoryId;
    public ArmAnimSlotCatalog AnimPipeline =>
        _fallbacks != null ? _fallbacks.AnimPipeline : null;
    public WeaponImpactVfxDefaults ImpactVfxDefaults =>
        _fallbacks != null ? _fallbacks.ImpactVfxDefaults : null;
    public DistProjectile DefaultProjectile =>
        _fallbacks != null ? _fallbacks.DefaultProjectile : null;

    public void SetFallbacks(WeaponCombatFallbacks fallbacks) => _fallbacks = fallbacks;

    public void SetAnimPipeline(ArmAnimSlotCatalog pipeline)
    {
        if (_fallbacks == null)
        {
#if UNITY_EDITOR
            _fallbacks = AssetDatabase.LoadAssetAtPath<WeaponCombatFallbacks>(FallbacksAssetPath);
#endif
            if (_fallbacks == null)
                return;
        }

        _fallbacks.SetAnimPipeline(pipeline);
    }

    public WeaponPresentation Resolve(string itemId, ItemData item)
    {
        if (!string.IsNullOrEmpty(itemId) &&
            TryFind(_byItemId, itemId, out WeaponPresentation byItem))
            return byItem;

        string skillId = item?.gun != null ? item.gun.skill : null;
        if (!string.IsNullOrEmpty(skillId) &&
            TryFind(_bySkillId, skillId, out WeaponPresentation bySkill))
            return bySkill;

        if (item?.weapon_category != null)
        {
            for (int i = 0; i < item.weapon_category.Count; i++)
            {
                string categoryId = item.weapon_category[i];
                if (string.IsNullOrEmpty(categoryId))
                    continue;
                if (TryFind(_byCategoryId, categoryId, out WeaponPresentation byCategory))
                    return byCategory;
            }
        }

        return _unarmed;
    }

    public bool TryGetByItemId(string itemId, out WeaponPresentation presentation) =>
        TryFind(_byItemId, itemId, out presentation);

    public void EnsureItemBinding(string itemId, WeaponPresentation presentation)
    {
        if (string.IsNullOrEmpty(itemId) || presentation == null)
            return;

        if (_byItemId != null)
        {
            for (int i = 0; i < _byItemId.Length; i++)
            {
                Binding binding = _byItemId[i];
                if (binding == null ||
                    !string.Equals(binding.id, itemId, StringComparison.Ordinal))
                    continue;
                binding.presentation = presentation;
                return;
            }
        }

        int len = _byItemId?.Length ?? 0;
        var next = new Binding[len + 1];
        if (_byItemId != null && len > 0)
            Array.Copy(_byItemId, next, len);
        next[len] = new Binding { id = itemId, presentation = presentation };
        _byItemId = next;
    }

    public void UnlinkItem(string itemId)
    {
        if (string.IsNullOrEmpty(itemId) || _byItemId == null || _byItemId.Length == 0)
            return;

        int keep = 0;
        for (int i = 0; i < _byItemId.Length; i++)
        {
            Binding binding = _byItemId[i];
            if (binding == null ||
                string.Equals(binding.id, itemId, StringComparison.Ordinal))
                continue;
            _byItemId[keep++] = binding;
        }

        if (keep == _byItemId.Length)
            return;

        Array.Resize(ref _byItemId, keep);
    }

    static bool TryFind(Binding[] bindings, string id, out WeaponPresentation presentation)
    {
        presentation = null;
        if (bindings == null || string.IsNullOrEmpty(id))
            return false;

        for (int i = 0; i < bindings.Length; i++)
        {
            Binding binding = bindings[i];
            if (binding == null ||
                binding.presentation == null ||
                !string.Equals(binding.id, id, StringComparison.Ordinal))
                continue;
            presentation = binding.presentation;
            return true;
        }

        return false;
    }
}
