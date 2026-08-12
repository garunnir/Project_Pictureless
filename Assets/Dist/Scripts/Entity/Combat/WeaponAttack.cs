// ============================================================
// WeaponAttack — Attack 튜닝 데이터 (페이로드·핸들러 id·큐 시각). Perform 없음
// ============================================================

using System;
using Garunnir.Runtime.Gameplay.Data;
using UnityEngine;

[CreateAssetMenu(
    fileName = "WeaponAttack",
    menuName = "Dist/Combat/Weapon Attack")]
public sealed class WeaponAttack : ScriptableObject
{
    public const float DefaultCueNormalizedTime = 0.35f;

    [Serializable]
    public sealed class EffectSeed
    {
        public string effectId = BodyPartEffectIds.Bleed;
        public int intensity = 1;
        public float remainingSeconds = 8f;
    }

    [SerializeField] string _logicId = ActionHandlerIds.MeleeHit;
    [SerializeField] string _damageTag = AttackDamageTags.Bash;
    [SerializeField] string _impactTag;
    [SerializeField] string _fallbackImpactTag = AttackImpactTags.Fallback;
    [SerializeField] EffectSeed[] _effectSeeds = Array.Empty<EffectSeed>();
    [SerializeField] WeaponActionVfx _attackVfx = new WeaponActionVfx();
    [SerializeField, Range(0f, 1f)] float _cueNormalizedTime = DefaultCueNormalizedTime;
    [Tooltip("켜면 발사 큐에서 약실이 비었을 때 메거진 1발을 올린 뒤 소모. 끄면 펌프/수동(빈 약실=NoAmmo).")]
    [SerializeField] bool _feedsChamberOnFire = true;
    [Tooltip("spawn_projectile이 생성할 Dist 발사체. 비우면 Catalog 기본 → 레이 스텁.")]
    [SerializeField] DistProjectile _projectilePrefab;

    public string LogicId =>
        string.IsNullOrEmpty(_logicId) ? ActionHandlerIds.MeleeHit : _logicId;

    public string DamageTag =>
        string.IsNullOrEmpty(_damageTag) ? AttackDamageTags.Bash : _damageTag;

    public string ImpactTag =>
        string.IsNullOrEmpty(_impactTag) ? DamageTag : _impactTag;

    public string FallbackImpactTag =>
        string.IsNullOrEmpty(_fallbackImpactTag)
            ? AttackImpactTags.Fallback
            : _fallbackImpactTag;

    public EffectSeed[] EffectSeeds => _effectSeeds;

    public WeaponActionVfx AttackVfx => _attackVfx;

    public float CueNormalizedTime => Mathf.Clamp01(_cueNormalizedTime);

    /// <summary>null Attack은 spawn_projectile 기본(자동 보급)으로 본다.</summary>
    public bool FeedsChamberOnFire => _feedsChamberOnFire;

    public DistProjectile ProjectilePrefab => _projectilePrefab;
}
