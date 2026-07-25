// ============================================================
// WorldClock — 인게임 일수·시각 진행 SSOT (World 채널 delta 소비)
// ============================================================

using System;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public sealed class WorldClock : SceneSingleton<WorldClock>
{
    [SerializeField] WorldClockSettings _settings;

    int _dayIndex;
    int _minuteOfDay;
    float _minuteAccumulator;
    DayPeriod _period;
    bool _initialized;

    public event Action MinuteChanged;
    public event Action DayChanged;
    public event Action PeriodChanged;

    public WorldClockSettings Settings => _settings;
    public int DayIndex => _dayIndex;
    public int MinuteOfDay => _minuteOfDay;
    public DayPeriod Period => _period;

    public int HourOfDay
    {
        get
        {
            int minutesPerDay = GetMinutesPerDay();
            int minutesPerHour = Mathf.Max(1, minutesPerDay / 24);
            return _minuteOfDay / minutesPerHour;
        }
    }

    public int MinuteOfHour
    {
        get
        {
            int minutesPerDay = GetMinutesPerDay();
            int minutesPerHour = Mathf.Max(1, minutesPerDay / 24);
            return _minuteOfDay % minutesPerHour;
        }
    }

    /// <summary>하루 진행도 [0, 1). MinuteOfDay + 분 미만 accumulator.</summary>
    public float DayNormalized
    {
        get
        {
            int minutesPerDay = GetMinutesPerDay();
            float t = (_minuteOfDay + _minuteAccumulator) / minutesPerDay;
            return t < 1f ? Mathf.Max(0f, t) : 0f;
        }
    }

    protected override void Awake()
    {
        base.Awake();
        EnsureInitialized();
    }

    void Update()
    {
        EnsureInitialized();
        if (_settings == null)
            return;

        TimeScaleService scales = TimeScaleService.Instance;
        if (scales == null)
            return;

        float worldDelta = scales.GetDelta(TimeScaleChannel.World);
        if (worldDelta <= 0f)
            return;

        float rate = _settings.WorldMinutesPerRealtimeSecond;
        if (rate <= 0f)
            return;

        AdvanceMinutes(worldDelta * rate);
    }

    public void SetSettings(WorldClockSettings settings)
    {
        _settings = settings;
        _initialized = false;
        EnsureInitialized();
    }

    public void SetTime(int dayIndex, int minuteOfDay)
    {
        EnsureInitialized();
        int minutesPerDay = GetMinutesPerDay();
        int clampedMinute = Mathf.Clamp(minuteOfDay, 0, minutesPerDay - 1);
        bool dayChanged = dayIndex != _dayIndex;
        bool minuteChanged = clampedMinute != _minuteOfDay;

        _dayIndex = dayIndex;
        _minuteOfDay = clampedMinute;
        _minuteAccumulator = 0f;

        if (dayChanged)
            DayChanged?.Invoke();
        if (minuteChanged || dayChanged)
            MinuteChanged?.Invoke();

        RaisePeriodIfChanged();
    }

    void EnsureInitialized()
    {
        if (_initialized)
            return;

        if (_settings != null)
        {
            _dayIndex = _settings.StartingDayIndex;
            _minuteOfDay = _settings.StartingMinuteOfDay;
            _period = _settings.ResolvePeriod(_minuteOfDay);
        }
        else
        {
            _dayIndex = 1;
            _minuteOfDay = 0;
            _period = DayPeriod.Night;
        }

        _minuteAccumulator = 0f;
        _initialized = true;
    }

    void AdvanceMinutes(float deltaWorldMinutes)
    {
        if (deltaWorldMinutes <= 0f)
            return;

        int minutesPerDay = GetMinutesPerDay();
        _minuteAccumulator += deltaWorldMinutes;

        int wholeMinutes = (int)_minuteAccumulator;
        if (wholeMinutes <= 0)
            return;

        _minuteAccumulator -= wholeMinutes;
        int newMinute = _minuteOfDay + wholeMinutes;
        int daysAdvanced = 0;
        while (newMinute >= minutesPerDay)
        {
            newMinute -= minutesPerDay;
            daysAdvanced++;
        }

        _minuteOfDay = newMinute;
        if (daysAdvanced > 0)
        {
            _dayIndex += daysAdvanced;
            DayChanged?.Invoke();
        }

        MinuteChanged?.Invoke();
        RaisePeriodIfChanged();
    }

    void RaisePeriodIfChanged()
    {
        DayPeriod next = _settings != null
            ? _settings.ResolvePeriod(_minuteOfDay)
            : DayPeriod.Night;
        if (next == _period)
            return;
        _period = next;
        PeriodChanged?.Invoke();
    }

    int GetMinutesPerDay() =>
        _settings != null ? _settings.MinutesPerDay : WorldClockSettings.DefaultMinutesPerDay;
}
