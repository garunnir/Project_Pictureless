// ============================================================
// GameplayData — possessed runtime + GameData 조회 파사드
// ============================================================

using System.Collections.Generic;
using Garunnir.Runtime.Gameplay.Data;

public static class GameplayData
{
    public static IPlayerStats Stats
    {
        get => GameplayPlayerRuntime.Stats;
        set => GameplayPlayerRuntime.Stats = value;
    }

    public static ICharacterSkills CharacterSkills => GameplayPlayerRuntime.CharacterSkills;

    public static ICharacterBody Body
    {
        get => GameplayPlayerRuntime.Body;
        set => GameplayPlayerRuntime.Body = value;
    }

    public static IPlayerVitals Vitals
    {
        get => GameplayPlayerRuntime.Vitals;
        set => GameplayPlayerRuntime.Vitals = value;
    }

    public static ICharacterDefeat Defeat
    {
        get => GameplayPlayerRuntime.Defeat;
        set => GameplayPlayerRuntime.Defeat = value;
    }

    public static ICharacterProficiencies Proficiencies
    {
        get => GameplayPlayerRuntime.Proficiencies;
        set => GameplayPlayerRuntime.Proficiencies = value;
    }

    public static ICharacterRecipeMemory RecipeMemory
    {
        get => GameplayPlayerRuntime.RecipeMemory;
        set => GameplayPlayerRuntime.RecipeMemory = value;
    }

    public static ICharacterTraits Traits
    {
        get => GameplayPlayerRuntime.Traits;
        set => GameplayPlayerRuntime.Traits = value;
    }

    public static GameDatabase GameItems => GameDataQueries.GameItems;

    public static GameDatabase RefData => GameDataQueries.RefData;

    public static ItemData GetItem(string id) => GameDataQueries.GetItem(id);

    public static ContainerData GetContainer(string id) => GameDataQueries.GetContainer(id);

    public static TerrainData GetTerrain(string id) => GameDataQueries.GetTerrain(id);

    public static FurnitureData GetFurniture(string id) => GameDataQueries.GetFurniture(id);

    public static MaterialData GetMaterial(string id) => GameDataQueries.GetMaterial(id);

    public static List<RecipeData> GetRecipesForResult(string resultId) =>
        GameDataQueries.GetRecipesForResult(resultId);

    public static List<RecipeData> GetRecipesUsingIngredient(string itemId) =>
        GameDataQueries.GetRecipesUsingIngredient(itemId);

    public static List<RecipeData> GetUncraftForResult(string resultId) =>
        GameDataQueries.GetUncraftForResult(resultId);

    public static List<RecipeData> GetAllRecipes() => GameDataQueries.GetAllRecipes();

    public static List<string> GetRecipeCategories() => GameDataQueries.GetRecipeCategories();

    public static List<RecipeData> GetRecipesByCategory(string category) =>
        GameDataQueries.GetRecipesByCategory(category);

    public static void ClearCache() => GameDataLoader.Unload();
}
