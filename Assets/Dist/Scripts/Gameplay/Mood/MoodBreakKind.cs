// ============================================================
// MoodBreakKind — 정신붕괴 종류 (전투 FSM과 분리)
// ============================================================

public enum MoodBreakKind
{
    None = 0,
    Wander = 1,
    // Runtime behavior Pending — enum·로컬·HUD 라벨만 선행
    Flee = 2,
    Berserk = 3,
    Catatonic = 4
}
