// ============================================================
// MoodThoughtLabels — 기분 수치·사고 목록 문구
// ============================================================

using System.Collections.Generic;
using System.Text;
using Garunnir.Runtime.Gameplay.Data;

public static class MoodThoughtLabels
{
    public const string KeySection = "PlayerStatus.ThoughtsSection";
    public const string KeyMoodFormat = "PlayerStatus.MoodNeed.Format";
    public const string KeyThoughtLine = "PlayerStatus.Thought.Line";
    public const string KeyThoughtPrefix = "PlayerStatus.Thought.";
    public const string KeyBreakPrefix = "PlayerStatus.Thought.Breaking";
    public const string KeyBreakWander = "PlayerStatus.Thought.BreakingWander";

    static readonly StringBuilder Builder = new(256);

    public static string ThoughtsSection => Loc.Get(KeySection);

    public static string GetThoughtName(ThoughtId id)
    {
        string key = KeyThoughtPrefix + id;
        if (Loc.TryGet(key, out string text))
            return text;
        return id.ToString();
    }

    public static string FormatMoodValue(float mood)
    {
        return Loc.Format(KeyMoodFormat, MathfRound(mood));
    }

    public static string FormatThoughtLine(MoodThought thought)
    {
        return Loc.Format(KeyThoughtLine, FormatOffset(thought.Offset), GetThoughtName(thought.Id));
    }

    public static string FormatHudTooltip(float mood, IReadOnlyList<MoodThought> thoughts, MoodBreakKind breakKind)
    {
        Builder.Length = 0;
        Builder.Append(FormatMoodValue(mood));
        if (breakKind != MoodBreakKind.None)
        {
            Builder.Append('\n');
            Builder.Append(FormatBreakLabel(breakKind));
        }

        if (thoughts != null)
        {
            for (int i = 0; i < thoughts.Count; i++)
            {
                Builder.Append('\n');
                Builder.Append(FormatThoughtLine(thoughts[i]));
            }
        }

        return Builder.ToString();
    }

    public static void AppendStatusLines(
        List<string> lines,
        float mood,
        IReadOnlyList<MoodThought> thoughts,
        MoodBreakKind breakKind)
    {
        if (lines == null)
            return;

        lines.Add(ThoughtsSection);
        lines.Add(FormatMoodValue(mood));
        if (breakKind != MoodBreakKind.None)
            lines.Add(FormatBreakLabel(breakKind));

        if (thoughts == null)
            return;

        for (int i = 0; i < thoughts.Count; i++)
            lines.Add(FormatThoughtLine(thoughts[i]));
    }

    public static string FormatBreakLabel(MoodBreakKind breakKind)
    {
        if (breakKind == MoodBreakKind.None)
            return string.Empty;

        string key = KeyBreakPrefix + breakKind;
        if (Loc.TryGet(key, out string breaking))
            return breaking;
        return breakKind.ToString();
    }

    public static MoodIconId ResolveMoodIcon(float mood)
    {
        if (mood >= 80f)
            return MoodIconId.VeryHappy;
        if (mood >= 65f)
            return MoodIconId.Happy;
        if (mood >= 55f)
            return MoodIconId.SlightlyHappy;
        if (mood >= 45f)
            return MoodIconId.Neutral;
        if (mood >= 35f)
            return MoodIconId.SlightlySad;
        if (mood >= 25f)
            return MoodIconId.Sad;
        if (mood >= 15f)
            return MoodIconId.VerySad;
        return MoodIconId.Depressed;
    }

    public static MoodPolarity ResolveMoodPolarity(float mood)
    {
        if (mood >= 55f)
            return MoodPolarity.Positive;
        if (mood <= 35f)
            return MoodPolarity.Negative;
        return MoodPolarity.Neutral;
    }

    static string FormatOffset(int offset)
    {
        if (offset > 0)
            return "+" + offset;
        return offset.ToString();
    }

    static int MathfRound(float mood) => UnityEngine.Mathf.RoundToInt(mood);
}
