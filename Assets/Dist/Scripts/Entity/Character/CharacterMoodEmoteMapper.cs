// ============================================================
// CharacterMoodEmoteMapper — 기분 수치 → EmoteId (MoodThoughtLabels 밴드 SSOT)
// ============================================================

using Garunnir.Runtime.Gameplay.Data;

public static class CharacterMoodEmoteMapper
{
    public static EmoteId FromMood(float mood) =>
        FromMoodIcon(MoodThoughtLabels.ResolveMoodIcon(mood));

    public static EmoteId FromMoodIcon(MoodIconId iconId) =>
        iconId switch
        {
            MoodIconId.VeryHappy => EmoteId.MoodVeryHappy,
            MoodIconId.Happy => EmoteId.MoodHappy,
            MoodIconId.SlightlyHappy => EmoteId.MoodSlightlyHappy,
            MoodIconId.Neutral => EmoteId.MoodNeutral,
            MoodIconId.SlightlySad => EmoteId.MoodSlightlySad,
            MoodIconId.Sad => EmoteId.MoodSad,
            MoodIconId.VerySad => EmoteId.MoodVerySad,
            MoodIconId.Depressed => EmoteId.MoodDepressed,
            _ => EmoteId.MoodNeutral,
        };
}
