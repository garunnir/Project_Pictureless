#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(InventoryListColumnLayoutSettings))]
public sealed class InventoryListColumnLayoutSettingsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(8f);
        if (GUILayout.Button("Apply to all InventoryListColumnLineLayout in open prefab/scene"))
            ApplyToAllLineLayouts();

        if (GUILayout.Button("Sync List Column Layout (row + window prefabs)"))
            EditorApplication.ExecuteMenuItem(DistMcpMenus.InventorySyncListColumnLayout);
    }

    static void ApplyToAllLineLayouts()
    {
        var layouts = Object.FindObjectsByType<InventoryListColumnLineLayout>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        if (layouts.Length == 0)
        {
            Debug.Log("[InventoryListColumnLayoutSettings] No InventoryListColumnLineLayout found in open contexts.");
            return;
        }

        for (int i = 0; i < layouts.Length; i++)
        {
            if (layouts[i] == null)
                continue;

            bool asHeader = layouts[i].transform.name == "Area_ColumnHeader";
            if (asHeader)
                layouts[i].Apply(asHeader: true);
            else
                layouts[i].ApplyDataRowGeometry();
            EditorUtility.SetDirty(layouts[i]);
        }

        Debug.Log($"[InventoryListColumnLayoutSettings] Applied column layout to {layouts.Length} line layout(s).");
    }
}
#endif
