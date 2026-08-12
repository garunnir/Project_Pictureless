// ============================================================
// WeaponActionUtil — ResolveMode / 마스크 순회 (동사 enum: Dist.Gameplay.Data)
// ============================================================

public enum WeaponResolveMode
{
    MeleeReach = 0,
    RangedRay = 1
}

public enum AttackPerformResult
{
    Performed = 0,
    Miss = 1,
    Unsupported = 2,
    OutOfRange = 3,
    Cooling = 4,
    NoTarget = 5,
    NoAmmo = 6
}

public static class WeaponActionUtil
{
    public const int LegacyCuttingValue = 1;

    public static readonly WeaponAction[] All =
    {
        WeaponAction.Swing,
        WeaponAction.Thrust,
        WeaponAction.Trigger,
        WeaponAction.Raise
    };

    public static WeaponAction Normalize(WeaponAction action)
    {
        if ((int)action == LegacyCuttingValue)
            return WeaponAction.Swing;
        return action;
    }

    public static WeaponActionMask ToMask(WeaponAction action)
    {
        switch (Normalize(action))
        {
            case WeaponAction.Swing: return WeaponActionMask.Swing;
            case WeaponAction.Trigger: return WeaponActionMask.Trigger;
            case WeaponAction.Thrust: return WeaponActionMask.Thrust;
            case WeaponAction.Raise: return WeaponActionMask.Raise;
            default: return WeaponActionMask.None;
        }
    }

    public static WeaponResolveMode ResolveMode(WeaponAction action) =>
        Normalize(action) == WeaponAction.Trigger
            ? WeaponResolveMode.RangedRay
            : WeaponResolveMode.MeleeReach;

    public static bool SuppressesAttackTrigger(WeaponAction action) =>
        Normalize(action) == WeaponAction.Raise;

    public static bool TryNextAvailable(
        WeaponActionMask available,
        WeaponAction current,
        out WeaponAction next)
    {
        next = current;
        if (available == WeaponActionMask.None)
            return false;

        WeaponAction normalized = Normalize(current);
        int count = All.Length;
        int currentIndex = 0;
        for (int i = 0; i < count; i++)
        {
            if (All[i] != normalized)
                continue;
            currentIndex = i;
            break;
        }

        for (int step = 1; step <= count; step++)
        {
            WeaponAction candidate = All[(currentIndex + step) % count];
            if ((available & ToMask(candidate)) == 0)
                continue;
            next = candidate;
            return true;
        }

        return false;
    }

    public static bool TryFirstAvailable(WeaponActionMask available, out WeaponAction action)
    {
        for (int i = 0; i < All.Length; i++)
        {
            WeaponAction candidate = All[i];
            if ((available & ToMask(candidate)) == 0)
                continue;
            action = candidate;
            return true;
        }

        action = WeaponAction.Swing;
        return false;
    }
}
