// ============================================================
// WeaponActionUtil — Leaf / Family / AnimVerb / 마스크 순회
// ============================================================

using System.Collections.Generic;
using Garunnir.Runtime.Gameplay.Data;

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
    NoAmmo = 6,
    /// <summary>장애물에 막힘 (빗나감 Miss와 구분 — Impact Blocked).</summary>
    Obstructed = 7
}

public static class WeaponActionUtil
{
    public const int LegacyCuttingValue = 1;

    /// <summary>UI·Cycle·Entry용 Leaf 전부.</summary>
    public static readonly WeaponAction[] All =
    {
        WeaponAction.Swing,
        WeaponAction.Thrust,
        WeaponAction.Semi,
        WeaponAction.Burst,
        WeaponAction.Auto,
        WeaponAction.Raise
    };

    /// <summary>슬롯 파일 접두용. Catalog는 Leaf마다 행 — AllAnimVerbs만이 아님.</summary>
    public static readonly WeaponAction[] AllAnimVerbs =
    {
        WeaponAction.Swing,
        WeaponAction.Thrust,
        WeaponAction.Trigger,
        WeaponAction.Raise
    };

    /// <summary>구 Cutting만 Swing으로. Trigger 값은 유지(AnimVerb 베이크용).</summary>
    public static WeaponAction FoldLegacyCutting(WeaponAction action)
    {
        if ((int)action == LegacyCuttingValue)
            return WeaponAction.Swing;
        return action;
    }

    /// <summary>Leaf 정규화: Cutting→Swing, 구 Trigger→Semi.</summary>
    public static WeaponAction Normalize(WeaponAction action)
    {
        action = FoldLegacyCutting(action);
        if (action == WeaponAction.Trigger)
            return WeaponAction.Semi;
        return action;
    }

    /// <summary>핸들러 등 레거시 접기. Catalog 폴백 행 조회에는 쓰지 않음 — Leaf마다 행.</summary>
    public static WeaponAction ToAnimVerb(WeaponAction action)
    {
        switch (Normalize(action))
        {
            case WeaponAction.Semi:
            case WeaponAction.Burst:
            case WeaponAction.Auto:
                return WeaponAction.Trigger;
            default:
                return Normalize(action);
        }
    }

    public static bool IsRanged(WeaponAction action) =>
        ToAnimVerb(action) == WeaponAction.Trigger;

    public static bool TryGetFamily(WeaponAction action, out WeaponActionFamily family)
    {
        switch (Normalize(action))
        {
            case WeaponAction.Swing:
            case WeaponAction.Thrust:
                family = WeaponActionFamily.Melee;
                return true;
            case WeaponAction.Semi:
            case WeaponAction.Burst:
            case WeaponAction.Auto:
                family = WeaponActionFamily.Trigger;
                return true;
            default:
                family = default;
                return false;
        }
    }

    /// <summary>Odin/컨텍스트 경로. Family 있으면 "Melee/Swing", 없으면 "Raise".</summary>
    public static string DropdownPath(WeaponAction action)
    {
        WeaponAction leaf = Normalize(action);
        if (TryGetFamily(leaf, out WeaponActionFamily family))
            return FamilyLabel(family) + "/" + LeafLabel(leaf);
        return LeafLabel(leaf);
    }

    public static string FamilyLabel(WeaponActionFamily family)
    {
        switch (family)
        {
            case WeaponActionFamily.Melee: return "Melee";
            case WeaponActionFamily.Trigger: return "Trigger";
            default: return family.ToString();
        }
    }

    public static string LeafLabel(WeaponAction action)
    {
        switch (Normalize(action))
        {
            case WeaponAction.Swing: return "Swing";
            case WeaponAction.Thrust: return "Thrust";
            case WeaponAction.Semi: return "Semi";
            case WeaponAction.Burst: return "Burst";
            case WeaponAction.Auto: return "Auto";
            case WeaponAction.Raise: return "Raise";
            default: return Normalize(action).ToString();
        }
    }

    public static WeaponActionMask ToMask(WeaponAction action)
    {
        switch (Normalize(action))
        {
            case WeaponAction.Swing: return WeaponActionMask.Swing;
            case WeaponAction.Thrust: return WeaponActionMask.Thrust;
            case WeaponAction.Raise: return WeaponActionMask.Raise;
            case WeaponAction.Semi: return WeaponActionMask.Semi;
            case WeaponAction.Burst: return WeaponActionMask.Burst;
            case WeaponAction.Auto: return WeaponActionMask.Auto;
            default: return WeaponActionMask.None;
        }
    }

    public static WeaponResolveMode ResolveMode(WeaponAction action) =>
        IsRanged(action)
            ? WeaponResolveMode.RangedRay
            : WeaponResolveMode.MeleeReach;

    public static bool SuppressesAttackTrigger(WeaponAction action) =>
        Normalize(action) == WeaponAction.Raise;

    /// <summary>클릭 1회당 발사 수. Burst=gun.burst(없으면 DefaultBurstShots), Auto=클립 상한.</summary>
    public const int DefaultBurstShots = 3;
    public const int AutoClickVolleyMax = 10;

    public static int ShotsPerPerform(WeaponAction action, ItemData item)
    {
        switch (Normalize(action))
        {
            case WeaponAction.Burst:
            {
                int burst = item?.gun != null ? item.gun.burst : 0;
                return burst > 0 ? burst : DefaultBurstShots;
            }
            case WeaponAction.Auto:
            {
                int clip = item?.gun != null ? item.gun.clip_size : 0;
                if (clip > 0)
                    return clip < AutoClickVolleyMax ? clip : AutoClickVolleyMax;
                return AutoClickVolleyMax;
            }
            default:
                return 1;
        }
    }

    /// <summary>볼리 탄 사이 간격 = 기본 공속 × 이 비율 (첫 탄 제외).</summary>
    public const float BurstShotIntervalFactor = 0.2f;
    public const float AutoShotIntervalFactor = 0.12f;

    public static float VolleyShotIntervalFactor(WeaponAction action)
    {
        switch (Normalize(action))
        {
            case WeaponAction.Burst: return BurstShotIntervalFactor;
            case WeaponAction.Auto: return AutoShotIntervalFactor;
            default: return 1f;
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

    /// <summary>가용 Leaf를 Family 순(Melee→Trigger→평면)으로 나열.</summary>
    public static void CollectAvailableLeaves(
        WeaponActionMask available,
        List<WeaponAction> into)
    {
        if (into == null)
            return;
        into.Clear();
        for (int i = 0; i < All.Length; i++)
        {
            WeaponAction leaf = All[i];
            if ((available & ToMask(leaf)) == 0)
                continue;
            into.Add(leaf);
        }
    }
}
