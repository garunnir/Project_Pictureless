// ============================================================
// ItemVisualPresenter — 아이템 표시 아이콘 공통 진입점 (UI·월드 SSOT)
// ============================================================

using Garunnir.Runtime.Gameplay.Item;
using UnityEngine;

public static class ItemVisualPresenter
{
    public static Sprite GetDisplayIcon(ItemDefinitionSO item)
    {
        ItemCatalogSO catalog = GameplayData.ItemCatalog;
        if (catalog != null)
            return catalog.ResolveDisplayIcon(item);

        return item != null ? item.Icon : null;
    }

    public static Sprite GetDefaultIcon() => GameplayData.ItemCatalog?.DefaultItemIcon;
}
