#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

static class InventoryListColumnLayoutSettingsUtility
{
    public const string SettingsAssetPath = InventoryListColumnLayoutSettings.DefaultAssetPath;

    public static InventoryListColumnLayoutSettings LoadOrCreateSettings()
    {
        InventoryListColumnLayoutSettings settings =
            DistScriptableObjectEnsure.LoadOrCreate<InventoryListColumnLayoutSettings>(SettingsAssetPath);
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
