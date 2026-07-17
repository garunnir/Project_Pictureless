// ============================================================
// RecipeCategoryLabels — BN category/subcategory 표시명
// ============================================================

public static class RecipeCategoryLabels
{
    const string KeyPrefix = "RecipeCategory.";

    public static string Get(string id)
    {
        if (string.IsNullOrEmpty(id))
            return ItemContextMenuLabels.MiscGroup;

        return Loc.Get(KeyPrefix + id);
    }
}
