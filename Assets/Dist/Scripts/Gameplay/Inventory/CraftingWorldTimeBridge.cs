// ============================================================
// CraftingWorldTimeBridge — pushes WorldClock into CraftingWorldTime
// ============================================================

using UnityEngine;

[DisallowMultipleComponent]
public sealed class CraftingWorldTimeBridge : MonoBehaviour
{
    void OnEnable()
    {
        WorldClock clock = WorldClock.Instance;
        if (clock != null)
        {
            clock.MinuteChanged += OnMinute;
            Sync(clock);
        }
    }

    void Start()
    {
        WorldClock clock = WorldClock.Instance;
        if (clock != null)
        {
            clock.MinuteChanged -= OnMinute;
            clock.MinuteChanged += OnMinute;
            Sync(clock);
        }
    }

    void OnDisable()
    {
        WorldClock clock = WorldClock.Instance;
        if (clock != null)
            clock.MinuteChanged -= OnMinute;
    }

    void OnMinute()
    {
        WorldClock clock = WorldClock.Instance;
        if (clock != null)
            Sync(clock);
    }

    static void Sync(WorldClock clock)
    {
        int minutesPerDay = clock.Settings != null
            ? clock.Settings.MinutesPerDay
            : WorldClockSettings.DefaultMinutesPerDay;
        CraftingWorldTime.AbsoluteWorldMinute =
            clock.DayIndex * minutesPerDay + clock.MinuteOfDay;
    }
}
