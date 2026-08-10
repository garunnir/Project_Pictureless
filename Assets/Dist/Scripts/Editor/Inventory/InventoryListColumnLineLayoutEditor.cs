#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(InventoryListColumnLineLayout))]
public sealed class InventoryListColumnLineLayoutEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        SerializedProperty settingsProperty = serializedObject.FindProperty("_settings");
        EditorGUILayout.PropertyField(settingsProperty);

        InventoryListColumnLineLayout lineLayout = (InventoryListColumnLineLayout)target;
        InventoryListColumnLayoutSettings settings = lineLayout.Settings;
        if (settings != null)
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Column Layout", EditorStyles.boldLabel);

            var settingsObject = new SerializedObject(settings);
            settingsObject.Update();
            SerializedProperty iterator = settingsObject.GetIterator();
            if (iterator.NextVisible(true))
            {
                do
                {
                    if (iterator.name == "m_Script")
                        continue;

                    EditorGUILayout.PropertyField(iterator, true);
                }
                while (iterator.NextVisible(false));
            }

            settingsObject.ApplyModifiedProperties();
        }
        else
        {
            EditorGUILayout.HelpBox(
                "Assign InventoryListColumnLayoutSettings. " +
                "Run Dist/MCP/Inventory/Sync List Column Layout to create the default asset.",
                MessageType.Warning);
        }

        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.Space(8f);
        using (new EditorGUI.DisabledScope(settings == null))
        {
            if (GUILayout.Button("Apply to Layout Elements"))
            {
                if (lineLayout.transform.name == "Area_ColumnHeader")
                    lineLayout.Apply(asHeader: true);
                else
                    lineLayout.ApplyDataRowGeometry();
            }
        }
    }
}
#endif
