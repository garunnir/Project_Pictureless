// ============================================================
// CompositeCraftingSource — 여러 ICraftingSource를 하나로 합침
// ============================================================

using System.Collections.Generic;
using Garunnir.Runtime.Gameplay.Data;

public sealed class CompositeCraftingSource : ICraftingSource
{
    readonly List<ICraftingSource> _sources = new();

    public void Add(ICraftingSource source)
    {
        if (source != null)
            _sources.Add(source);
    }

    public IReadOnlyList<RecipeView> GetAllRecipes()
    {
        var merged = new List<RecipeView>();
        foreach (ICraftingSource src in _sources)
        {
            var recipes = src.GetAllRecipes();
            if (recipes != null)
                merged.AddRange(recipes);
        }
        return merged;
    }

    public IReadOnlyList<RecipeView> GetRecipesByCategory(string category)
    {
        var merged = new List<RecipeView>();
        foreach (ICraftingSource src in _sources)
        {
            var recipes = src.GetRecipesByCategory(category);
            if (recipes != null)
                merged.AddRange(recipes);
        }
        return merged;
    }

    public IReadOnlyList<RecipeView> FindRecipesUsingIngredient(string itemId)
    {
        var merged = new List<RecipeView>();
        foreach (ICraftingSource src in _sources)
        {
            var recipes = src.FindRecipesUsingIngredient(itemId);
            if (recipes != null)
                merged.AddRange(recipes);
        }
        return merged;
    }

    public RecipeView? GetRecipe(string recipeId)
    {
        foreach (ICraftingSource src in _sources)
        {
            RecipeView? view = src.GetRecipe(recipeId);
            if (view.HasValue)
                return view;
        }
        return null;
    }
}
