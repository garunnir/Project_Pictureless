// ============================================================
// WeaponPresentationCatalog — 비주얼 허브 + itemId → category → Unarmed resolve
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
    const string TabAttack = "공격 전·중";
    const string TabHit = "맞힌 결과";
    const string TabBind = "무기 바인딩";
    const string TabMisc = "기타";

    [Serializable]
    public sealed class Binding
    {
        [HorizontalGroup("Row", Width = 0.4f)]
        [HideLabel]
        public string id;

        [HorizontalGroup("Row")]
        [HideLabel]
        public WeaponPresentation presentation;
    }

    [InfoBox(
        "비주얼 보조 허브. 탭에서 잎 SO를 인라인으로 편집한다.\n" +
        "· 공격 전·중 = Pipeline (동사·Recoil/Blocked)\n" +
        "· 맞힌 결과 = 태그 hit/miss/tracer\n" +
        "· 무기 바인딩 = 아이템/카테고리 Presentation",
        InfoMessageType.None)]
    [SerializeField, HideInInspector] int _inspectorPad;

    [TabGroup(TabAttack)]
    [Title("Arm Anim Pipeline", "동사 클립+VFX · Impact 반응", horizontalLine: false)]
    [InlineEditor(InlineEditorObjectFieldModes.Foldout)]
    [LabelText("Pipeline")]
    [SerializeField] ArmAnimSlotCatalog _animPipeline;

    [TabGroup(TabAttack)]
    [Button("Pipeline 에셋만 선택", ButtonSizes.Medium)]
    [EnableIf(nameof(_animPipeline))]
    void SelectPipelineAsset()
    {
#if UNITY_EDITOR
        if (_animPipeline != null)
            Selection.activeObject = _animPipeline;
#endif
    }

    [TabGroup(TabHit)]
    [Title("Impact Tag VFX", "bash / cut / bullet → hit·miss·tracer", horizontalLine: false)]
    [InlineEditor(InlineEditorObjectFieldModes.Foldout)]
    [LabelText("Tag Defaults")]
    [SerializeField] WeaponImpactVfxDefaults _impactVfxDefaults;

    [TabGroup(TabHit)]
    [Button("Tag VFX 에셋만 선택", ButtonSizes.Medium)]
    [EnableIf(nameof(_impactVfxDefaults))]
    void SelectImpactVfxAsset()
    {
#if UNITY_EDITOR
        if (_impactVfxDefaults != null)
            Selection.activeObject = _impactVfxDefaults;
#endif
    }

    [TabGroup(TabBind)]
    [LabelText("Unarmed")]
    [SerializeField] WeaponPresentation _unarmed;

    [TabGroup(TabBind)]
    [ListDrawerSettings(ShowFoldout = true, ListElementLabelName = "id")]
    [LabelText("By Item Id")]
    [SerializeField] Binding[] _byItemId = Array.Empty<Binding>();

    [TabGroup(TabBind)]
    [ListDrawerSettings(ShowFoldout = true, ListElementLabelName = "id")]
    [LabelText("By Category Id")]
    [SerializeField] Binding[] _byCategoryId = Array.Empty<Binding>();

    [TabGroup(TabMisc)]
    [Tooltip("Attack에 프리팹이 없을 때 spawn_projectile 기본 발사체.")]
    [LabelText("Default Projectile")]
    [SerializeField] DistProjectile _defaultProjectile;

    public WeaponPresentation Unarmed => _unarmed;
    public ArmAnimSlotCatalog AnimPipeline => _animPipeline;
    public WeaponImpactVfxDefaults ImpactVfxDefaults => _impactVfxDefaults;
    public DistProjectile DefaultProjectile => _defaultProjectile;

    public void SetAnimPipeline(ArmAnimSlotCatalog pipeline) => _animPipeline = pipeline;

    public WeaponPresentation Resolve(string itemId, ItemData item)
    {
        if (!string.IsNullOrEmpty(itemId) &&
            TryFind(_byItemId, itemId, out WeaponPresentation byItem))
            return byItem;

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
