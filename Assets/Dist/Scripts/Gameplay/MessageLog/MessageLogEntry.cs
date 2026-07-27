// ============================================================
// MessageLogEntry — 메시지 로그 한 줄 스냅샷
// ============================================================

public readonly struct MessageLogEntry
{
    public readonly MessageLogCategory Category;
    public readonly MessageLogImportance Importance;
    public readonly string Text;

    public MessageLogEntry(
        MessageLogCategory category,
        MessageLogImportance importance,
        string text)
    {
        Category = category;
        Importance = importance;
        Text = text ?? string.Empty;
    }
}
