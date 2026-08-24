// ============================================================
// PlantGrowth — seed grow_minutes 대비 경과로 stage/wither 판정 SSOT
// ============================================================

namespace Garunnir.Runtime.Gameplay.Data
{
    public enum PlantGrowthStage
    {
        Seed = 0,
        Seedling = 1,
        Mature = 2,
        Harvestable = 3,
        Withered = 4
    }

    /// <summary>
    /// Fertilizer (once) and weather Kind apply to effective grow.
    /// Frost kill is a precomputed day-span flag — not climate frostbite.
    /// </summary>
    public readonly struct PlantGrowthContext
    {
        public readonly bool Fertilized;
        public readonly float WeatherGrowFactor;
        public readonly bool FrostKills;

        public PlantGrowthContext(bool fertilized, float weatherGrowFactor, bool frostKills)
        {
            Fertilized = fertilized;
            WeatherGrowFactor = weatherGrowFactor;
            FrostKills = frostKills;
        }

        public static readonly PlantGrowthContext Default =
            new PlantGrowthContext(false, 1f, false);
    }

    public static class PlantGrowth
    {
        public const float SeedlingAtGrowFraction = 0.25f;
        public const float MatureAtGrowFraction = 0.75f;

        /// <summary>harvestable 이후 시들기까지 여유(월드 분). SSOT.</summary>
        public const int WitherSlackMinutes = 24 * 60;

        /// <summary>Once per plant. Multiplies required grow minutes (&lt;1 = faster).</summary>
        public const float FertilizerGrowFactor = 0.5f;

        /// <summary>Clear Kind: required grow unchanged.</summary>
        public const float WeatherClearGrowFactor = 1f;

        /// <summary>Rain Kind: required grow reduced (faster).</summary>
        public const float WeatherRainGrowFactor = 0.75f;

        /// <summary>Wind Kind: required grow increased (slower).</summary>
        public const float WeatherWindGrowFactor = 1.25f;

        public static int ElapsedMinutes(int plantedWorldMinute, int currentWorldMinute)
        {
            if (currentWorldMinute < plantedWorldMinute)
                return 0;
            return currentWorldMinute - plantedWorldMinute;
        }

        public static float EffectiveGrowMinutes(SeedDetailData seed, in PlantGrowthContext context)
        {
            float grow = seed != null && seed.grow_minutes > 0f ? seed.grow_minutes : 1f;
            float weather = context.WeatherGrowFactor > 0f
                ? context.WeatherGrowFactor
                : WeatherClearGrowFactor;
            grow *= weather;
            if (context.Fertilized)
                grow *= FertilizerGrowFactor;
            if (grow <= 0f)
                grow = 1f;
            return grow;
        }

        public static PlantGrowthStage Resolve(SeedDetailData seed, int elapsedMinutes) =>
            Resolve(seed, elapsedMinutes, PlantGrowthContext.Default);

        public static PlantGrowthStage Resolve(
            SeedDetailData seed,
            int elapsedMinutes,
            in PlantGrowthContext context)
        {
            if (context.FrostKills)
                return PlantGrowthStage.Withered;

            float grow = EffectiveGrowMinutes(seed, in context);
            float elapsed = elapsedMinutes < 0 ? 0f : elapsedMinutes;
            if (elapsed >= grow + WitherSlackMinutes)
                return PlantGrowthStage.Withered;
            if (elapsed >= grow)
                return PlantGrowthStage.Harvestable;
            if (elapsed >= grow * MatureAtGrowFraction)
                return PlantGrowthStage.Mature;
            if (elapsed >= grow * SeedlingAtGrowFraction)
                return PlantGrowthStage.Seedling;
            return PlantGrowthStage.Seed;
        }

        public static PlantGrowthStage Resolve(ItemData item, int plantedWorldMinute, int currentWorldMinute) =>
            Resolve(item?.seed, ElapsedMinutes(plantedWorldMinute, currentWorldMinute), PlantGrowthContext.Default);

        public static PlantGrowthStage Resolve(
            ItemData item,
            int plantedWorldMinute,
            int currentWorldMinute,
            in PlantGrowthContext context) =>
            Resolve(item?.seed, ElapsedMinutes(plantedWorldMinute, currentWorldMinute), in context);

        public static bool IsHarvestable(PlantGrowthStage stage) =>
            stage == PlantGrowthStage.Harvestable;

        public static bool IsWithered(PlantGrowthStage stage) =>
            stage == PlantGrowthStage.Withered;
    }
}
