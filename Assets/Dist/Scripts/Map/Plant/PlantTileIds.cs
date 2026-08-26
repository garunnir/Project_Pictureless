// ============================================================
// PlantTileIds — OccupiedCell plant + tilled floor prefabId SSOT
// ============================================================

namespace IsoTilemap
{
    public static class PlantTileIds
    {
        public const string FloorTilled = "Floor/Tilled";

        public const string PlantSeed = "Furniture/Plant_Seed";
        public const string PlantSeedling = "Furniture/Plant_Seedling";
        public const string PlantMature = "Furniture/Plant_Mature";
        public const string PlantHarvestable = "Furniture/Plant_Harvestable";
        public const string PlantWithered = "Furniture/Plant_Withered";

        public static string PrefabIdForStage(Garunnir.Runtime.Gameplay.Data.PlantGrowthStage stage)
        {
            switch (stage)
            {
                case Garunnir.Runtime.Gameplay.Data.PlantGrowthStage.Seedling:
                    return PlantSeedling;
                case Garunnir.Runtime.Gameplay.Data.PlantGrowthStage.Mature:
                    return PlantMature;
                case Garunnir.Runtime.Gameplay.Data.PlantGrowthStage.Harvestable:
                    return PlantHarvestable;
                case Garunnir.Runtime.Gameplay.Data.PlantGrowthStage.Withered:
                    return PlantWithered;
                default:
                    return PlantSeed;
            }
        }

        public static bool TryParseStage(
            string prefabId,
            out Garunnir.Runtime.Gameplay.Data.PlantGrowthStage stage)
        {
            stage = Garunnir.Runtime.Gameplay.Data.PlantGrowthStage.Seed;
            if (string.IsNullOrEmpty(prefabId))
                return false;
            if (prefabId.Equals(PlantSeed, System.StringComparison.Ordinal))
            {
                stage = Garunnir.Runtime.Gameplay.Data.PlantGrowthStage.Seed;
                return true;
            }
            if (prefabId.Equals(PlantSeedling, System.StringComparison.Ordinal))
            {
                stage = Garunnir.Runtime.Gameplay.Data.PlantGrowthStage.Seedling;
                return true;
            }
            if (prefabId.Equals(PlantMature, System.StringComparison.Ordinal))
            {
                stage = Garunnir.Runtime.Gameplay.Data.PlantGrowthStage.Mature;
                return true;
            }
            if (prefabId.Equals(PlantHarvestable, System.StringComparison.Ordinal))
            {
                stage = Garunnir.Runtime.Gameplay.Data.PlantGrowthStage.Harvestable;
                return true;
            }
            if (prefabId.Equals(PlantWithered, System.StringComparison.Ordinal))
            {
                stage = Garunnir.Runtime.Gameplay.Data.PlantGrowthStage.Withered;
                return true;
            }
            return false;
        }

        public static bool IsPlantPrefabId(string prefabId) =>
            TryParseStage(prefabId, out _);

        public static bool IsTilledFloorPrefabId(string prefabId) =>
            !string.IsNullOrEmpty(prefabId) &&
            prefabId.Equals(FloorTilled, System.StringComparison.Ordinal);
    }
}
