// ============================================================
// RecipeContextMenuText — 레시피 리프 표시명 포맷
// ============================================================

using Garunnir.Runtime.Gameplay.Data;

static class RecipeContextMenuText
{
    public static string FormatResultLabel(RecipeData recipe)
    {
        if (recipe == null || string.IsNullOrEmpty(recipe.result))
            return "?";

        ItemData resultItem = GameplayData.GetItem(recipe.result);
        string resultName = resultItem?.name ?? recipe.result;
        int count = recipe.result_count > 0 ? recipe.result_count : 1;
        return count > 1 ? $"{resultName} x{count}" : resultName;
    }
}
