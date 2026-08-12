// ============================================================
// ActionHandlerIds — IActionHandler logicId / 데미지·임팩트 태그 SSOT
// ============================================================

using System;

public static class ActionHandlerIds
{
    public const string MeleeHit = "melee_hit";
    public const string SpawnProjectile = "spawn_projectile";
    public const string RaiseGuard = "raise_guard";

    public static string DefaultFor(WeaponAction action)
    {
        switch (WeaponActionUtil.Normalize(action))
        {
            case WeaponAction.Trigger:
                return SpawnProjectile;
            case WeaponAction.Raise:
                return RaiseGuard;
            default:
                return MeleeHit;
        }
    }
}

public static class AttackDamageTags
{
    public const string Bash = "bash";
    public const string Cut = "cut";
    public const string Bullet = "bullet";

    /// <summary>SO 없을 때 최후 폴백. 액션으로 cut/bash를 고르지 않음.</summary>
    public static string Fallback => Bash;

    public static string DefaultFor(WeaponAction action) =>
        WeaponActionUtil.Normalize(action) == WeaponAction.Trigger ? Bullet : Fallback;
}

public static class AttackImpactTags
{
    public const string Fallback = "fallback";
}

public static class ActionHandlerRegistry
{
    static readonly MeleeHitHandler Melee = new MeleeHitHandler();
    static readonly SpawnProjectileHandler Projectile = new SpawnProjectileHandler();
    static readonly RaiseGuardHandler Raise = new RaiseGuardHandler();

    public static bool TryGet(string logicId, out IActionHandler handler)
    {
        handler = null;
        if (string.Equals(logicId, ActionHandlerIds.MeleeHit, StringComparison.Ordinal))
        {
            handler = Melee;
            return true;
        }

        if (string.Equals(logicId, ActionHandlerIds.SpawnProjectile, StringComparison.Ordinal))
        {
            handler = Projectile;
            return true;
        }

        if (string.Equals(logicId, ActionHandlerIds.RaiseGuard, StringComparison.Ordinal))
        {
            handler = Raise;
            return true;
        }

        return false;
    }

    public static IActionHandler Resolve(WeaponAttack attack, WeaponAction action)
    {
        string logicId = attack != null && !string.IsNullOrEmpty(attack.LogicId)
            ? attack.LogicId
            : ActionHandlerIds.DefaultFor(action);
        TryGet(logicId, out IActionHandler handler);
        return handler;
    }
}
