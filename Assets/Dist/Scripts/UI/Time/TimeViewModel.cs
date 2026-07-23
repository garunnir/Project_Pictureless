// ============================================================
// TimeViewModel — WorldClock 구독 + HUD 표시용 스냅샷
// ============================================================

using System;

public sealed class TimeViewModel
{
    WorldClock _clock;

    public event Action Changed;

    public int DayIndex { get; private set; }
    public int HourOfDay { get; private set; }
    public int MinuteOfHour { get; private set; }
    public DayPeriod Period { get; private set; }

    public string DisplayText =>
        TimeDisplayFormat.Format(DayIndex, HourOfDay, MinuteOfHour);

    public void Bind(WorldClock clock)
    {
        Unbind();
        _clock = clock;
        if (_clock != null)
        {
            _clock.MinuteChanged += OnClockChanged;
            _clock.DayChanged += OnClockChanged;
            _clock.PeriodChanged += OnClockChanged;
        }

        Snapshot();
        Changed?.Invoke();
    }

    public void Unbind()
    {
        if (_clock != null)
        {
            _clock.MinuteChanged -= OnClockChanged;
            _clock.DayChanged -= OnClockChanged;
            _clock.PeriodChanged -= OnClockChanged;
        }

        _clock = null;
    }

    void OnClockChanged()
    {
        Snapshot();
        Changed?.Invoke();
    }

    void Snapshot()
    {
        if (_clock == null)
        {
            DayIndex = 0;
            HourOfDay = 0;
            MinuteOfHour = 0;
            Period = DayPeriod.Night;
            return;
        }

        DayIndex = _clock.DayIndex;
        HourOfDay = _clock.HourOfDay;
        MinuteOfHour = _clock.MinuteOfHour;
        Period = _clock.Period;
    }
}
