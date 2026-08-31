// ============================================================
// TraitVisualPresenter — 특성 표시 아이콘 공통 진입점
// ============================================================

using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public static class TraitVisualPresenter
{
    static TraitIconCatalog _catalog;

    public static TraitIconCatalog Catalog
    {
        get
        {
            if (_catalog == null)
                _catalog = LoadCatalog();
            return _catalog;
        }
    }

    public static Sprite GetDisplayIcon(string traitId)
    {
        TraitIconCatalog catalog = Catalog;
        if (catalog == null)
            return null;

        Sprite assigned = catalog.GetAssignedIcon(traitId);
        if (assigned != null)
            return assigned;

        return catalog.DefaultIcon;
    }

    public static Sprite GetDefaultIcon() => Catalog?.DefaultIcon;

    public static void BindCatalog(TraitIconCatalog catalog)
    {
        _catalog = catalog;
        catalog?.RebuildCache();
    }

    public static void InvalidateCache() => _catalog = null;

    static TraitIconCatalog LoadCatalog()
    {
        TraitIconCatalog fromResources = Resources.Load<TraitIconCatalog>(TraitIconCatalog.ResourcesLoadName);
        if (fromResources != null)
            return fromResources;

#if UNITY_EDITOR
        return AssetDatabase.LoadAssetAtPath<TraitIconCatalog>(TraitIconCatalog.DefaultAssetPath);
#else
        return null;
#endif
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => InvalidateCache();
}
