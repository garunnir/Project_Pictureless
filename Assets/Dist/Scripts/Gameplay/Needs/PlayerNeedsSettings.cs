// ============================================================
// PlayerNeedsSettings — 플레이어 위장·저장 kcal·갈증·활동 BMR 비율 SSOT
// ============================================================

using UnityEngine;

[CreateAssetMenu(
    fileName = "PlayerNeedsSettings",
    menuName = "Dist/Needs/Player Needs Settings")]
public sealed class PlayerNeedsSettings : ScriptableObject
{
    public const int DefaultDailyKcalBurn = 2500;
    public const int DefaultMaxStoredKcal = 17500;
    public const float DefaultStomachCapacityMl = 1500f;
    public const float DefaultFoodDigestMlPerMinute = 8f;
    public const float DefaultWaterDigestMlPerMinute = 50f;
    public const float DefaultDigestKcalPerMinute = 7f;
    public const float DefaultDailyThirstDrain = 80f;
    public const float DefaultActivityMulIdle = 1f;
    public const float DefaultActivityMulWalk = 1.2f;
    public const float DefaultActivityMulBusy = 1.6f;
    public const float DefaultActivityMulSprint = 2f;
    public const float DefaultWalkActivitySpeedMin = 0.1f;

    public const float DefaultMoodOverateRatio = 0.9f;
    public const float DefaultMoodFedRatio = 0.25f;
    public const float DefaultMoodHungryStoredRatio = 0.45f;
    public const float DefaultMoodVeryHungryStoredRatio = 0.2f;
    public const float DefaultMoodStomachEmptyRatio = 0.05f;
    public const float DefaultMoodThirstQuenchedRatio = 0.75f;
    public const float DefaultMoodThirstyRatio = 0.45f;
    public const float DefaultMoodVeryThirstyRatio = 0.2f;
    public const float DefaultProseFullRatio = 0.75f;
    public const float DefaultProseOkRatio = 0.45f;
    public const float DefaultProseLowRatio = 0.2f;

    public const int DefaultBloatWorldMinutes = 5;
    public const int DefaultOvereatHit = 1;
    public const float DefaultVomitStomachFraction = 0.5f;
    public const int DefaultRotFunPenalty = -10;
    public const int DefaultRotHealthyPenalty = -3;
    public const int DefaultWarningIntervalWorldHours = 6;
    public const int DefaultWarningKcalPct70 = 70;
    public const int DefaultWarningKcalPct50 = 50;
    public const int DefaultWarningKcalPct25 = 25;
    public const int DefaultWarningKcalPct10 = 10;

    [Header("Stored Energy")]
    [SerializeField] int _dailyKcalBurn = DefaultDailyKcalBurn;
    [SerializeField] int _maxStoredKcal = DefaultMaxStoredKcal;

    [Header("Stomach")]
    [SerializeField] float _stomachCapacityMl = DefaultStomachCapacityMl;
    [SerializeField] float _foodDigestMlPerMinute = DefaultFoodDigestMlPerMinute;
    [SerializeField] float _waterDigestMlPerMinute = DefaultWaterDigestMlPerMinute;
    [SerializeField] float _digestKcalPerMinute = DefaultDigestKcalPerMinute;

    [Header("Thirst")]
    [SerializeField] float _dailyThirstDrain = DefaultDailyThirstDrain;

    [Header("Activity BMR")]
    [SerializeField] float _activityMulIdle = DefaultActivityMulIdle;
    [SerializeField] float _activityMulWalk = DefaultActivityMulWalk;
    [SerializeField] float _activityMulBusy = DefaultActivityMulBusy;
    [SerializeField] float _activityMulSprint = DefaultActivityMulSprint;
    [SerializeField] float _walkActivitySpeedMin = DefaultWalkActivitySpeedMin;

    [Header("Mood Ratios")]
    [SerializeField] float _moodOverateRatio = DefaultMoodOverateRatio;
    [SerializeField] float _moodFedRatio = DefaultMoodFedRatio;
    [SerializeField] float _moodHungryStoredRatio = DefaultMoodHungryStoredRatio;
    [SerializeField] float _moodVeryHungryStoredRatio = DefaultMoodVeryHungryStoredRatio;
    [SerializeField] float _moodStomachEmptyRatio = DefaultMoodStomachEmptyRatio;
    [SerializeField] float _moodThirstQuenchedRatio = DefaultMoodThirstQuenchedRatio;
    [SerializeField] float _moodThirstyRatio = DefaultMoodThirstyRatio;
    [SerializeField] float _moodVeryThirstyRatio = DefaultMoodVeryThirstyRatio;

    [Header("Prose Ratios")]
    [SerializeField] float _proseFullRatio = DefaultProseFullRatio;
    [SerializeField] float _proseOkRatio = DefaultProseOkRatio;
    [SerializeField] float _proseLowRatio = DefaultProseLowRatio;

    [Header("Bloat / Overeat")]
    [SerializeField] int _bloatWorldMinutes = DefaultBloatWorldMinutes;
    [SerializeField] int _overeatHit = DefaultOvereatHit;
    [SerializeField] float _vomitStomachFraction = DefaultVomitStomachFraction;

    [Header("Rot")]
    [SerializeField] int _rotFunPenalty = DefaultRotFunPenalty;
    [SerializeField] int _rotHealthyPenalty = DefaultRotHealthyPenalty;

    [Header("Needs Warnings")]
    [SerializeField] int _warningIntervalWorldHours = DefaultWarningIntervalWorldHours;
    [SerializeField] int _warningKcalPct70 = DefaultWarningKcalPct70;
    [SerializeField] int _warningKcalPct50 = DefaultWarningKcalPct50;
    [SerializeField] int _warningKcalPct25 = DefaultWarningKcalPct25;
    [SerializeField] int _warningKcalPct10 = DefaultWarningKcalPct10;

    public int DailyKcalBurn => Mathf.Max(0, _dailyKcalBurn);
    public int MaxStoredKcal => Mathf.Max(0, _maxStoredKcal);
    public float StomachCapacityMl => Mathf.Max(0f, _stomachCapacityMl);
    public float FoodDigestMlPerMinute => Mathf.Max(0f, _foodDigestMlPerMinute);
    public float WaterDigestMlPerMinute => Mathf.Max(0f, _waterDigestMlPerMinute);
    public float DigestKcalPerMinute => Mathf.Max(0f, _digestKcalPerMinute);
    public float DailyThirstDrain => Mathf.Max(0f, _dailyThirstDrain);
    public float ActivityMulIdle => Mathf.Max(0f, _activityMulIdle);
    public float ActivityMulWalk => Mathf.Max(0f, _activityMulWalk);
    public float ActivityMulBusy => Mathf.Max(0f, _activityMulBusy);
    public float ActivityMulSprint => Mathf.Max(0f, _activityMulSprint);
    public float WalkActivitySpeedMin => Mathf.Max(0f, _walkActivitySpeedMin);
    public float MoodOverateRatio => Mathf.Clamp01(_moodOverateRatio);
    public float MoodFedRatio => Mathf.Clamp01(_moodFedRatio);
    public float MoodHungryStoredRatio => Mathf.Clamp01(_moodHungryStoredRatio);
    public float MoodVeryHungryStoredRatio => Mathf.Clamp01(_moodVeryHungryStoredRatio);
    public float MoodStomachEmptyRatio => Mathf.Clamp01(_moodStomachEmptyRatio);
    public float MoodThirstQuenchedRatio => Mathf.Clamp01(_moodThirstQuenchedRatio);
    public float MoodThirstyRatio => Mathf.Clamp01(_moodThirstyRatio);
    public float MoodVeryThirstyRatio => Mathf.Clamp01(_moodVeryThirstyRatio);
    public float ProseFullRatio => Mathf.Clamp01(_proseFullRatio);
    public float ProseOkRatio => Mathf.Clamp01(_proseOkRatio);
    public float ProseLowRatio => Mathf.Clamp01(_proseLowRatio);
    public int BloatWorldMinutes => Mathf.Max(0, _bloatWorldMinutes);
    public int OvereatHit => Mathf.Max(0, _overeatHit);
    public float VomitStomachFraction => Mathf.Clamp01(_vomitStomachFraction);
    public int RotFunPenalty => _rotFunPenalty;
    public int RotHealthyPenalty => _rotHealthyPenalty;
    public int WarningIntervalWorldHours => Mathf.Max(0, _warningIntervalWorldHours);
    public int WarningKcalPct70 => Mathf.Clamp(_warningKcalPct70, 0, 100);
    public int WarningKcalPct50 => Mathf.Clamp(_warningKcalPct50, 0, 100);
    public int WarningKcalPct25 => Mathf.Clamp(_warningKcalPct25, 0, 100);
    public int WarningKcalPct10 => Mathf.Clamp(_warningKcalPct10, 0, 100);
}
