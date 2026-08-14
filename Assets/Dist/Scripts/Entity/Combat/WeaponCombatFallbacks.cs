// ============================================================
// WeaponCombatFallbacks — 공용 폴백 (팔 애니·타격 VFX·발사체). 진입점 아님
// ============================================================

using Sirenix.OdinInspector;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(
    fileName = "WeaponCombatFallbacks",
    menuName = "Dist/Combat/Weapon Combat Fallbacks")]
public sealed class WeaponCombatFallbacks : ScriptableObject
{
    const string TabAttack = "팔 애니";
    const string TabHit = "타격 VFX";
    const string TabProjectile = "발사체";
    const string ProjectilePrefabPath =
        "Assets/Dist/Visual/Prefabs/Combat/DistProjectile.prefab";
    const string ExampleProjectileAttackPath =
        "Assets/Dist/SOData/Combat/Attacks/Attack_Projectile_Bullet.asset";

    [InfoBox(
        "【폴백 · 거의 안 건드림】 Presentation/Attack이 비운 칸을 채우는 공용 기본값입니다.\n" +
        "Pipeline = Leaf마다 행(Swing/Thrust/Semi/Burst/Auto/Raise). 표시는 Melee/Trigger 묶음.\n" +
        "무기별 Leaf 목록은 WeaponPresentationCatalog 바인딩에서 편집하세요.",
        InfoMessageType.None)]
    [SerializeField, HideInInspector] int _inspectorPad;

    [TabGroup(TabAttack)]
    [InfoBox(
        "개별 Presentation이 비운 팔 애니·동작 VFX를 **같은 Leaf** Catalog 행으로 채웁니다.\n" +
        "Recoil·Blocked는 Impact 반응.",
        InfoMessageType.None)]
    [Title("Arm Anim Pipeline", "Leaf 폴백 — Presentation이 비울 때", horizontalLine: false)]
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
    [InfoBox(
        "맞은 자리 이펙트 기본표. Presentation/Attack VFX가 비었을 때 bash·cut·bullet(및 fallback).\n" +
        "팔 Recoil/Blocked와는 별개.",
        InfoMessageType.None)]
    [Title("Impact Tag VFX", "공용 기본 — Entry/Attack VFX가 비울 때", horizontalLine: false)]
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

    [TabGroup(TabProjectile)]
    [InfoBox(
        "미사일/탄도 Pending. spawn_projectile은 히트스캔만 쓰므로 런타임 무시.\n" +
        "탄도 핸들러가 생길 때까지 Inspector 비노출(직렬화 유지).",
        InfoMessageType.None)]
    [Title("Default Projectile", "미사일 Pending — 히트스캔은 무시", horizontalLine: false)]
    [HideInInspector]
    [SerializeField] DistProjectile _defaultProjectile;

    [TabGroup(TabProjectile)]
    [Button("총알 Prefab 선택 (생성·외형)", ButtonSizes.Medium)]
    void SelectProjectilePrefabAsset()
    {
#if UNITY_EDITOR
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectilePrefabPath);
#endif
    }

    [TabGroup(TabProjectile)]
    [Button("예: Attack_Projectile_Bullet (개별 할당)", ButtonSizes.Medium)]
    void SelectExampleProjectileAttack()
    {
#if UNITY_EDITOR
        Selection.activeObject =
            AssetDatabase.LoadAssetAtPath<WeaponAttack>(ExampleProjectileAttackPath);
#endif
    }

    [TabGroup(TabProjectile)]
    [Button("이 Default 에셋만 선택", ButtonSizes.Medium)]
    [EnableIf(nameof(_defaultProjectile))]
    void SelectDefaultProjectileAsset()
    {
#if UNITY_EDITOR
        if (_defaultProjectile != null)
            Selection.activeObject = _defaultProjectile.gameObject;
#endif
    }

    public ArmAnimSlotCatalog AnimPipeline => _animPipeline;
    public WeaponImpactVfxDefaults ImpactVfxDefaults => _impactVfxDefaults;
    public DistProjectile DefaultProjectile => _defaultProjectile;

    public void SetAnimPipeline(ArmAnimSlotCatalog pipeline) => _animPipeline = pipeline;

    public void SetImpactVfxDefaults(WeaponImpactVfxDefaults defaults) =>
        _impactVfxDefaults = defaults;

    public void SetDefaultProjectile(DistProjectile projectile) =>
        _defaultProjectile = projectile;
}
