// ============================================================
// GameplayData ? ????? ??? SSOT (??? ?? ? ?? fallback)
// ============================================================

using System.Collections.Generic;
using Garunnir.Runtime.Gameplay.Data;
using UnityEngine;

public static class GameplayData
{
    static IPlayerStats _stats;
    static ICharacterBody _body;
    static IPlayerVitals _vitals;
    static DefaultCharacterDefeat _defeat;

    /// <summary>
    /// ???? ??/?? ?? ?? (???? ??; NPC? ?? ICharacterSkills).
    /// </summary>
    public static IPlayerStats Stats
    {
        get
        {
            if (_stats == null)
                _stats = new DefaultPlayerStats();
            return _stats;
        }
        set
        {
            _stats = value;
            InvalidateDefeat();
        }
    }

    /// <summary>?? ?? API. Stats? DefaultPlayerStats? ?? ??.</summary>
    public static ICharacterSkills CharacterSkills =>
        Stats is DefaultPlayerStats dps ? dps.Skills : null;

    /// <summary>
    /// ?? ??? ?? SSOT.
    /// </summary>
    public static ICharacterBody Body
    {
        get
        {
            if (_body == null)
            {
                _body = CharacterBody.CreateHumanDefault(Stats.GetStat(AttributeIds.Str));
                InvalidateDefeat();
            }

            return _body;
        }
        set
        {
            _body = value;
            InvalidateDefeat();
        }
    }

    /// <summary>
    /// ?? ???(??/??/????) SSOT.
    /// </summary>
    public static IPlayerVitals Vitals
    {
        get
        {
            if (_vitals == null)
                _vitals = new DefaultPlayerVitals();
            return _vitals;
        }
        set => _vitals = value;
    }

    /// <summary>
    /// ???? ?? ??/?? ?? (Body ? Skills).
    /// </summary>
    public static ICharacterDefeat Defeat
    {
        get
        {
            if (_defeat == null)
                _defeat = new DefaultCharacterDefeat(Body, CharacterSkills);
            return _defeat;
        }
        set
        {
            InvalidateDefeat();
            if (value is DefaultCharacterDefeat concrete)
                _defeat = concrete;
            else if (value != null)
                Debug.LogWarning("[GameplayData] Defeat setter expects DefaultCharacterDefeat; ignored.");
        }
    }

    /// <summary>???? ??? ??? (?? ??)</summary>
    public static GameDatabase GameItems => GameDataLoader.GameData;

    /// <summary>?? ??? (CC BY-SA 3.0, ?? ??)</summary>
    public static GameDatabase RefData => GameDataLoader.RefData;

    public static ItemData GetItem(string id)
    {
        return GameItems?.GetItem(id) ?? RefData?.GetItem(id);
    }

    public static ContainerData GetContainer(string id)
    {
        return GameItems?.GetContainer(id) ?? RefData?.GetContainer(id);
    }

    public static Garunnir.Runtime.Gameplay.Data.TerrainData GetTerrain(string id)
    {
        return GameItems?.GetTerrain(id) ?? RefData?.GetTerrain(id);
    }

    public static FurnitureData GetFurniture(string id)
    {
        return GameItems?.GetFurniture(id) ?? RefData?.GetFurniture(id);
    }

    public static MaterialData GetMaterial(string id)
    {
        return GameItems?.GetMaterial(id) ?? RefData?.GetMaterial(id);
    }

    public static List<RecipeData> GetRecipesForResult(string resultId)
    {
        var list = GameItems?.GetRecipesForResult(resultId);
        if (list != null && list.Count > 0) return list;
        return RefData?.GetRecipesForResult(resultId) ?? _emptyRecipes;
    }

    public static List<RecipeData> GetRecipesUsingIngredient(string itemId)
    {
        var list = GameItems?.GetRecipesUsingIngredient(itemId);
        if (list != null && list.Count > 0) return list;
        return RefData?.GetRecipesUsingIngredient(itemId) ?? _emptyRecipes;
    }

    public static List<RecipeData> GetUncraftForResult(string resultId)
    {
        var list = GameItems?.GetUncraftForResult(resultId);
        if (list != null && list.Count > 0) return list;
        return RefData?.GetUncraftForResult(resultId) ?? _emptyRecipes;
    }

    public static List<RecipeData> GetAllRecipes()
    {
        var merged = new List<RecipeData>();
        var customIds = new HashSet<string>();

        AppendRecipes(GameItems?.Recipes, merged, customIds, skipIfCustomId: false);
        AppendRecipes(RefData?.Recipes, merged, customIds, skipIfCustomId: true);
        return merged;
    }

    public static List<string> GetRecipeCategories()
    {
        List<RecipeData> recipes = GetAllRecipes();
        var categories = new List<string>();
        var seen = new HashSet<string>();

        for (int i = 0; i < recipes.Count; i++)
        {
            string category = recipes[i]?.category;
            if (string.IsNullOrEmpty(category) || !seen.Add(category))
                continue;
            categories.Add(category);
        }

        return categories;
    }

    public static List<RecipeData> GetRecipesByCategory(string category)
    {
        List<RecipeData> all = GetAllRecipes();
        if (string.IsNullOrEmpty(category))
            return all;

        var filtered = new List<RecipeData>();
        for (int i = 0; i < all.Count; i++)
        {
            RecipeData recipe = all[i];
            if (recipe == null || recipe.category != category)
                continue;
            filtered.Add(recipe);
        }

        return filtered;
    }

    public static void ClearCache()
    {
        GameDataLoader.Unload();
    }

    static void AppendRecipes(
        IReadOnlyList<RecipeData> recipes,
        List<RecipeData> dest,
        HashSet<string> customIds,
        bool skipIfCustomId)
    {
        if (recipes == null)
            return;

        for (int i = 0; i < recipes.Count; i++)
        {
            RecipeData recipe = recipes[i];
            if (recipe == null)
                continue;

            if (!string.IsNullOrEmpty(recipe.id))
            {
                if (skipIfCustomId && customIds.Contains(recipe.id))
                    continue;
                customIds.Add(recipe.id);
            }

            dest.Add(recipe);
        }
    }

    static void InvalidateDefeat()
    {
        _defeat?.Dispose();
        _defeat = null;
    }

    static readonly List<RecipeData> _emptyRecipes = new(0);
}
