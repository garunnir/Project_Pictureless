// ============================================================
// GameSaveSlotMetaDto — 슬롯 목록 UI용 메타 (맵 JSON과 분리)
// ============================================================

using System;

[Serializable]
public sealed class GameSaveSlotMetaDto
{
    public bool hasData;
    public long savedAtUtcTicks;
    public int dayIndex;
    public int minuteOfDay;
    public bool hasClockSnapshot;
}
