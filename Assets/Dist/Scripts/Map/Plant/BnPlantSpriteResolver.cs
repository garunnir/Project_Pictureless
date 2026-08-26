// ============================================================
// BnPlantSpriteResolver — BN plant_sprites.json → PlantGrowthStage Sprite (lazy)
// ============================================================

using System;
using System.Collections.Generic;
using System.IO;
using Garunnir.Runtime.Gameplay.Data;
using Newtonsoft.Json;
using UnityEngine;

namespace IsoTilemap
{
    public static class BnPlantSpriteResolver
    {
        public const string RefDataFolder = "BNData";
        public const string TilesetFolder = "tileset";
        public const string IndexFileName = "plant_sprites.json";
        public const int DefaultPixelsPerUnit = 100;

        public const string FurnitureSeed = "f_plant_seed";
        public const string FurnitureSeedling = "f_plant_seedling";
        public const string FurnitureMature = "f_plant_mature";
        public const string FurnitureHarvest = "f_plant_harvest";

        static readonly Vector2 Pivot = new Vector2(0.5f, 0f);

        static BnPlantIndexFile _index;
        static bool _loadAttempted;
        static readonly Dictionary<string, Texture2D> Atlases = new(StringComparer.Ordinal);
        static readonly Dictionary<string, Sprite> Sprites = new(StringComparer.Ordinal);

        public static bool TryGetStage(PlantGrowthStage stage, out Sprite sprite)
        {
            sprite = null;
            string key = StageKey(stage);
            if (key == null)
                return false;

            if (Sprites.TryGetValue(key, out sprite) && sprite != null)
                return true;

            BnPlantIndexFile index = LoadIndex();
            if (index?.stages == null ||
                !index.stages.TryGetValue(key, out BnPlantSpriteSpec spec) ||
                spec == null ||
                string.IsNullOrEmpty(spec.file))
                return false;

            sprite = CreateSprite(index, spec, key);
            return sprite != null;
        }

        public static void Invalidate()
        {
            foreach (KeyValuePair<string, Sprite> pair in Sprites)
                DestroyUnityObject(pair.Value);
            foreach (KeyValuePair<string, Texture2D> pair in Atlases)
                DestroyUnityObject(pair.Value);

            Sprites.Clear();
            Atlases.Clear();
            _index = null;
            _loadAttempted = false;
        }

        static string StageKey(PlantGrowthStage stage)
        {
            switch (stage)
            {
                case PlantGrowthStage.Seed:
                    return "Seed";
                case PlantGrowthStage.Seedling:
                    return "Seedling";
                case PlantGrowthStage.Mature:
                    return "Mature";
                case PlantGrowthStage.Harvestable:
                    return "Harvestable";
                default:
                    return null;
            }
        }

        static BnPlantIndexFile LoadIndex()
        {
            if (_loadAttempted)
                return _index;

            _loadAttempted = true;
            string path = Path.Combine(
                Application.streamingAssetsPath,
                RefDataFolder,
                TilesetFolder,
                IndexFileName);
            if (!File.Exists(path))
            {
                Debug.LogWarning($"[BnPlantSpriteResolver] Missing '{path}' — plant overlay uses Catalog/primitive.");
                return null;
            }

            try
            {
                string json = File.ReadAllText(path);
                _index = JsonConvert.DeserializeObject<BnPlantIndexFile>(json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BnPlantSpriteResolver] Failed to load '{path}': {ex.Message}");
                _index = null;
            }

            return _index;
        }

        static Sprite CreateSprite(BnPlantIndexFile index, BnPlantSpriteSpec spec, string cacheKey)
        {
            Texture2D atlas = LoadAtlas(spec.file);
            if (atlas == null)
                return null;

            if (index.files == null ||
                !index.files.TryGetValue(spec.file, out BnPlantFileSpec fileSpec) ||
                fileSpec == null)
                return null;

            int spriteW = fileSpec.sprite_width;
            int spriteH = fileSpec.sprite_height;
            if (spriteW <= 0 || spriteH <= 0)
                return null;

            int cols = atlas.width / spriteW;
            int rows = atlas.height / spriteH;
            if (cols <= 0 || rows <= 0)
                return null;

            int local = spec.index;
            if (local < 0 || local >= cols * rows)
                return null;

            int col = local % cols;
            int row = local / cols;
            int srcX = col * spriteW;
            int srcY = row * spriteH;
            int unityY = atlas.height - srcY - spriteH;
            if (srcX < 0 || unityY < 0 || srcX + spriteW > atlas.width || unityY + spriteH > atlas.height)
                return null;

            int ppu = index.ppu > 0 ? index.ppu : DefaultPixelsPerUnit;
            var rect = new Rect(srcX, unityY, spriteW, spriteH);
            Sprite sprite = Sprite.Create(atlas, rect, Pivot, ppu, 0, SpriteMeshType.FullRect);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            Sprites[cacheKey] = sprite;
            return sprite;
        }

        static Texture2D LoadAtlas(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return null;

            if (Atlases.TryGetValue(fileName, out Texture2D cached) && cached != null)
                return cached;

            string path = Path.Combine(
                Application.streamingAssetsPath,
                RefDataFolder,
                TilesetFolder,
                fileName);
            if (!File.Exists(path))
            {
                Debug.LogError($"[BnPlantSpriteResolver] Atlas missing: {path}");
                return null;
            }

            byte[] bytes = File.ReadAllBytes(path);
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
                name = fileName
            };
            if (!tex.LoadImage(bytes, false))
            {
                DestroyUnityObject(tex);
                Debug.LogError($"[BnPlantSpriteResolver] LoadImage failed: {path}");
                return null;
            }

            Atlases[fileName] = tex;
            return tex;
        }

        static void DestroyUnityObject(UnityEngine.Object obj)
        {
            if (obj == null)
                return;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                UnityEngine.Object.DestroyImmediate(obj);
                return;
            }
#endif
            UnityEngine.Object.Destroy(obj);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Invalidate();

        sealed class BnPlantIndexFile
        {
            public int ppu = DefaultPixelsPerUnit;
            public Dictionary<string, BnPlantFileSpec> files;
            public Dictionary<string, BnPlantSpriteSpec> stages;
        }

        sealed class BnPlantFileSpec
        {
            public int sprite_width;
            public int sprite_height;
        }

        sealed class BnPlantSpriteSpec
        {
            public string file;
            public int index;
        }
    }
}
