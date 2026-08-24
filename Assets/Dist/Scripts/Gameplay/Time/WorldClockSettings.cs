// ============================================================
// WorldClockSettings — 하루 길이·시작 시각·시간대 경계·진행 배율 SSOT
// ============================================================

using UnityEngine;

[CreateAssetMenu(
    fileName = "WorldClockSettings",
    menuName = "Dist/Time/World Clock Settings")]
public sealed class WorldClockSettings : ScriptableObject
{
    public const int DefaultMinutesPerDay = 24 * 60;
    public const float DefaultWorldMinutesPerRealtimeSecond = 1f;
    public const int DefaultDaysPerYear = 364;
    public const int DefaultDaysPerSeason = 91;

    [SerializeField] int _minutesPerDay = DefaultMinutesPerDay;
    [SerializeField] int _startingDayIndex = 1;
    [SerializeField] int _startingMinuteOfDay;
    [SerializeField]
    [Tooltip("Realtime 1초당 진행되는 월드 분 (World 채널 scale=1 기준).")]
    float _worldMinutesPerRealtimeSecond = DefaultWorldMinutesPerRealtimeSecond;

    [Header("Calendar (derived season — DayIndex stays absolute)")]
    [SerializeField]
    [Tooltip("Absolute DayIndex wraps here for day-of-year / season. Does not change SetTime.")]
    int _daysPerYear = DefaultDaysPerYear;
    [SerializeField]
    [Tooltip("Named season length. Year is DaysPerSeason × 4 (default 91 × 4 = 364).")]
    int _daysPerSeason = DefaultDaysPerSeason;

    [Header("Day Period Boundaries (minute of day, inclusive start)")]
    [SerializeField] int _dawnStartMinute = 5 * 60;
    [SerializeField] int _dayStartMinute = 7 * 60;
    [SerializeField] int _duskStartMinute = 18 * 60;
    [SerializeField] int _nightStartMinute = 20 * 60;

    public int MinutesPerDay => Mathf.Max(1, _minutesPerDay);
    public int StartingDayIndex => _startingDayIndex;
    public int StartingMinuteOfDay =>
        Mathf.Clamp(_startingMinuteOfDay, 0, MinutesPerDay - 1);
    public float WorldMinutesPerRealtimeSecond =>
        Mathf.Max(0f, _worldMinutesPerRealtimeSecond);
    public int DaysPerYear => Mathf.Max(1, _daysPerYear);
    public int DaysPerSeason => Mathf.Max(1, _daysPerSeason);

    public int DawnStartMinute => ClampMinute(_dawnStartMinute);
    public int DayStartMinute => ClampMinute(_dayStartMinute);
    public int DuskStartMinute => ClampMinute(_duskStartMinute);
    public int NightStartMinute => ClampMinute(_nightStartMinute);

    public DayPeriod ResolvePeriod(int minuteOfDay)
    {
        int m = ClampMinute(minuteOfDay);
        // Night wraps across midnight: [NightStart .. DawnStart)
        if (m >= NightStartMinute || m < DawnStartMinute)
            return DayPeriod.Night;
        if (m >= DuskStartMinute)
            return DayPeriod.Dusk;
        if (m >= DayStartMinute)
            return DayPeriod.Day;
        return DayPeriod.Dawn;
    }

    int ClampMinute(int minute) =>
        Mathf.Clamp(minute, 0, MinutesPerDay - 1);
}
