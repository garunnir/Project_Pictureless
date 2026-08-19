// ============================================================
// RecipeCategoryLabels — BN category/subcategory 표시명
// ============================================================

public static class RecipeCategoryLabels
{
    public static string Get(string id)
    {
        if (string.IsNullOrEmpty(id))
            return ItemContextMenuLabels.MiscGroup;

        return UITextPresenter.GetRecipeCategory(id);
    }
}
