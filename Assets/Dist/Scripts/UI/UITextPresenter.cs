// ============================================================
// UITextPresenter — 표시 텍스트 공통 진입점
// ============================================================

using Garunnir.Runtime.Gameplay.Data;

public static class UITextPresenter
{
    public static string GetText(string locKey) =>
        string.IsNullOrEmpty(locKey) ? string.Empty : locKey;

    public static string GetItemName(ItemData item) =>
        item == null ? string.Empty : item.name ?? string.Empty;

    public static string GetContainerName(ContainerData definition) =>
        definition == null ? string.Empty : definition.name ?? string.Empty;
}
