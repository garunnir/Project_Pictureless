// ============================================================
// WorldCalendar — DayIndex에서 day-of-year·Season 파생 (시계 SSOT는 WorldClock)
// ============================================================

public enum WorldSeason
{
    Spring = 0,
    Summer = 1,
    Autumn = 2,
    Winter = 3
}

public static class WorldCalendar
{
    public const int SeasonCount = 4;

    public static int DayOfYear(int dayIndex, int daysPerYear)
    {
        int year = daysPerYear < 1 ? WorldClockSettings.DefaultDaysPerYear : daysPerYear;
        int wrapped = dayIndex % year;
        if (wrapped < 0)
            wrapped += year;
        return wrapped;
    }

    public static WorldSeason Season(int dayIndex, int daysPerYear, int daysPerSeason)
    {
        int seasonLen = daysPerSeason < 1 ? WorldClockSettings.DefaultDaysPerSeason : daysPerSeason;
        int doy = DayOfYear(dayIndex, daysPerYear);
        int index = doy / seasonLen;
        if (index < 0)
            index = 0;
        if (index >= SeasonCount)
            index = SeasonCount - 1;
        return (WorldSeason)index;
    }

    /// <summary>
    /// Inclusive day-span [plantedDayIndex, currentDayIndex] contains <paramref name="season"/>.
    /// Day-granularity only — not per-minute.
    /// </summary>
    public static bool SpanIncludesSeason(
        int plantedDayIndex,
        int currentDayIndex,
        WorldSeason season,
        int daysPerYear,
        int daysPerSeason)
    {
        if (currentDayIndex < plantedDayIndex)
            return false;

        int year = daysPerYear < 1 ? WorldClockSettings.DefaultDaysPerYear : daysPerYear;
        int spanInclusive = currentDayIndex - plantedDayIndex + 1;
        if (spanInclusive >= year)
            return true;

        for (int day = plantedDayIndex; day <= currentDayIndex; day++)
        {
            if (Season(day, year, daysPerSeason) == season)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Inclusive day-span [startDayIndex, endDayIndex]: count days whose season equals <paramref name="season"/>.
    /// </summary>
    public static int CountDaysInSeason(
        int startDayIndex,
        int endDayIndex,
        WorldSeason season,
        int daysPerYear,
        int daysPerSeason)
    {
        if (endDayIndex < startDayIndex)
            return 0;

        int count = 0;
        for (int day = startDayIndex; day <= endDayIndex; day++)
        {
            if (Season(day, daysPerYear, daysPerSeason) == season)
                count++;
        }

        return count;
    }

    /// <summary>
    /// Elapsed minutes minus winter days (day granularity) between planted and current world minutes.
    /// </summary>
    public static int ElapsedMinutesExcludingSeason(
        int startWorldMinute,
        int endWorldMinute,
        WorldSeason excludedSeason,
        int minutesPerDay,
        int daysPerYear,
        int daysPerSeason)
    {
        if (endWorldMinute <= startWorldMinute || minutesPerDay < 1)
            return 0;

        int startDay = startWorldMinute / minutesPerDay;
        int endDay = endWorldMinute / minutesPerDay;
        int winterDays = CountDaysInSeason(startDay, endDay, excludedSeason, daysPerYear, daysPerSeason);
        int elapsed = endWorldMinute - startWorldMinute;
        int excluded = winterDays * minutesPerDay;
        return elapsed > excluded ? elapsed - excluded : 0;
    }
}
