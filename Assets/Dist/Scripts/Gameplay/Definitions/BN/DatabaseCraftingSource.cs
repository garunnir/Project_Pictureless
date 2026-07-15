// ============================================================
// DatabaseCraftingSource — GameDatabase를 ICraftingSource로 래핑
// ============================================================

using System.Collections.Generic;

namespace Garunnir.Runtime.Gameplay.Data
{
    public sealed class DatabaseCraftingSource : ICraftingSource
    {
        readonly GameDatabase _db;
        List<RecipeView> _allCache;
        readonly Dictionary<string, List<RecipeView>> _categoryCache = new();
        readonly Dictionary<string, RecipeView> _byId = new();

        public DatabaseCraftingSource(GameDatabase db) => _db = db;

        public IReadOnlyList<RecipeView> GetAllRecipes()
        {
            if (_allCache != null) return _allCache;
            _allCache = new List<RecipeView>(_db.Recipes.Count);
            foreach (RecipeData r in _db.Recipes)
                _allCache.Add(ToView(r));
            return _allCache;
        }

        public IReadOnlyList<RecipeView> GetRecipesByCategory(string category)
        {
            if (string.IsNullOrEmpty(category)) return GetAllRecipes();
            if (_categoryCache.TryGetValue(category, out var cached)) return cached;

            var raw = _db.GetRecipesByCategory(category);
            var list = new List<RecipeView>(raw.Count);
            foreach (RecipeData r in raw)
                list.Add(ToView(r));
            _categoryCache[category] = list;
            return list;
        }

        public IReadOnlyList<RecipeView> FindRecipesUsingIngredient(string itemId)
        {
            var raw = _db.GetRecipesUsingIngredient(itemId);
            var list = new List<RecipeView>(raw.Count);
            foreach (RecipeData r in raw)
                list.Add(ToView(r));
            return list;
        }

        public RecipeView? GetRecipe(string recipeId)
        {
            if (string.IsNullOrEmpty(recipeId)) return null;

            if (_byId.TryGetValue(recipeId, out var cached))
                return cached;

            foreach (RecipeData r in _db.Recipes)
            {
                if (r.id == recipeId)
                {
                    var view = ToView(r);
                    _byId[recipeId] = view;
                    return view;
                }
            }

            return null;
        }

        RecipeView ToView(RecipeData r)
        {
            var resultItem = _db.GetItem(r.result);
            string resultName = resultItem?.name ?? r.result;

            List<ComponentSlotView> compSlots = null;
            if (r.components is { Count: > 0 })
            {
                compSlots = new List<ComponentSlotView>(r.components.Count);
                foreach (ComponentSlot slot in r.components)
                {
                    if (slot.alternatives == null) continue;
                    var alts = new List<ComponentAltView>(slot.alternatives.Count);
                    foreach (ComponentAlt alt in slot.alternatives)
                    {
                        var altItem = _db.GetItem(alt.item);
                        alts.Add(new ComponentAltView(
                            alt.item,
                            altItem?.name ?? alt.item,
                            alt.count));
                    }
                    compSlots.Add(new ComponentSlotView(alts));
                }
            }

            return new RecipeView(
                r.id, r.result, resultName,
                r.category, r.difficulty, r.time_minutes,
                compSlots);
        }
    }
}
