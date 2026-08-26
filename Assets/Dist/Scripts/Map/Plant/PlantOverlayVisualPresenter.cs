// ============================================================
// PlantOverlayVisualPresenter — 맵 작물 스프라이트 진입점 (Catalog → BN → null)
// ============================================================

using Garunnir.Runtime.Gameplay.Data;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace IsoTilemap
{
    public static class PlantOverlayVisualPresenter
    {
        static PlantOverlaySpriteCatalog _catalog;

        public static PlantOverlaySpriteCatalog Catalog
        {
            get
            {
                if (_catalog == null)
                    _catalog = LoadCatalog();
                return _catalog;
            }
        }

        /// <summary>
        /// Catalog override → BN plant_sprites → null (caller keeps primitive fallback).
        /// Withered: Catalog only, else Harvestable BN sprite (tint applied by visual).
        /// </summary>
        public static Sprite GetStageSprite(PlantGrowthStage stage, string seedItemId = null)
        {
            PlantOverlaySpriteCatalog catalog = Catalog;
            if (catalog != null)
            {
                Sprite assigned = catalog.GetAssigned(stage, seedItemId);
                if (assigned != null)
                    return assigned;
            }

            if (stage == PlantGrowthStage.Withered)
            {
                if (BnPlantSpriteResolver.TryGetStage(PlantGrowthStage.Harvestable, out Sprite harvest))
                    return harvest;
                return null;
            }

            return BnPlantSpriteResolver.TryGetStage(stage, out Sprite bn) ? bn : null;
        }

        public static void BindCatalog(PlantOverlaySpriteCatalog catalog)
        {
            _catalog = catalog;
            catalog?.RebuildCache();
        }

        public static void InvalidateCache()
        {
            _catalog = null;
            BnPlantSpriteResolver.Invalidate();
        }

        static PlantOverlaySpriteCatalog LoadCatalog()
        {
            PlantOverlaySpriteCatalog fromResources =
                Resources.Load<PlantOverlaySpriteCatalog>(PlantOverlaySpriteCatalog.ResourcesLoadName);
            if (fromResources != null)
                return fromResources;

#if UNITY_EDITOR
            return AssetDatabase.LoadAssetAtPath<PlantOverlaySpriteCatalog>(
                PlantOverlaySpriteCatalog.AssetPath);
#else
            return null;
#endif
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => InvalidateCache();
    }
}
