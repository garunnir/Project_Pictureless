// ============================================================
// WeaponAnimClipSpeedsHost — Presentation/Catalog ClipSpeeds 조회·생성
// ============================================================

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class WeaponAnimClipSpeedsHost
{
    public static WeaponAnimClipSpeeds GetExisting(Object host)
    {
        if (host is WeaponPresentation presentation)
        {
            if (presentation.AnimClipSpeeds != null)
                return presentation.AnimClipSpeeds;
            WeaponAnimClipSpeeds fromOverride = FindOn(presentation.AnimatorOverride);
            if (fromOverride == null)
                return null;
            presentation.SetAnimClipSpeeds(fromOverride);
            EditorUtility.SetDirty(presentation);
            return fromOverride;
        }

        if (host is ArmAnimSlotCatalog catalog)
            return catalog.ClipSpeeds;

        return FindOn(host);
    }

    public static WeaponAnimClipSpeeds GetOrCreate(Object host)
    {
        WeaponAnimClipSpeeds existing = GetExisting(host);
        if (existing != null)
            return existing;

        if (host is WeaponPresentation presentation)
            return CreateOn(presentation, presentation.SetAnimClipSpeeds);
        if (host is ArmAnimSlotCatalog catalog)
            return CreateOn(catalog, catalog.SetClipSpeeds);
        return null;
    }

    static WeaponAnimClipSpeeds FindOn(Object host)
    {
        if (host == null)
            return null;
        string path = AssetDatabase.GetAssetPath(host);
        if (string.IsNullOrEmpty(path))
            return null;
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
        for (int i = 0; i < assets.Length; i++)
        {
            if (assets[i] is WeaponAnimClipSpeeds speeds)
                return speeds;
        }

        return null;
    }

    static WeaponAnimClipSpeeds CreateOn(Object host, System.Action<WeaponAnimClipSpeeds> assign)
    {
        string path = AssetDatabase.GetAssetPath(host);
        if (string.IsNullOrEmpty(path) || assign == null)
            return null;

        var speeds = ScriptableObject.CreateInstance<WeaponAnimClipSpeeds>();
        speeds.name = "ClipSpeeds";
        AssetDatabase.AddObjectToAsset(speeds, host);
        assign(speeds);
        EditorUtility.SetDirty(host);
        EditorUtility.SetDirty(speeds);
        AssetDatabase.SaveAssets();
        return speeds;
    }
}
#endif
