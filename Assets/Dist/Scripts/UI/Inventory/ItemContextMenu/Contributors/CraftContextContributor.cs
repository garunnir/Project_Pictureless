// ============================================================
// CraftContextContributor — 제작 서브트리 (category → subcategory → 리프)
// ============================================================

using System;
using System.Collections.Generic;
using Garunnir.Runtime.Gameplay.Data;

public sealed class CraftContextContributor : IContextMenuContributor
{
    public void Contribute(
        ItemStack stack,
        InventoryContainer container,
        InventorySession session,
        List<ContextMenuEntry> roots)
    {
        if (stack?.Item == null || roots == null)
            return;

        List<RecipeData> recipes = GameplayData.GetRecipesUsingIngredient(stack.ItemId);
        if (recipes == null || recipes.Count == 0)
            return;

        List<ContextMenuEntry> categoryChildren = BuildCategoryTree(recipes, container, session);
        if (categoryChildren.Count == 0)
            return;

        roots.Add(ContextMenuEntry.Group("craft", ItemContextMenuLabels.Craft, categoryChildren));
    }

    static List<ContextMenuEntry> BuildCategoryTree(
        List<RecipeData> recipes,
        InventoryContainer container,
        InventorySession session)
    {
        // category → subcategory → recipe id → entry
        var byCategory = new SortedDictionary<string, SortedDictionary<string, SortedDictionary<string, ContextMenuEntry>>>(
            StringComparer.Ordinal);

        for (int i = 0; i < recipes.Count; i++)
        {
            RecipeData recipe = recipes[i];
            if (recipe == null || string.IsNullOrEmpty(recipe.result) || string.IsNullOrEmpty(recipe.id))
                continue;

            string catKey = string.IsNullOrEmpty(recipe.category) ? "" : recipe.category;
            string subKey = string.IsNullOrEmpty(recipe.subcategory) ? "" : recipe.subcategory;

            if (!byCategory.TryGetValue(catKey, out SortedDictionary<string, SortedDictionary<string, ContextMenuEntry>> bySub))
            {
                bySub = new SortedDictionary<string, SortedDictionary<string, ContextMenuEntry>>(StringComparer.Ordinal);
                byCategory[catKey] = bySub;
            }

            if (!bySub.TryGetValue(subKey, out SortedDictionary<string, ContextMenuEntry> byRecipe))
            {
                byRecipe = new SortedDictionary<string, ContextMenuEntry>(StringComparer.Ordinal);
                bySub[subKey] = byRecipe;
            }

            if (byRecipe.ContainsKey(recipe.id))
                continue;

            byRecipe[recipe.id] = ContextMenuEntry.Leaf(
                $"craft:{recipe.id}",
                RecipeContextMenuText.FormatResultLabel(recipe),
                new CraftContextAction(recipe, container, session));
        }

        var categoryEntries = new List<ContextMenuEntry>();
        foreach (KeyValuePair<string, SortedDictionary<string, SortedDictionary<string, ContextMenuEntry>>> catPair in byCategory)
        {
            var subEntries = new List<ContextMenuEntry>();
            foreach (KeyValuePair<string, SortedDictionary<string, ContextMenuEntry>> subPair in catPair.Value)
            {
                var leaves = new List<ContextMenuEntry>(subPair.Value.Values);
                leaves.Sort((a, b) => string.CompareOrdinal(a.Label, b.Label));
                subEntries.Add(ContextMenuEntry.Group(
                    $"craft-sub:{catPair.Key}:{subPair.Key}",
                    RecipeCategoryLabels.Get(subPair.Key),
                    leaves));
            }

            subEntries.Sort((a, b) => string.CompareOrdinal(a.Label, b.Label));
            categoryEntries.Add(ContextMenuEntry.Group(
                $"craft-cat:{catPair.Key}",
                RecipeCategoryLabels.Get(catPair.Key),
                subEntries));
        }

        categoryEntries.Sort((a, b) => string.CompareOrdinal(a.Label, b.Label));
        return categoryEntries;
    }
}
