// ============================================================
// SpawnProjectileHandler — spawn_projectile: Dist 발사체, 실패 시 레이 스텁
// ============================================================

using Garunnir.Runtime.Gameplay.Data;
using UnityEngine;

public sealed class SpawnProjectileHandler : IActionHandler
{
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
        float range = CombatMath.RangeMeters(item, context.Action, ammoProbe);
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

    static bool FireOne(
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
        float rayDist = dir.magnitude;
        if (rayDist <= CharacterAttacker.MinRayDistance)
        {
            attacker.ResolveCommittedHit(
                context, mode, item, origin, consumeAmmo: true, ammo, applyCooldown: false);
            return true;
        }

        dir /= rayDist;
        if (TrySpawnProjectile(attacker, context, item, ammo, origin, dir, range))
            return true;

        return ExecuteRayStub(
            attacker, context, item, ammo, origin, dir, rayDist, targetHost, mode);
    }

    static bool TrySpawnProjectile(
        CharacterAttacker attacker,
        in ActionHandlerContext context,
        ItemData item,
        ItemData ammo,
        Vector3 origin,
        Vector3 direction,
        float range)
    {
        DistProjectile prefab = ResolvePrefab(attacker, context);
        if (prefab == null)
            return false;

        DistProjectile projectile = Object.Instantiate(prefab, origin, Quaternion.LookRotation(direction));
        if (projectile == null)
            return false;

        attacker.CommitAttempt(
            context, item, consumeAmmo: true, ammo, applyCooldown: false, practice: true);
        int pierce = ammo?.ammo != null ? Mathf.Max(0, ammo.ammo.pierce) : 0;
        projectile.Launch(
            attacker,
            context,
            item,
            ammo,
            origin,
            direction,
            range,
            pierce,
            attacker.RangedObstructionMask);
        return true;
    }

    static DistProjectile ResolvePrefab(CharacterAttacker attacker, in ActionHandlerContext context)
    {
        if (context.Attack != null && context.Attack.ProjectilePrefab != null)
            return context.Attack.ProjectilePrefab;
        return attacker.Catalog != null ? attacker.Catalog.DefaultProjectile : null;
    }

    static bool ExecuteRayStub(
        CharacterAttacker attacker,
        in ActionHandlerContext context,
        ItemData item,
        ItemData ammo,
        Vector3 origin,
        Vector3 dir,
        float rayDist,
        CharacterBodyHost targetHost,
        WeaponResolveMode mode)
    {
        if (Physics.Raycast(
                origin,
                dir,
                out RaycastHit blocker,
                rayDist,
                attacker.RangedObstructionMask,
                QueryTriggerInteraction.Ignore) &&
            blocker.collider != null &&
            blocker.collider.transform != targetHost.transform &&
            !blocker.collider.transform.IsChildOf(targetHost.transform))
        {
            attacker.CommitAttempt(
                context, item, consumeAmmo: true, ammo, applyCooldown: false, practice: true);
            attacker.EmitJudged(
                context,
                mode,
                AttackPerformResult.Obstructed,
                targetHost,
                string.Empty,
                0,
                origin,
                blocker.point,
                item,
                ammo);
            return true;
        }

        attacker.ResolveCommittedHit(
            context, mode, item, origin, consumeAmmo: true, ammo, applyCooldown: false);
        return true;
    }
}
