// ============================================================
// CharacterCombatVfx — Action 시전 VFX + Hit(특성) + Reaction(Recoil/Blocked)
// ============================================================

using Lean.Pool;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterAttacker))]
public sealed class CharacterCombatVfx : MonoBehaviour
{
    [SerializeField] TimeScaleChannel _timeChannel = TimeScaleChannel.World;

    CharacterAttacker _attacker;

    void Awake() => _attacker = GetComponent<CharacterAttacker>();

    void OnEnable()
    {
        if (_attacker == null)
            return;
        _attacker.AttackResolved += OnAttackResolved;
        _attacker.AttackJudged += OnAttackJudged;
        _attacker.AttackCueFired += OnAttackCueFired;
    }

    void OnDisable()
    {
        if (_attacker == null)
            return;
        _attacker.AttackResolved -= OnAttackResolved;
        _attacker.AttackJudged -= OnAttackJudged;
        _attacker.AttackCueFired -= OnAttackCueFired;
    }

    void OnAttackResolved(AttackOutcome outcome)
    {
        ArmAnimSlotCatalog pipeline = ResolvePipeline();
        WeaponActionVfx vfx = WeaponActionVfxResolver.Resolve(
            _attacker.Presentation,
            outcome.Action,
            pipeline);
        if (vfx == null)
            return;

        Spawn(vfx.actionVfx, outcome.OriginPoint, outcome.Direction);
    }

    void OnAttackJudged(AttackOutcome outcome)
    {
        if (outcome.Result == AttackPerformResult.Obstructed)
        {
            SpawnImpactKind(ArmImpactKind.Blocked, outcome.OriginPoint, outcome.Direction);
            return;
        }

        WeaponImpactVfxDefaults impactDefaults = _attacker.Catalog != null
            ? _attacker.Catalog.ImpactVfxDefaults
            : null;

        WeaponActionVfx vfx = WeaponActionVfxResolver.ResolveImpact(
            outcome.Attack,
            _attacker.Presentation,
            outcome.Action,
            ResolvePipeline(),
            impactDefaults,
            outcome.HitTag);
        if (vfx == null)
            return;

        if (outcome.ResolveMode == WeaponResolveMode.RangedRay)
            SpawnTracer(vfx.tracerVfx, outcome);

        GameObject impactPrefab = outcome.DidHit ? vfx.hitVfx : vfx.missVfx;
        Spawn(impactPrefab, outcome.ImpactPoint, -outcome.Direction);
    }

    void OnAttackCueFired(WieldHand hand, WeaponAction action)
    {
        SpawnImpactKind(ArmImpactKind.Recoil, _attacker.ResolveOrigin(), transform.forward);
    }

    void SpawnImpactKind(ArmImpactKind kind, Vector3 origin, Vector3 forward)
    {
        WeaponActionVfx vfx = WeaponActionVfxResolver.ResolveImpactKind(ResolvePipeline(), kind);
        if (vfx == null)
            return;
        Spawn(vfx.actionVfx, origin, forward);
    }

    ArmAnimSlotCatalog ResolvePipeline()
    {
        if (_attacker?.Catalog != null && _attacker.Catalog.AnimPipeline != null)
            return _attacker.Catalog.AnimPipeline;
        CharacterLocomotionAnim loc = GetComponent<CharacterLocomotionAnim>();
        if (loc == null)
            loc = GetComponentInParent<CharacterLocomotionAnim>();
        return loc != null ? loc.ArmSlotCatalog : null;
    }

    GameObject Spawn(GameObject prefab, Vector3 position, Vector3 forward)
    {
        if (prefab == null)
            return null;

        Quaternion rotation = forward.sqrMagnitude > 1e-6f
            ? Quaternion.LookRotation(forward, Vector3.up)
            : Quaternion.identity;

        GameObject instance = LeanPool.Spawn(prefab, position, rotation);
        VfxChannelTicker ticker = instance.GetComponent<VfxChannelTicker>();
        if (ticker != null)
            ticker.SetChannel(_timeChannel);
        return instance;
    }

    void SpawnTracer(GameObject prefab, AttackOutcome outcome)
    {
        GameObject instance = Spawn(prefab, outcome.OriginPoint, outcome.Direction);
        if (instance == null)
            return;

        VfxTracerLine tracer = instance.GetComponent<VfxTracerLine>();
        if (tracer != null)
            tracer.SetEndpoints(outcome.OriginPoint, outcome.ImpactPoint);
    }
}
