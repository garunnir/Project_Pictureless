// ============================================================
// WorldWeatherHost — 월드 WeatherKind SSOT + 글로벌 스케줄러 (Phase D: TryGetKindAt)
// ============================================================

using System;
using UnityEngine;

[DefaultExecutionOrder(-90)]
public sealed class WorldWeatherHost : SceneSingleton<WorldWeatherHost>
{
    [SerializeField] WeatherKind _kind = WeatherKind.Clear;
    [SerializeField] WorldWeatherSettings _settings;
    [SerializeField]
    [Tooltip("When true, rolls a new Kind after MinDurationWorldMinutes on WorldClock minutes.")]
    bool _schedulerEnabled = true;

    int _minutesOnCurrentKind;
    WorldClock _subscribedClock;

    public event Action WeatherKindChanged;

    public WorldWeatherSettings Settings => _settings;
    public WeatherKind CurrentKind => _kind;
    public bool SchedulerEnabled
    {
        get => _schedulerEnabled;
        set => _schedulerEnabled = value;
    }

    public int MinutesOnCurrentKind => _minutesOnCurrentKind;

    protected override void Awake()
    {
        base.Awake();
        _minutesOnCurrentKind = 0;
    }

    void OnEnable() => BindClock();

    void OnDisable() => UnbindClock();

    void Update()
    {
        if (_subscribedClock == null && WorldClock.Instance != null)
            BindClock();
    }

    public void SetSettings(WorldWeatherSettings settings) => _settings = settings;

    /// <summary>
    /// Phase 1 stub: always returns <see cref="CurrentKind"/>.
    /// Phase D: sample WeatherField at world cell (x, z).
    /// </summary>
    public bool TryGetKindAt(int x, int z, out WeatherKind kind)
    {
        kind = _kind;
        return true;
    }

    public void SetKind(WeatherKind kind, WeatherChangeReason reason = WeatherChangeReason.Manual)
    {
        if (_kind == kind)
        {
            if (reason == WeatherChangeReason.Scheduler)
                _minutesOnCurrentKind = 0;
            return;
        }

        _kind = kind;
        _minutesOnCurrentKind = 0;
        WeatherKindChanged?.Invoke();
    }

    void BindClock()
    {
        UnbindClock();
        WorldClock clock = WorldClock.Instance;
        if (clock == null)
            return;
        _subscribedClock = clock;
        _subscribedClock.MinuteChanged += OnClockMinuteChanged;
    }

    void UnbindClock()
    {
        if (_subscribedClock == null)
            return;
        _subscribedClock.MinuteChanged -= OnClockMinuteChanged;
        _subscribedClock = null;
    }

    void OnClockMinuteChanged()
    {
        if (!_schedulerEnabled || _settings == null)
            return;

        WorldClock clock = WorldClock.Instance;
        if (clock == null)
            return;

        _minutesOnCurrentKind++;
        if (_minutesOnCurrentKind < _settings.MinDurationWorldMinutes)
            return;

        int daysPerYear = WorldClockSettings.DefaultDaysPerYear;
        int daysPerSeason = WorldClockSettings.DefaultDaysPerSeason;
        if (clock.Settings != null)
        {
            daysPerYear = clock.Settings.DaysPerYear;
            daysPerSeason = clock.Settings.DaysPerSeason;
        }

        WorldSeason season = WorldCalendar.Season(clock.DayIndex, daysPerYear, daysPerSeason);
        int seed = clock.DayIndex * 1440 + clock.MinuteOfDay + (int)_kind * 17;
        WeatherKind next = _settings.PickKind(season, seed);
        SetKind(next, WeatherChangeReason.Scheduler);
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!Application.isPlaying)
            return;
        // Inspector Kind edits in Play still raise consumers via Changed path on next read.
    }
#endif
}
