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

        string sourceName = item.name ?? string.Empty;
        if (string.IsNullOrEmpty(item.id))
            return sourceName;

        return Loc.TryGet(KeyItemPrefix + item.id, out string localizedName)
            ? localizedName
            : sourceName;
    }

    public static string GetContainerName(ContainerData definition)
    {
        if (definition == null)
            return string.Empty;

        string sourceName = definition.name ?? string.Empty;
        if (string.IsNullOrEmpty(definition.id))
            return sourceName;

        return Loc.TryGet(KeyContainerPrefix + definition.id, out string localizedName)
            ? localizedName
            : sourceName;
    }
}
