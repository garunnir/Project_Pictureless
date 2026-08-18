// ============================================================
// TimeScaleHudLabels — 배속 HUD 문구 SSOT
// ============================================================

public static class TimeScaleHudLabels
{
    const string KeyPause = "TimeScale.Pause";
    const string KeyNormal = "TimeScale.Normal";
    const string KeyDouble = "TimeScale.Double";
    const string KeySmart = "TimeScale.Smart";

    public static string Pause => Loc.Get(KeyPause);
    public static string Normal => Loc.Get(KeyNormal);
    public static string Double => Loc.Get(KeyDouble);
    public static string Smart => Loc.Get(KeySmart);
}
