// ============================================================
// WeaponAction — 캐릭터 무기 활용 동사 / 마스크
// ============================================================

using System;

[Flags]
public enum WeaponActionMask
{
    None = 0,
    Swing = 1,
    Stab = 2,
    Trigger = 4
}

public enum WeaponAction
{
    Swing = 0,
    Stab = 1,
    Trigger = 2
}

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
    NoTarget = 5
}

public static class WeaponActionUtil
{
    public static WeaponActionMask ToMask(WeaponAction action)
    {
        switch (action)
        {
            case WeaponAction.Swing: return WeaponActionMask.Swing;
            case WeaponAction.Stab: return WeaponActionMask.Stab;
            case WeaponAction.Trigger: return WeaponActionMask.Trigger;
            default: return WeaponActionMask.None;
        }
    }

    public static bool TryNextAvailable(
        WeaponActionMask available,
        WeaponAction current,
        out WeaponAction next)
    {
        next = current;
        if (available == WeaponActionMask.None)
            return false;

        for (int step = 1; step <= 3; step++)
        {
            int index = ((int)current + step) % 3;
            var candidate = (WeaponAction)index;
            if ((available & ToMask(candidate)) == 0)
                continue;
            next = candidate;
            return true;
        }

        return false;
    }

    public static bool TryFirstAvailable(WeaponActionMask available, out WeaponAction action)
    {
        if ((available & WeaponActionMask.Swing) != 0)
        {
            action = WeaponAction.Swing;
            return true;
        }

        if ((available & WeaponActionMask.Stab) != 0)
        {
            action = WeaponAction.Stab;
            return true;
        }

        if ((available & WeaponActionMask.Trigger) != 0)
        {
            action = WeaponAction.Trigger;
            return true;
        }

        action = WeaponAction.Swing;
        return false;
    }
}
