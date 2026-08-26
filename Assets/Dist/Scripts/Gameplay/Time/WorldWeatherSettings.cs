// ============================================================
// WorldWeatherSettings — 계절별 Kind 가중치·최소 지속 (글로벌 스케줄러 입력)
// ============================================================

using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "WorldWeatherSettings",
    menuName = "Dist/Time/World Weather Settings")]
public sealed class WorldWeatherSettings : ScriptableObject
{
    public const string DefaultAssetPath =
        "Assets/Dist/SOData/Gameplay/Time/WorldWeatherSettings.asset";

    public const int DefaultMinDurationWorldMinutes = 60;

    [Serializable]
    public struct SeasonWeights
    {
        [Min(0f)] public float Clear;
        [Min(0f)] public float Rain;
        [Min(0f)] public float Wind;
        [Min(0f)] public float Snow;

        public float Total => Clear + Rain + Wind + Snow;

        public float WeightOf(WeatherKind kind)
        {
            switch (kind)
            {
                case WeatherKind.Rain:
                    return Rain;
                case WeatherKind.Wind:
                    return Wind;
                case WeatherKind.Snow:
                    return Snow;
                default:
                    return Clear;
            }
        }
    }

    [SerializeField]
    [Tooltip("Same Kind must last at least this many world minutes before a scheduler roll.")]
    int _minDurationWorldMinutes = DefaultMinDurationWorldMinutes;

    [Header("Season Kind Weights (relative)")]
    [SerializeField] SeasonWeights _spring = new SeasonWeights
    {
        Clear = 4f,
        Rain = 3f,
        Wind = 2f,
        Snow = 0f
    };
    [SerializeField] SeasonWeights _summer = new SeasonWeights
    {
        Clear = 5f,
        Rain = 2f,
        Wind = 1f,
        Snow = 0f
    };
    [SerializeField] SeasonWeights _autumn = new SeasonWeights
    {
        Clear = 3f,
        Rain = 3f,
        Wind = 3f,
        Snow = 0.5f
    };
    [SerializeField] SeasonWeights _winter = new SeasonWeights
    {
        Clear = 2f,
        Rain = 0.5f,
        Wind = 3f,
        Snow = 4f
    };

    public int MinDurationWorldMinutes =>
        Mathf.Max(1, _minDurationWorldMinutes);

    public SeasonWeights WeightsFor(WorldSeason season)
    {
        switch (season)
        {
            case WorldSeason.Summer:
                return _summer;
            case WorldSeason.Autumn:
                return _autumn;
            case WorldSeason.Winter:
                return _winter;
            default:
                return _spring;
        }
    }

    /// <summary>Weighted pick. Zero-total → Clear. Deterministic when seed is fixed.</summary>
    public WeatherKind PickKind(WorldSeason season, int seed)
    {
        SeasonWeights weights = WeightsFor(season);
        float total = weights.Total;
        if (total <= 0f)
            return WeatherKind.Clear;

        // Deterministic unit interval from seed (not UnityEngine.Random).
        uint u = (uint)seed * 1664525u + 1013904223u;
        float roll = (u & 0xFFFFFF) / (float)0x1000000 * total;
        float cursor = 0f;
        cursor += weights.Clear;
        if (roll < cursor)
            return WeatherKind.Clear;
        cursor += weights.Rain;
        if (roll < cursor)
            return WeatherKind.Rain;
        cursor += weights.Wind;
        if (roll < cursor)
            return WeatherKind.Wind;
        return WeatherKind.Snow;
    }
}
