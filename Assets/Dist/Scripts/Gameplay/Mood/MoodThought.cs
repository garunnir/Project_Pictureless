// ============================================================
// MoodThought — 런타임 사고 한 줄 (offset + 남은 분)
// ============================================================

public readonly struct MoodThought
{
    public readonly ThoughtId Id;
    public readonly MoodThoughtKind Kind;
    public readonly int Offset;
    public readonly int RemainingMinutes;

    public MoodThought(ThoughtId id, MoodThoughtKind kind, int offset, int remainingMinutes)
    {
        Id = id;
        Kind = kind;
        Offset = offset;
        RemainingMinutes = remainingMinutes;
    }
}
