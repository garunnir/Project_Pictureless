// ============================================================
// CharacterCombatVfx — 공격 판정 결과를 받아 무기 액션 연출을 스폰
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
        if (_attacker != null)
            _attacker.AttackResolved += OnAttackResolved;
    }

    void OnDisable()
    {
        if (_attacker != null)
            _attacker.AttackResolved -= OnAttackResolved;
    }

    void OnAttackResolved(AttackOutcome outcome)
    {
        WeaponProfile weapon = _attacker.Weapon;
        if (weapon == null ||
            !weapon.TryGetEntry(outcome.Action, out WeaponProfile.Entry entry) ||
            entry.vfx == null)
        {
            return;
        }

        WeaponActionVfx vfx = entry.vfx;
        Spawn(vfx.actionVfx, outcome.OriginPoint, outcome.Direction);

        if (outcome.ResolveMode == WeaponResolveMode.RangedRay)
            SpawnTracer(vfx.tracerVfx, outcome);

        GameObject impactPrefab = outcome.DidHit ? vfx.hitVfx : vfx.missVfx;
        Spawn(impactPrefab, outcome.ImpactPoint, -outcome.Direction);
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
