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

        if (!WeaponChamber.EnsureChamberForFire(
                context.Instance, context.Stack, item, context.Attack))
        {
            attacker.EmitJudgedGate(context, mode, AttackPerformResult.NoAmmo, item, origin);
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
        float range = CombatMath.RangeMeters(item, context.Action);
        if (distance > range)
        {
            attacker.EmitJudgedGate(context, mode, AttackPerformResult.OutOfRange, item, origin);
            return;
        }

        Collider targetCollider = targetHost.GetComponentInChildren<Collider>();
        Vector3 targetCenter = CharacterAttacker.ResolveBodyCenter(targetHost.transform, targetCollider);
        Vector3 dir = targetCenter - origin;
        float rayDist = dir.magnitude;
        if (rayDist <= CharacterAttacker.MinRayDistance)
        {
            attacker.ResolveCommittedHit(context, mode, item, origin, consumeAmmo: true);
            return;
        }

        dir /= rayDist;
        if (TrySpawnProjectile(attacker, context, item, origin, dir, range))
            return;

        ExecuteRayStub(attacker, context, item, origin, dir, rayDist, targetHost, mode);
    }

    static bool TrySpawnProjectile(
        CharacterAttacker attacker,
        in ActionHandlerContext context,
        ItemData item,
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

        attacker.CommitAttempt(context, item, consumeAmmo: true);
        projectile.Launch(
            attacker,
            context,
            item,
            origin,
            direction,
            range,
            WeaponChamber.ResolvePierce(context.Stack),
            attacker.RangedObstructionMask);
        return true;
    }

    static DistProjectile ResolvePrefab(CharacterAttacker attacker, in ActionHandlerContext context)
    {
        if (context.Attack != null && context.Attack.ProjectilePrefab != null)
            return context.Attack.ProjectilePrefab;
        return attacker.Catalog != null ? attacker.Catalog.DefaultProjectile : null;
    }

    static void ExecuteRayStub(
        CharacterAttacker attacker,
        in ActionHandlerContext context,
        ItemData item,
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
            attacker.CommitAttempt(context, item, consumeAmmo: true);
            attacker.EmitJudged(
                context,
                mode,
                AttackPerformResult.Miss,
                targetHost,
                string.Empty,
                0,
                origin,
                blocker.point);
            return;
        }

        attacker.ResolveCommittedHit(context, mode, item, origin, consumeAmmo: true);
    }
}
