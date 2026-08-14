// ============================================================
// SpawnProjectileHandler — spawn_projectile: cue 히트스캔 명중
// ============================================================

using Garunnir.Runtime.Gameplay.Data;
using UnityEngine;

public sealed class SpawnProjectileHandler : IActionHandler
{
    readonly RaycastHit[] _hits = new RaycastHit[CombatHitscan.BufferSize];
    readonly Collider[] _overlaps = new Collider[CombatHitscan.BufferSize];
    readonly CharacterBodyHost[] _hosts = new CharacterBodyHost[CombatHitscan.BufferSize];
    readonly Vector3[] _impacts = new Vector3[CombatHitscan.BufferSize];

    public string LogicId => ActionHandlerIds.SpawnProjectile;

    public void Execute(CharacterAttacker attacker, in ActionHandlerContext context)
    {
        if (attacker == null)
            return;

        ItemData item = attacker.ItemFor(context.ItemId);
        WeaponResolveMode mode = WeaponResolveMode.RangedRay;
        Vector3 origin = attacker.ResolveOrigin();

        if (attacker.GetCooldown(context.Hand) > 0f)
        {
            attacker.EmitJudgedGate(context, mode, AttackPerformResult.Cooling, item, origin);
            return;
        }

        CharacterBodyHost targetHost = context.Target;
        if (targetHost == null || targetHost.Body == null)
        {
            attacker.EmitJudgedGate(context, mode, AttackPerformResult.NoTarget, item, origin);
            return;
        }

        Vector3 toTarget = targetHost.transform.position - attacker.transform.position;
        toTarget.y = 0f;
        float distance = toTarget.magnitude;
        ItemData ammoProbe = WeaponChamber.ResolveAmmo(context.Stack, context.Instance);
        float range = CombatHitscan.EffectiveRange(item, context.Action, ammoProbe, origin);
        if (distance > range)
        {
            attacker.EmitJudgedGate(context, mode, AttackPerformResult.OutOfRange, item, origin);
            return;
        }

        int shots = WeaponActionUtil.ShotsPerPerform(context.Action, item);
        if (shots < 1)
            shots = 1;

        int fired = 0;
        for (int s = 0; s < shots; s++)
        {
            if (!WeaponChamber.EnsureChamberForFire(
                    context.Instance, context.Stack, item, context.Attack))
            {
                if (fired == 0)
                    attacker.EmitJudgedGate(context, mode, AttackPerformResult.NoAmmo, item, origin);
                break;
            }

            ItemData ammo = WeaponChamber.ResolveAmmo(context.Stack, context.Instance);
            if (!FireOne(attacker, context, item, ammo, origin, range, targetHost, mode))
                break;
            fired++;
        }

        if (fired > 0)
        {
            attacker.CommitAttempt(
                context,
                item,
                consumeAmmo: false,
                WeaponChamber.ResolveAmmo(context.Stack, context.Instance),
                applyCooldown: true,
                practice: false);
        }
    }

    bool FireOne(
        CharacterAttacker attacker,
        in ActionHandlerContext context,
        ItemData item,
        ItemData ammo,
        Vector3 origin,
        float range,
        CharacterBodyHost targetHost,
        WeaponResolveMode mode)
    {
        Collider targetCollider = targetHost.GetComponentInChildren<Collider>();
        Vector3 targetCenter = CharacterAttacker.ResolveBodyCenter(targetHost.transform, targetCollider);
        Vector3 dir = targetCenter - origin;
        float aimDist = dir.magnitude;
        if (aimDist <= CharacterAttacker.MinRayDistance)
        {
            attacker.CommitAttempt(
                context, item, consumeAmmo: true, ammo, applyCooldown: false, practice: true);
            attacker.ResolveCommittedHit(
                context,
                mode,
                item,
                origin,
                consumeAmmo: false,
                ammo,
                applyCooldown: false,
                practice: false,
                impactOverride: targetCenter);
            return true;
        }

        dir /= aimDist;
        attacker.CommitAttempt(
            context, item, consumeAmmo: true, ammo, applyCooldown: false, practice: true);

        int pierce = WeaponChamber.ResolvePierce(context.Stack, context.Instance);
        CombatHitscan.Trace(
            attacker,
            origin,
            dir,
            range,
            attacker.RangedObstructionMask,
            pierce,
            _hits,
            _overlaps,
            _hosts,
            _impacts,
            out int bodyHitCount,
            out bool obstructed,
            out Vector3 obstructImpact,
            out bool missAtRangeEnd,
            out Vector3 missImpact);

        for (int i = 0; i < bodyHitCount; i++)
        {
            CharacterBodyHost host = _hosts[i];
            var hitContext = new ActionHandlerContext(
                context.Action,
                context.Hand,
                context.Attack,
                host,
                context.OffenseFactor,
                context.ItemId,
                context.Instance,
                context.Stack);
            attacker.ResolveCommittedHit(
                hitContext,
                mode,
                item,
                origin,
                consumeAmmo: false,
                ammo,
                applyCooldown: false,
                practice: false,
                impactOverride: _impacts[i]);
        }

        if (obstructed)
        {
            attacker.EmitJudged(
                context,
                mode,
                AttackPerformResult.Obstructed,
                targetHost,
                string.Empty,
                0,
                origin,
                obstructImpact,
                item,
                ammo);
            return true;
        }

        if (missAtRangeEnd)
        {
            attacker.EmitJudged(
                context,
                mode,
                AttackPerformResult.Miss,
                targetHost,
                string.Empty,
                0,
                origin,
                missImpact,
                item,
                ammo);
        }

        return true;
    }
}
