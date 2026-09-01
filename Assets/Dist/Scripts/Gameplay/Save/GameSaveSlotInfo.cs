// ============================================================
// GameSaveSlotInfo — 슬롯 UI 표시용 런타임 스냅샷
// ============================================================

public readonly struct GameSaveSlotInfo
{
    public int Index { get; }
    public bool HasData { get; }
    public long SavedAtUtcTicks { get; }
    public int DayIndex { get; }
    public int MinuteOfDay { get; }
    public bool HasClockSnapshot { get; }

    public GameSaveSlotInfo(
        int index,
        bool hasData,
        long savedAtUtcTicks,
        int dayIndex,
        int minuteOfDay,
        bool hasClockSnapshot)
    {
        Index = index;
        HasData = hasData;
        SavedAtUtcTicks = savedAtUtcTicks;
        DayIndex = dayIndex;
        MinuteOfDay = minuteOfDay;
        HasClockSnapshot = hasClockSnapshot;
    }
}
