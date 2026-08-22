// ============================================================
// ConsumeDuration — Eat/Drink mealtime(초) SSOT
// ============================================================

/// <summary>
/// BN <c>consumption.cpp</c> mealtime=250 moves. 아이템 JSON 필드 없음.
/// Use/MED는 mealtime 없음. 인출 시간은 Wield 단계(<see cref="InventoryTransferDuration"/>).
/// </summary>
public static class ConsumeDuration
{
    public const int MealtimeMoves = 250;

    public static float MealtimeSeconds =>
        MealtimeMoves / CombatMath.MovesPerSecond;

    public static float ActSeconds(ConsumeKind kind) =>
        kind == ConsumeKind.Use ? 0f : MealtimeSeconds;
}
