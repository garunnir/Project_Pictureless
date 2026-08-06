// ============================================================
// WeaponAction — 캐릭터 무기 활용 동사 / 마스크 (Bashing/Cutting/Gun)
// ============================================================

using System;

[Flags]
public enum WeaponActionMask
{
    None = 0,
    Bashing = 1,
    Cutting = 2,
    Gun = 4
}

public enum WeaponAction
{
    Bashing = 0,
    Cutting = 1,
    Gun = 2
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
    NoTarget = 5,
    NoAmmo = 6
}

public static class WeaponActionUtil
{
    public static WeaponActionMask ToMask(WeaponAction action)
    {
        switch (action)
        {
            case WeaponAction.Bashing: return WeaponActionMask.Bashing;
            case WeaponAction.Cutting: return WeaponActionMask.Cutting;
            case WeaponAction.Gun: return WeaponActionMask.Gun;
            default: return WeaponActionMask.None;
        }
    }

    public static WeaponResolveMode ResolveMode(WeaponAction action) =>
        action == WeaponAction.Gun
            ? WeaponResolveMode.RangedRay
            : WeaponResolveMode.MeleeReach;

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
        if ((available & WeaponActionMask.Bashing) != 0)
        {
            action = WeaponAction.Bashing;
            return true;
        }

        if ((available & WeaponActionMask.Cutting) != 0)
        {
            action = WeaponAction.Cutting;
            return true;
        }

        if ((available & WeaponActionMask.Gun) != 0)
        {
            action = WeaponAction.Gun;
            return true;
        }

        action = WeaponAction.Bashing;
        return false;
    }
}
