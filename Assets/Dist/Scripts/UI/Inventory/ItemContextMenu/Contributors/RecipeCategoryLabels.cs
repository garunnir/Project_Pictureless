// ============================================================
// RecipeCategoryLabels — BN category/subcategory 표시명
// ============================================================

using System.Collections.Generic;

public static class RecipeCategoryLabels
{
    const string KeyPrefix = "RecipeCategory.";

    static readonly Dictionary<string, string> Fallbacks = new Dictionary<string, string>
    {
        { "CC_FOOD", "음식" },
        { "CC_DRINK", "음료" },
        { "CC_CHEM", "화학" },
        { "CC_AMMO", "탄약" },
        { "CC_WEAPON", "무기" },
        { "CC_ARMOR", "방어구" },
        { "CC_ELECTRONIC", "전자" },
        { "CC_MISC", "기타" },
        { "CSC_FOOD_MEAT", "육류" },
        { "CSC_FOOD_VEGGI", "채소" },
        { "CSC_FOOD_OTHER", "기타 음식" },
    };

    public static string Get(string id)
    {
        if (string.IsNullOrEmpty(id))
            return ItemContextMenuLabels.MiscGroup;

        string fallback = Fallbacks.TryGetValue(id, out string label) ? label : id;
        return Loc.Get(KeyPrefix + id, fallback);
    }
}
