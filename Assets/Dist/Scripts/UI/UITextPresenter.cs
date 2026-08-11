// ============================================================
// UITextPresenter — 표시 텍스트 공통 진입점
// ============================================================

using Garunnir.Runtime.Gameplay.Data;

public static class UITextPresenter
{
    const string KeyItemPrefix = "Item.";
    const string KeyContainerPrefix = "Container.";

    public static string GetText(string locKey) =>
        string.IsNullOrEmpty(locKey) ? string.Empty : Loc.Get(locKey);

    public static string GetItemName(ItemData item)
    {
        if (item == null)
            return string.Empty;

        if (string.IsNullOrEmpty(item.id))
            return string.Empty;

        if (Loc.TryGet(KeyItemPrefix + item.id, out string forced) && !string.IsNullOrEmpty(forced))
            return forced;

        DisplayLanguage language = LocalizationBundle.Get()?.ActiveLanguage ?? DisplayLanguage.Ko;
        return ItemNameTable.Get(item.id, language);
    }

    public static string GetItemName(string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
            return string.Empty;

        if (Loc.TryGet(KeyItemPrefix + itemId, out string forced) && !string.IsNullOrEmpty(forced))
            return forced;

        DisplayLanguage language = LocalizationBundle.Get()?.ActiveLanguage ?? DisplayLanguage.Ko;
        return ItemNameTable.Get(itemId, language);
    }

    public static string GetContainerName(ContainerData definition)
    {
        if (definition == null)
            return string.Empty;

        if (string.IsNullOrEmpty(definition.id))
            return string.Empty;

        if (Loc.TryGet(KeyContainerPrefix + definition.id, out string localizedName) &&
            !string.IsNullOrEmpty(localizedName))
            return localizedName;

        // Containers are not in item_names yet — Dist Loc override or missing marker via id
        return ItemNameTable.Get(definition.id, LocalizationBundle.Get()?.ActiveLanguage ?? DisplayLanguage.Ko);
    }
}
