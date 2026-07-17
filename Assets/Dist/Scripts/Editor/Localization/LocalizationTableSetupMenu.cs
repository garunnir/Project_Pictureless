// ============================================================
// LocalizationTableSetupMenu — UI_ko LocalizationTable 생성/선택
// ============================================================

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

static class LocalizationTableSetupMenu
{
    const string AssetFolder = "Assets/Dist/Resources/Localization";

    [MenuItem("Dist/Localization/Select Or Create UI_ko Table")]
    static void SelectOrCreateUiKoTable()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Dist/Resources"))
            AssetDatabase.CreateFolder("Assets/Dist", "Resources");
        if (!AssetDatabase.IsValidFolder(AssetFolder))
            AssetDatabase.CreateFolder("Assets/Dist/Resources", "Localization");

        LocalizationTable table = AssetDatabase.LoadAssetAtPath<LocalizationTable>(LocalizationTable.AssetPath);
        if (table == null)
        {
            table = ScriptableObject.CreateInstance<LocalizationTable>();
            AssetDatabase.CreateAsset(table, LocalizationTable.AssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        Selection.activeObject = table;
    }
}
#endif
