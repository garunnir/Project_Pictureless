// ============================================================
// TileFlags — TileDefinition gameplay string-flag SSOT (BN-style)
// ============================================================
using System;
using System.Collections.Generic;

namespace IsoTilemap
{
    public static class TileFlags
    {
        public const string Plantable = "PLANTABLE";
        public const string Plowable = "PLOWABLE";
        public const string Diggable = "DIGGABLE";
        public const string Plant = "PLANT";
        public const string GrowthSeed = "GROWTH_SEED";
        public const string GrowthSeedling = "GROWTH_SEEDLING";
        public const string GrowthMature = "GROWTH_MATURE";
        public const string GrowthHarvest = "GROWTH_HARVEST";
        public const string GrowthWithered = "GROWTH_WITHERED";

        public static bool HasFlag(TileDefinition definition, string flag)
        {
            if (definition == null || definition.flags == null || string.IsNullOrEmpty(flag))
                return false;

            List<string> flags = definition.flags;
            for (int i = 0; i < flags.Count; i++)
            {
                string value = flags[i];
                if (!string.IsNullOrEmpty(value) &&
                    value.Equals(flag, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
    }
}
