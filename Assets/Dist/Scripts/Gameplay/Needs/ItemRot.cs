// ============================================================
// ItemRot — 부패 식품 CreatedWorldMinute·신선/썩음 판정 SSOT
// ============================================================

using Garunnir.Runtime.Gameplay.Data;

public static class ItemRot
{
    public const int UnsetCreatedWorldMinute = ItemInstance.UnsetCreatedWorldMinute;

    public static bool IsSpoiling(ItemData item)
    {
        return item?.comestible != null && item.comestible.spoils_in_minutes > 0f;
    }

    public static int CurrentWorldMinute()
    {
        WorldClock clock = WorldClock.Instance;
        if (clock == null)
            return 0;

        int minutesPerDay = WorldClockSettings.DefaultMinutesPerDay;
        if (clock.Settings != null)
            minutesPerDay = clock.Settings.MinutesPerDay;

        return clock.DayIndex * minutesPerDay + clock.MinuteOfDay;
    }

    public static bool IsRotten(ItemInstance instance, int worldMinute)
    {
        if (instance == null || !IsSpoiling(instance.Item))
            return false;

        int created = instance.CreatedWorldMinute;
        if (created == UnsetCreatedWorldMinute)
            return false;

        return worldMinute - created >= instance.Item.comestible.spoils_in_minutes;
    }

    public static bool IsRottenNow(ItemInstance instance)
    {
        return IsRotten(instance, CurrentWorldMinute());
    }

    public static bool TryStampCreated(ItemInstance instance, int worldMinute)
    {
        if (instance == null || !IsSpoiling(instance.Item))
            return false;
        if (instance.CreatedWorldMinute != UnsetCreatedWorldMinute)
            return false;

        instance.SetCreatedWorldMinute(worldMinute);
        return true;
    }
}
