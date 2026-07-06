// ============================================================
// GameplayData — 게임플레이 SO 진입점 (ResourceManager 레거시 대체)
// ============================================================

using Garunnir.Runtime.Gameplay.Item;
using UnityEngine;

public static class GameplayData
{
    static ItemCatalogSO _itemCatalog;

    public static ItemCatalogSO ItemCatalog
    {
        get
        {
            if (_itemCatalog == null)
            {
                var host = Object.FindAnyObjectByType<GameplayCatalogHost>();
                _itemCatalog = host != null ? host.ItemCatalog : null;
            }

            return _itemCatalog;
        }
    }

    public static void Register(ItemCatalogSO catalog) => _itemCatalog = catalog;

    public static void Unregister(ItemCatalogSO catalog)
    {
        if (_itemCatalog == catalog)
            _itemCatalog = null;
    }

    public static void ClearCache() => _itemCatalog = null;
}
