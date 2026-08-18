// ============================================================
// CraftingWindowLabels — 제작 창 표시 문구 Loc 키 SSOT
// ============================================================

using UnityEngine;

public static class CraftingWindowLabels
{
    public const string CategoryAllId = "__all__";
    public const string CategoryFavouritesId = "__favourites__";

    const string KeyTitle = "Crafting.Title";
    const string KeyTitleOn = "Crafting.TitleOn";
    const string KeySearchPlaceholder = "Crafting.SearchPlaceholder";
    const string KeyCraft = "Crafting.Craft";
    const string KeyAll = "Crafting.All";
    const string KeyFavourites = "Crafting.Favourites";
    const string KeyRequiredItems = "Crafting.RequiredItems";
    const string KeyOutputs = "Crafting.Outputs";
    const string KeyTimeRequired = "Crafting.TimeRequired";
    const string KeyTimeRemaining = "Crafting.TimeRemaining";
    const string KeyDurationFormat = "Crafting.DurationFormat";
    const string KeyMax = "Crafting.Max";
    const string KeyOutputCountFormat = "Crafting.OutputCountFormat";
    const string KeySkillLine = "Crafting.SkillLine";
    const string KeyTimeMinutes = "Crafting.TimeMinutes";
    const string KeyBookKnown = "Crafting.BookKnown";
    const string KeyCannotCraft = "Crafting.CannotCraft";
    const string KeyQualityFormat = "Crafting.QualityFormat";
    const string KeyQualityCountFormat = "Crafting.QualityCountFormat";
    const string KeyQualityAltFormat = "Crafting.QualityAltFormat";
    const string KeyCountFormat = "Crafting.CountFormat";
    const string KeyCategoryPrefix = "RecipeCategory.";

    public static string Title => Loc.Get(KeyTitle);
    public static string SearchPlaceholder => Loc.Get(KeySearchPlaceholder);
    public static string Craft => Loc.Get(KeyCraft);
    public static string All => Loc.Get(KeyAll);
    public static string Favourites => Loc.Get(KeyFavourites);
    public static string RequiredItems => Loc.Get(KeyRequiredItems);
    public static string Outputs => Loc.Get(KeyOutputs);
    public static string Max => Loc.Get(KeyMax);
    public static string BookKnown => Loc.Get(KeyBookKnown);
    public static string CannotCraft => Loc.Get(KeyCannotCraft);

    public static string FormatTitleOn(string workbenchName) =>
        Loc.Format(KeyTitleOn, workbenchName);

    public static string FormatSkillLine(string skillName, int have, int need) =>
        Loc.Format(KeySkillLine, skillName, have, need);

    public static string FormatTimeMinutes(float minutes) =>
        Loc.Format(KeyTimeMinutes, minutes);

    public static string FormatQuality(string qualityId, int level) =>
        Loc.Format(KeyQualityFormat, qualityId, level);

    public static string FormatQualityCount(int have, int need) =>
        Loc.Format(KeyQualityCountFormat, have, need);

    public static string FormatQualityAlt(string itemName, int level) =>
        Loc.Format(KeyQualityAltFormat, itemName, level);

    public static string FormatCount(int have, int need) =>
        Loc.Format(KeyCountFormat, have, need);

    public static string FormatOutputCount(int count) =>
        Loc.Format(KeyOutputCountFormat, count);

    public static string FormatTimeRequired(float seconds) =>
        Loc.Format(KeyTimeRequired, FormatDuration(seconds));

    public static string FormatTimeRemaining(float seconds) =>
        Loc.Format(KeyTimeRemaining, FormatDuration(seconds));

    static string FormatDuration(float seconds)
    {
        int total = Mathf.Max(0, Mathf.CeilToInt(seconds));
        int minuteDivisor = Mathf.Max(1, Mathf.RoundToInt(CraftingWindowLayout.SecondsPerMinute));

        int minutes = total / minuteDivisor;
        int secs = total % minuteDivisor;
        return Loc.Format(KeyDurationFormat, minutes.ToString("00"), secs.ToString("00"));
    }

    public static string GetCategoryName(string id)
    {
        if (string.IsNullOrEmpty(id))
            return All;

        if (id == CategoryAllId)
            return All;

        if (id == CategoryFavouritesId)
            return Favourites;

        return Loc.Get(KeyCategoryPrefix + id);
    }
}
