// ============================================================
// RecipeContextMenuText — 레시피 리프 표시명 포맷
// ============================================================

using Garunnir.Runtime.Gameplay.Data;

static class RecipeContextMenuText
{
    const string KeyResultCountFormat = "Recipe.ResultCountFormat";

    public static string FormatResultLabel(RecipeData recipe)
    {
        if (recipe == null || string.IsNullOrEmpty(recipe.result))
            return ItemContextMenuLabels.UnknownResult;

        ItemData resultItem = GameplayData.GetItem(recipe.result);
        string resultName = resultItem != null
            ? UITextPresenter.GetItemName(resultItem)
            : recipe.result;
        int count = recipe.result_count > 0 ? recipe.result_count : 1;
        return count > 1
            ? Loc.Format(KeyResultCountFormat, resultName, count)
            : resultName;
    }
}
