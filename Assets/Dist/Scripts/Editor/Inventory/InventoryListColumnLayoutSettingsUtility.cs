#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

static class InventoryListColumnLayoutSettingsUtility
{
    public const string SettingsAssetPath =
        "Assets/Dist/Resources/Inventory/InventoryListColumnLayoutSettings.asset";

    public static InventoryListColumnLayoutSettings LoadOrCreateSettings()
    {
        var existing = AssetDatabase.LoadAssetAtPath<InventoryListColumnLayoutSettings>(SettingsAssetPath);
        if (existing != null)
        {
            InventoryListColumnLayoutSettings.SetCachedDefault(existing);
            return existing;
        }

        string folder = Path.GetDirectoryName(SettingsAssetPath)?.Replace('\\', '/');
        if (!string.IsNullOrEmpty(folder) && !AssetDatabase.IsValidFolder(folder))
        {
            string parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
            string leaf = Path.GetFileName(folder);
            if (!string.IsNullOrEmpty(parent) && !string.IsNullOrEmpty(leaf))
                AssetDatabase.CreateFolder(parent, leaf);
        }

        var settings = ScriptableObject.CreateInstance<InventoryListColumnLayoutSettings>();
        AssetDatabase.CreateAsset(settings, SettingsAssetPath);
        AssetDatabase.SaveAssets();
        InventoryListColumnLayoutSettings.SetCachedDefault(settings);
        return settings;
    }

    public static void EnsureLineLayout(Transform lineRoot, InventoryListColumnLayoutSettings settings, bool dataRow)
    {
        if (lineRoot == null || settings == null)
            return;

        if (!lineRoot.TryGetComponent(out InventoryListColumnLineLayout lineLayout))
            lineLayout = lineRoot.gameObject.AddComponent<InventoryListColumnLineLayout>();

        var so = new SerializedObject(lineLayout);
        so.FindProperty("_settings").objectReferenceValue = settings;
        so.ApplyModifiedPropertiesWithoutUndo();

        if (dataRow)
            lineLayout.ApplyDataRowGeometry();
        else
            lineLayout.Apply(asHeader: true);

        EditorUtility.SetDirty(lineRoot.gameObject);
        EditorUtility.SetDirty(lineLayout);
    }
}
#endif
