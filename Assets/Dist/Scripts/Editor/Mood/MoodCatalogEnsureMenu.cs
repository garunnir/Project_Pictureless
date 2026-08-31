// ============================================================
// MoodCatalogEnsureMenu — MoodSettings 사고 표·무드 에셋 Ensure (MCP)
// ============================================================

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

static class MoodCatalogEnsureMenu
{
    [MenuItem(DistMcpMenus.MoodEnsureCatalog)]
    static void EnsureCatalog()
    {
        MoodSettings settings =
            DistScriptableObjectEnsure.LoadOrCreate<MoodSettings>(MoodSettings.DefaultAssetPath);
        if (settings == null)
            return;

        int before = CountThoughts(settings);
        settings.EnsureCatalogRows();
        int after = CountThoughts(settings);
        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();
        Debug.Log(
            $"[MoodCatalogEnsureMenu] MoodSettings thoughts {before} → {after} at {MoodSettings.DefaultAssetPath}");
    }

    static int CountThoughts(MoodSettings settings)
    {
        SerializedObject so = new(settings);
        SerializedProperty prop = so.FindProperty("_thoughts");
        return prop != null ? prop.arraySize : 0;
    }
}
#endif
