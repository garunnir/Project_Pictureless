// ============================================================
// MoodGameplayGate — 붕괴 중 행동 창·손 작업 차단
// ============================================================

public static class MoodGameplayGate
{
    public static bool IsBlocked =>
        CharacterMoodHost.Active != null && CharacterMoodHost.Active.IsControlYielded;
}
