// ============================================================
// UITextPresenter — 표시 텍스트 공통 진입점 (Pixel Crushers 레거시 제거)
// ============================================================

public static class UITextPresenter
{
    public static string GetText(string locKey) =>
        string.IsNullOrEmpty(locKey) ? string.Empty : locKey;

    public static string GetItemName(Garunnir.Runtime.Gameplay.Item.ItemDefinitionSO item) =>
        item == null ? string.Empty : GetText(item.LocKey);

    public static string GetContainerName(Garunnir.Runtime.Gameplay.Item.ContainerDefinitionSO definition) =>
        definition == null ? string.Empty : GetText(definition.LocKey);
}
