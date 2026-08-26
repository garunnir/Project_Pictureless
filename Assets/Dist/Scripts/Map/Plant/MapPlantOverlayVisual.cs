// ============================================================
// MapPlantOverlayVisual — plant overlay look by PlantGrowthStage (sprite or primitive)
// ============================================================

using UnityEngine;
using UnityEngine.Rendering;
using PlantGrowth = global::Garunnir.Runtime.Gameplay.Data.PlantGrowth;
using PlantGrowthContext = global::Garunnir.Runtime.Gameplay.Data.PlantGrowthContext;
using PlantGrowthStage = global::Garunnir.Runtime.Gameplay.Data.PlantGrowthStage;
using ItemData = global::Garunnir.Runtime.Gameplay.Data.ItemData;

namespace IsoTilemap
{
    public static class MapPlantOverlayVisual
    {
        static Mesh _cubeMesh;
        static Mesh _sphereMesh;
        static Material[] _materials;

        public static PlantGrowthStage ResolveStage(in PlantCell plant)
        {
            ItemData item = GameplayData.GetItem(plant.SeedItemId);
            int elapsed = PlantGrowth.ElapsedMinutes(
                plant.PlantedWorldMinute,
                MapClockSnapshot.CurrentWorldMinute());
            var context = new PlantGrowthContext(
                plant.Fertilized,
                PlantGrowth.WeatherClearGrowFactor,
                frostKills: false);
            return PlantGrowth.Resolve(item?.seed, elapsed, in context);
        }

        public static void Apply(
            Transform transform,
            MeshFilter filter,
            MeshRenderer meshRenderer,
            SpriteRenderer spriteRenderer,
            Vector3Int cell,
            float cellSize,
            PlantGrowthStage stage,
            string seedItemId = null)
        {
            EnsureAssets();
            Sprite sprite = PlantOverlayVisualPresenter.GetStageSprite(stage, seedItemId);
            bool useSprite = sprite != null && spriteRenderer != null;

            if (useSprite)
                ApplySprite(transform, filter, meshRenderer, spriteRenderer, cell, cellSize, stage, sprite);
            else
                ApplyPrimitive(transform, filter, meshRenderer, spriteRenderer, cell, cellSize, stage);
        }

        /// <summary>Legacy call sites without SpriteRenderer — primitive path only.</summary>
        public static void Apply(
            Transform transform,
            MeshFilter filter,
            MeshRenderer renderer,
            Vector3Int cell,
            float cellSize,
            PlantGrowthStage stage)
        {
            Apply(transform, filter, renderer, null, cell, cellSize, stage, null);
        }

        static void ApplySprite(
            Transform transform,
            MeshFilter filter,
            MeshRenderer meshRenderer,
            SpriteRenderer spriteRenderer,
            Vector3Int cell,
            float cellSize,
            PlantGrowthStage stage,
            Sprite sprite)
        {
            float scale = SpriteScaleFor(stage);
            if (transform != null)
            {
                transform.localScale = new Vector3(scale, scale, scale);
                Vector3 pos = TileHelper.ConvertGridToWorldPos(cell, cellSize);
                pos.y += MapPlantConsts.OverlayYOffset;
                transform.position = pos;
            }

            if (filter != null)
                filter.sharedMesh = null;

            if (meshRenderer != null)
                meshRenderer.enabled = false;

            spriteRenderer.enabled = true;
            spriteRenderer.sprite = sprite;
            spriteRenderer.shadowCastingMode = ShadowCastingMode.Off;
            spriteRenderer.receiveShadows = false;
            spriteRenderer.color = stage == PlantGrowthStage.Withered
                ? MapPlantConsts.OverlayColorWithered
                : Color.white;
        }

        static void ApplyPrimitive(
            Transform transform,
            MeshFilter filter,
            MeshRenderer meshRenderer,
            SpriteRenderer spriteRenderer,
            Vector3Int cell,
            float cellSize,
            PlantGrowthStage stage)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = null;
                spriteRenderer.enabled = false;
            }

            Vector3 scale = ScaleFor(stage);
            if (transform != null)
            {
                transform.localScale = scale;
                Vector3 pos = TileHelper.ConvertGridToWorldPos(cell, cellSize);
                pos.y += MapPlantConsts.OverlayYOffset + scale.y * 0.5f;
                transform.position = pos;
            }

            if (filter != null)
                filter.sharedMesh = MeshFor(stage);

            if (meshRenderer != null)
            {
                meshRenderer.enabled = true;
                meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
                meshRenderer.receiveShadows = false;
                int index = StageIndex(stage);
                if (_materials != null && index >= 0 && index < _materials.Length)
                    meshRenderer.sharedMaterial = _materials[index];
            }
        }

        static float SpriteScaleFor(PlantGrowthStage stage)
        {
            switch (stage)
            {
                case PlantGrowthStage.Seedling:
                    return MapPlantConsts.SpriteWorldScaleSeedling;
                case PlantGrowthStage.Mature:
                    return MapPlantConsts.SpriteWorldScaleMature;
                case PlantGrowthStage.Harvestable:
                    return MapPlantConsts.SpriteWorldScaleHarvestable;
                case PlantGrowthStage.Withered:
                    return MapPlantConsts.SpriteWorldScaleWithered;
                default:
                    return MapPlantConsts.SpriteWorldScaleSeed;
            }
        }

        static Vector3 ScaleFor(PlantGrowthStage stage)
        {
            switch (stage)
            {
                case PlantGrowthStage.Seedling:
                    return MapPlantConsts.OverlayScaleSeedling;
                case PlantGrowthStage.Mature:
                    return MapPlantConsts.OverlayScaleMature;
                case PlantGrowthStage.Harvestable:
                    return MapPlantConsts.OverlayScaleHarvestable;
                case PlantGrowthStage.Withered:
                    return MapPlantConsts.OverlayScaleWithered;
                default:
                    return MapPlantConsts.OverlayScaleSeed;
            }
        }

        static Mesh MeshFor(PlantGrowthStage stage)
        {
            if (stage == PlantGrowthStage.Seed)
                return _sphereMesh;
            return _cubeMesh;
        }

        static int StageIndex(PlantGrowthStage stage)
        {
            int index = (int)stage;
            if (index < 0)
                return 0;
            if (index > (int)PlantGrowthStage.Withered)
                return (int)PlantGrowthStage.Withered;
            return index;
        }

        static void EnsureAssets()
        {
            if (_cubeMesh == null)
                _cubeMesh = BorrowPrimitiveMesh(PrimitiveType.Cube);
            if (_sphereMesh == null)
                _sphereMesh = BorrowPrimitiveMesh(PrimitiveType.Sphere);
            if (_materials != null)
                return;

            _materials = new[]
            {
                MakeMaterial(MapPlantConsts.OverlayColorSeed, "MapPlantOverlay_Seed"),
                MakeMaterial(MapPlantConsts.OverlayColorSeedling, "MapPlantOverlay_Seedling"),
                MakeMaterial(MapPlantConsts.OverlayColorMature, "MapPlantOverlay_Mature"),
                MakeMaterial(MapPlantConsts.OverlayColorHarvestable, "MapPlantOverlay_Harvestable"),
                MakeMaterial(MapPlantConsts.OverlayColorWithered, "MapPlantOverlay_Withered"),
            };
        }

        static Mesh BorrowPrimitiveMesh(PrimitiveType type)
        {
            GameObject temp = GameObject.CreatePrimitive(type);
            Mesh mesh = null;
            if (temp.TryGetComponent(out MeshFilter filter))
                mesh = filter.sharedMesh;
            Object.Destroy(temp);
            return mesh;
        }

        static Material MakeMaterial(Color color, string name)
        {
            Shader shader = Shader.Find(MapPlantConsts.OverlayShaderUrpUnlit);
            if (shader == null)
                shader = Shader.Find(MapPlantConsts.OverlayShaderUnlitColor);
            if (shader == null)
                return null;

            var mat = new Material(shader) { name = name };
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            else if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", color);
            return mat;
        }
    }
}
