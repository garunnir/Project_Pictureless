// ============================================================
// GameDataQueries — GameItems/RefData 병합 조회 SSOT
// ============================================================

using System.Collections.Generic;

namespace Garunnir.Runtime.Gameplay.Data
{
    public static class GameDataQueries
    {
        public static GameDatabase GameItems => GameDataLoader.GameData;

        public static GameDatabase RefData => GameDataLoader.RefData;

        public static ItemData GetItem(string id)
        {
            return GameItems?.GetItem(id) ?? RefData?.GetItem(id);
        }

        public static ContainerData GetContainer(string id)
        {
            return GameItems?.GetContainer(id) ?? RefData?.GetContainer(id);
        }

        public static TerrainData GetTerrain(string id)
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
            List<RecipeData> list = GameItems?.GetRecipesForResult(resultId);
            if (list != null && list.Count > 0)
                return list;
            return RefData?.GetRecipesForResult(resultId) ?? EmptyRecipes;
        }

        public static List<RecipeData> GetRecipesUsingIngredient(string itemId)
        {
            List<RecipeData> list = GameItems?.GetRecipesUsingIngredient(itemId);
            if (list != null && list.Count > 0)
                return list;
            return RefData?.GetRecipesUsingIngredient(itemId) ?? EmptyRecipes;
        }

        public static List<RecipeData> GetUncraftForResult(string resultId)
        {
            List<RecipeData> list = GameItems?.GetUncraftForResult(resultId);
            if (list != null && list.Count > 0)
                return list;
            return RefData?.GetUncraftForResult(resultId) ?? EmptyRecipes;
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

        static readonly List<RecipeData> EmptyRecipes = new(0);
    }
}
