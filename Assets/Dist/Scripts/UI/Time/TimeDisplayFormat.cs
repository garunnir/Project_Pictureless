// ============================================================
// TimeDisplayFormat — 시계 HUD 표시 문자열 SSOT
// ============================================================

public static class TimeDisplayFormat
{
    public const string DayTimePattern = "Day {0}  {1:00}:{2:00}";

    public static string Format(int dayIndex, int hour, int minute) =>
        string.Format(DayTimePattern, dayIndex, hour, minute);
}
