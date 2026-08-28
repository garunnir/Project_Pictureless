// ============================================================
// CharacterEmoteTypes — 이모트 소스·우선순위·요청 SSOT
// ============================================================

public enum EmoteSource
{
    None = 0,
    Mood = 1,
    Combat = 2,
    Dialogue = 3,
}

public enum EmotePriority
{
    Mood = 0,
    Dialogue = 1,
    Combat = 2,
}

public enum EmoteHideReason
{
    None = 0,
    NoActiveEmote = 1,
    CatalogMissing = 2,
    ObserverOnlyOnPossessed = 3,
    SightFadeHidden = 4,
}

public readonly struct EmoteRequest
{
    public EmoteId Id { get; }
    public EmoteSource Source { get; }
    public float? DurationSeconds { get; }

    public EmoteRequest(EmoteId id, EmoteSource source, float? durationSeconds = null)
    {
        Id = id;
        Source = source;
        DurationSeconds = durationSeconds;
    }

    public EmotePriority Priority => EmotePriorityUtility.FromSource(Source);
}

public static class EmotePriorityUtility
{
    public static EmotePriority FromSource(EmoteSource source) =>
        source switch
        {
            EmoteSource.Combat => EmotePriority.Combat,
            EmoteSource.Dialogue => EmotePriority.Dialogue,
            _ => EmotePriority.Mood,
        };
}
