// ============================================================
// MoodSettings — 기분 기준값·붕괴·사고 표 SSOT
// ============================================================

using System;
using UnityEngine;

[CreateAssetMenu(fileName = "MoodSettings", menuName = "Dist/Mood/Settings")]
public sealed class MoodSettings : ScriptableObject
{
    public const string DefaultAssetPath =
        "Assets/Dist/SOData/Gameplay/Mood/MoodSettings.asset";

    public const float DefaultBaseMood = 50f;
    public const float DefaultMoodMin = 0f;
    public const float DefaultMoodMax = 100f;
    public const float DefaultBreakThreshold = 35f;
    /// <summary>림월드: minor 문턱의 4/7 ≈ 20 (threshold 35).</summary>
    public const float MajorBreakThresholdFactor = 4f / 7f;
    /// <summary>림월드: minor 문턱의 1/7 = 5 (threshold 35).</summary>
    public const float ExtremeBreakThresholdFactor = 1f / 7f;
    public const float DefaultMinorBreakMtbDays = 10f;
    public const float DefaultMajorBreakMtbDays = 3f;
    public const float DefaultExtremeBreakMtbDays = 0.7f;
    public const int DefaultWanderDurationMinutes = 45;
    public const float DefaultWanderRadius = 6f;
    public const float DefaultWanderStoppingDistance = 0.35f;

    public const int DefaultPainOffset = -8;
    public const int DefaultSeverePainOffset = -16;
    public const int DefaultHungryOffset = -6;
    public const int DefaultVeryHungryOffset = -12;
    public const int DefaultThirstyOffset = -6;
    public const int DefaultVeryThirstyOffset = -12;
    public const int DefaultTooColdOffset = -6;
    public const int DefaultTooHotOffset = -6;
    public const int DefaultHypothermiaOffset = -16;
    public const int DefaultBleedOffset = -10;
    public const int DefaultOverencumberedOffset = -8;
    public const int DefaultAteMealOffset = 5;
    public const int DefaultAteMealMinutes = 180;
    public const int DefaultVomitedOffset = -12;
    public const int DefaultVomitedMinutes = 240;
    public const int DefaultAteRottenOffset = -10;
    public const int DefaultAteRottenMinutes = 360;
    public const int DefaultCatharsisOffset = 15;
    public const int DefaultCatharsisMinutes = 240;
    public const int DefaultMemoryStackLimit = 1;

    [Serializable]
    public sealed class ThoughtRow
    {
        public ThoughtId id;
        public MoodThoughtKind kind;
        public int offset;
        public int durationMinutes;
        public int stackLimit = DefaultMemoryStackLimit;
    }

    [Header("Mood")]
    [SerializeField] float _baseMood = DefaultBaseMood;
    [SerializeField] float _moodMin = DefaultMoodMin;
    [SerializeField] float _moodMax = DefaultMoodMax;

    [Header("Break")]
    [SerializeField] float _breakThreshold = DefaultBreakThreshold;
    [SerializeField] float _minorBreakMtbDays = DefaultMinorBreakMtbDays;
    [SerializeField] float _majorBreakMtbDays = DefaultMajorBreakMtbDays;
    [SerializeField] float _extremeBreakMtbDays = DefaultExtremeBreakMtbDays;
    [SerializeField] int _wanderDurationMinutes = DefaultWanderDurationMinutes;
    [SerializeField] float _wanderRadius = DefaultWanderRadius;
    [SerializeField] float _wanderStoppingDistance = DefaultWanderStoppingDistance;

    [Header("Thoughts")]
    [SerializeField] ThoughtRow[] _thoughts;

    public float BaseMood => _baseMood;
    public float MoodMin => _moodMin;
    public float MoodMax => _moodMax;
    public float BreakThreshold => _breakThreshold;
    public float MajorBreakThreshold => _breakThreshold * MajorBreakThresholdFactor;
    public float ExtremeBreakThreshold => _breakThreshold * ExtremeBreakThresholdFactor;
    public float MinorBreakMtbDays => Mathf.Max(0.01f, _minorBreakMtbDays);
    public float MajorBreakMtbDays => Mathf.Max(0.01f, _majorBreakMtbDays);
    public float ExtremeBreakMtbDays => Mathf.Max(0.01f, _extremeBreakMtbDays);
    public int WanderDurationMinutes => Mathf.Max(1, _wanderDurationMinutes);
    public float WanderRadius => Mathf.Max(0.1f, _wanderRadius);
    public float WanderStoppingDistance => Mathf.Max(0f, _wanderStoppingDistance);

    public bool TryGetThought(ThoughtId id, out ThoughtRow row)
    {
        EnsureThoughts();
        for (int i = 0; i < _thoughts.Length; i++)
        {
            ThoughtRow candidate = _thoughts[i];
            if (candidate == null || candidate.id != id)
                continue;

            row = candidate;
            return true;
        }

        row = null;
        return false;
    }

    void OnEnable() => EnsureThoughts();

    void Reset() => _thoughts = CreateDefaultThoughts();

    void EnsureThoughts()
    {
        if (_thoughts != null && _thoughts.Length > 0)
            return;

        _thoughts = CreateDefaultThoughts();
    }

    static ThoughtRow[] CreateDefaultThoughts()
    {
        return new[]
        {
            Situational(ThoughtId.Pain, DefaultPainOffset),
            Situational(ThoughtId.SeverePain, DefaultSeverePainOffset),
            Situational(ThoughtId.Hungry, DefaultHungryOffset),
            Situational(ThoughtId.VeryHungry, DefaultVeryHungryOffset),
            Situational(ThoughtId.Thirsty, DefaultThirstyOffset),
            Situational(ThoughtId.VeryThirsty, DefaultVeryThirstyOffset),
            Situational(ThoughtId.TooCold, DefaultTooColdOffset),
            Situational(ThoughtId.TooHot, DefaultTooHotOffset),
            Situational(ThoughtId.Hypothermia, DefaultHypothermiaOffset),
            Situational(ThoughtId.Bleed, DefaultBleedOffset),
            Situational(ThoughtId.Overencumbered, DefaultOverencumberedOffset),
            Memory(ThoughtId.AteMeal, DefaultAteMealOffset, DefaultAteMealMinutes),
            Memory(ThoughtId.Vomited, DefaultVomitedOffset, DefaultVomitedMinutes),
            Memory(ThoughtId.AteRotten, DefaultAteRottenOffset, DefaultAteRottenMinutes),
            Memory(ThoughtId.Catharsis, DefaultCatharsisOffset, DefaultCatharsisMinutes)
        };
    }

    static ThoughtRow Situational(ThoughtId id, int offset)
    {
        return new ThoughtRow
        {
            id = id,
            kind = MoodThoughtKind.Situational,
            offset = offset,
            durationMinutes = 0,
            stackLimit = 1
        };
    }

    static ThoughtRow Memory(ThoughtId id, int offset, int minutes)
    {
        return new ThoughtRow
        {
            id = id,
            kind = MoodThoughtKind.Memory,
            offset = offset,
            durationMinutes = minutes,
            stackLimit = DefaultMemoryStackLimit
        };
    }
}
