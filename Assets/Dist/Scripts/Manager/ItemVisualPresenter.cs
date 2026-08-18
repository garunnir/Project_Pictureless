// ============================================================
// ItemVisualPresenter — 아이템 표시 아이콘 공통 진입점 (UI·월드 SSOT)
// ============================================================

using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public static class ItemVisualPresenter
{
    public const string DefaultIconAssetPath =
        "Assets/Dist/Visual/Sprites/Textures/UI/Inventory/ui_icon_empty.png";

    static ItemIconCatalog _catalog;
    static Sprite _fallbackIcon;
    static bool _fallbackLoadAttempted;

    public static ItemIconCatalog Catalog
    {
        get
        {
            if (_catalog == null)
                _catalog = LoadCatalog();
            return _catalog;
        }
    }

    /// <summary>아이템별 아이콘. 카탈로그 할당 → BN 타일셋 → 기본 폴백.</summary>
    public static Sprite GetDisplayIcon(string itemId)
    {
        ItemIconCatalog catalog = Catalog;
        if (catalog != null)
        {
            Sprite assigned = catalog.GetAssignedIcon(itemId);
            if (assigned != null)
                return assigned;
        }

        if (BnTilesetIconResolver.TryGet(itemId, out Sprite tilesetIcon))
            return tilesetIcon;

        return GetDefaultIcon();
    }

    public static Sprite GetDefaultIcon()
    {
        ItemIconCatalog catalog = Catalog;
        if (catalog != null && catalog.DefaultIcon != null)
            return catalog.DefaultIcon;

        return LoadFallbackIcon();
    }

    public static void BindCatalog(ItemIconCatalog catalog)
    {
        _catalog = catalog;
        catalog?.RebuildCache();
    }

    public static void InvalidateCache()
    {
        _catalog = null;
        _fallbackIcon = null;
        _fallbackLoadAttempted = false;
        BnTilesetIconResolver.Invalidate();
        QualityVisualPresenter.Invalidate();
    }

    static ItemIconCatalog LoadCatalog()
    {
        ItemIconCatalog fromResources = Resources.Load<ItemIconCatalog>(ItemIconCatalog.ResourcesLoadName);
        if (fromResources != null)
            return fromResources;

#if UNITY_EDITOR
        return AssetDatabase.LoadAssetAtPath<ItemIconCatalog>(ItemIconCatalog.AssetPath);
#else
        return null;
#endif
    }

    static Sprite LoadFallbackIcon()
    {
        if (_fallbackIcon != null)
            return _fallbackIcon;

        if (_fallbackLoadAttempted)
            return null;

        _fallbackLoadAttempted = true;

#if UNITY_EDITOR
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(DefaultIconAssetPath);
        for (int i = 0; i < assets.Length; i++)
        {
            if (assets[i] is Sprite sprite)
            {
                _fallbackIcon = sprite;
                return _fallbackIcon;
            }
        }
#endif
        Debug.LogError(
            $"[ItemVisualPresenter] Default icon missing. Assign ItemIconCatalog.DefaultIcon or '{DefaultIconAssetPath}'.");
        return null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => InvalidateCache();
}
