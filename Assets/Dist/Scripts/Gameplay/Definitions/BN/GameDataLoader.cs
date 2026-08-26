// ============================================================
// GameDataLoader — RefData(참조) + GameData(커스텀) 듀얼 JSON 로더
// ============================================================

using System.IO;
using UnityEngine;

namespace Garunnir.Runtime.Gameplay.Data
{
    public static class GameDataLoader
    {
        const string REF_FOLDER = "BNData";
        const string GAME_FOLDER = "GameData";
        const string ITEMS_FILE = "items.json";
        const string RECIPES_FILE = "recipes.json";
        const string TERRAIN_FURNITURE_FILE = "terrain_furniture.json";
        const string PROFICIENCIES_FILE = "proficiencies.json";

        static GameDatabase _refDatabase;
        static GameDatabase _gameDatabase;

        /// <summary>참조 데이터 (CC BY-SA 3.0, 읽기 전용)</summary>
        public static GameDatabase RefData
        {
            get
            {
                if (_refDatabase == null) Load();
                return _refDatabase;
            }
        }

        /// <summary>프로젝트 커스텀 데이터 (편집 가능)</summary>
        public static GameDatabase GameData
        {
            get
            {
                if (_gameDatabase == null) Load();
                return _gameDatabase;
            }
        }

        public static bool IsLoaded => _refDatabase != null;

        public static void Load()
        {
            _refDatabase = LoadFromFolder(REF_FOLDER, "Ref");
            _gameDatabase = LoadFromFolder(GAME_FOLDER, "Game");
        }

        static GameDatabase LoadFromFolder(string folder, string tag)
        {
            string basePath = Path.Combine(Application.streamingAssetsPath, folder);
            string itemsPath = Path.Combine(basePath, ITEMS_FILE);
            string recipesPath = Path.Combine(basePath, RECIPES_FILE);
            string terrainFurniturePath = Path.Combine(basePath, TERRAIN_FURNITURE_FILE);
            string proficienciesPath = Path.Combine(basePath, PROFICIENCIES_FILE);

            ItemsFileRoot itemsRoot = null;
            RecipesFileRoot recipesRoot = null;
            TerrainFurnitureFileRoot terrainFurnitureRoot = null;
            ProficienciesFileRoot proficienciesRoot = null;

            if (File.Exists(itemsPath))
            {
                string json = File.ReadAllText(itemsPath);
                itemsRoot = GameDataJson.Deserialize<ItemsFileRoot>(json);
                Debug.Log($"[GameDataLoader:{tag}] items: {itemsRoot?.items?.Count ?? 0}");
            }

            if (File.Exists(recipesPath))
            {
                string json = File.ReadAllText(recipesPath);
                recipesRoot = GameDataJson.Deserialize<RecipesFileRoot>(json);
                Debug.Log($"[GameDataLoader:{tag}] recipes: {recipesRoot?.recipes?.Count ?? 0}");
            }

            if (File.Exists(terrainFurniturePath))
            {
                string json = File.ReadAllText(terrainFurniturePath);
                terrainFurnitureRoot = GameDataJson.Deserialize<TerrainFurnitureFileRoot>(json);
                Debug.Log(
                    $"[GameDataLoader:{tag}] terrain: {terrainFurnitureRoot?.terrain?.Count ?? 0} " +
                    $"furniture: {terrainFurnitureRoot?.furniture?.Count ?? 0}");
            }

            if (File.Exists(proficienciesPath))
            {
                string json = File.ReadAllText(proficienciesPath);
                proficienciesRoot = GameDataJson.Deserialize<ProficienciesFileRoot>(json);
                Debug.Log(
                    $"[GameDataLoader:{tag}] proficiencies: {proficienciesRoot?.proficiencies?.Count ?? 0}");
            }

            return new GameDatabase(itemsRoot, recipesRoot, terrainFurnitureRoot, proficienciesRoot);
        }

        public static void Unload()
        {
            _refDatabase = null;
            _gameDatabase = null;
            ItemNameTable.Unload();
        }

        public static void ReloadGameData()
        {
            _gameDatabase = LoadFromFolder(GAME_FOLDER, "Game");
        }

#if UNITY_EDITOR
        public static GameDatabase LoadFromPaths(
            string itemsJsonPath,
            string recipesJsonPath,
            string terrainFurnitureJsonPath = null)
        {
            ItemsFileRoot itemsRoot = null;
            RecipesFileRoot recipesRoot = null;
            TerrainFurnitureFileRoot terrainFurnitureRoot = null;

            if (!string.IsNullOrEmpty(itemsJsonPath) && File.Exists(itemsJsonPath))
            {
                string json = File.ReadAllText(itemsJsonPath);
                itemsRoot = GameDataJson.Deserialize<ItemsFileRoot>(json);
            }

            if (!string.IsNullOrEmpty(recipesJsonPath) && File.Exists(recipesJsonPath))
            {
                string json = File.ReadAllText(recipesJsonPath);
                recipesRoot = GameDataJson.Deserialize<RecipesFileRoot>(json);
            }

            if (!string.IsNullOrEmpty(terrainFurnitureJsonPath) && File.Exists(terrainFurnitureJsonPath))
            {
                string json = File.ReadAllText(terrainFurnitureJsonPath);
                terrainFurnitureRoot = GameDataJson.Deserialize<TerrainFurnitureFileRoot>(json);
            }

            return new GameDatabase(itemsRoot, recipesRoot, terrainFurnitureRoot);
        }

        public static string GetRefDataPath() =>
            Path.Combine(Application.streamingAssetsPath, REF_FOLDER);

        public static string GetGameDataPath() =>
            Path.Combine(Application.streamingAssetsPath, GAME_FOLDER);
#endif
    }
}
