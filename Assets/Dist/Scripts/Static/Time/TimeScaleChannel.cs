// ============================================================
// TimeScaleChannel — 배속·일시정지·불릿타임용 시간 채널 SSOT
// ============================================================

public enum TimeScaleChannel
{
    /// <summary>UI/메뉴 연출. 항상 scale 1 (모디파이어 무시).</summary>
    Realtime = 0,

    /// <summary>월드·NPC·환경·WorldClock 진행.</summary>
    World = 1,

    /// <summary>플레이어 전용 (불릿타임 면제 등).</summary>
    Player = 2,
}
