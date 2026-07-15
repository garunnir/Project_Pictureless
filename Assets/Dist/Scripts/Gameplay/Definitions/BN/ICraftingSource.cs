// ============================================================
// ICraftingSource — 레시피를 통합 조회하기 위한 인터페이스
// ============================================================

using System.Collections.Generic;

namespace Garunnir.Runtime.Gameplay.Data
{
    public readonly struct RecipeView
    {
        public readonly string Id;
        public readonly string ResultId;
        public readonly string ResultName;
        public readonly string Category;
        public readonly int Difficulty;
        public readonly float TimeMinutes;
        public readonly IReadOnlyList<ComponentSlotView> Components;

        public RecipeView(string id, string resultId, string resultName,
            string category, int difficulty, float timeMinutes,
            IReadOnlyList<ComponentSlotView> components)
        {
            Id = id;
            ResultId = resultId;
            ResultName = resultName;
            Category = category;
            Difficulty = difficulty;
            TimeMinutes = timeMinutes;
            Components = components;
        }
    }

    public readonly struct ComponentSlotView
    {
        public readonly IReadOnlyList<ComponentAltView> Alternatives;
        public ComponentSlotView(IReadOnlyList<ComponentAltView> alternatives) => Alternatives = alternatives;
    }

    public readonly struct ComponentAltView
    {
        public readonly string ItemId;
        public readonly string ItemName;
        public readonly int Count;

        public ComponentAltView(string itemId, string itemName, int count)
        {
            ItemId = itemId;
            ItemName = itemName;
            Count = count;
        }
    }

    public interface ICraftingSource
    {
        IReadOnlyList<RecipeView> GetAllRecipes();
        IReadOnlyList<RecipeView> GetRecipesByCategory(string category);
        IReadOnlyList<RecipeView> FindRecipesUsingIngredient(string itemId);
        RecipeView? GetRecipe(string recipeId);
    }
}
