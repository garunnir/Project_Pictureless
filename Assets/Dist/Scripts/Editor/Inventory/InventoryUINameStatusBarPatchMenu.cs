// ============================================================
// InventoryUINameStatusBarPatchMenu — 행 Name 셀 겹침 fill Ensure
// ============================================================

#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;

static class InventoryUINameStatusBarPatchMenu
{
    const string RowPath = InventoryUIHierarchyBuilder.PrefabFolder + "/Grp_ItemListRow.prefab";

    [MenuItem("Dist/Inventory/Patch Row Name Status Bar")]
    static void PatchRowNameStatusBar()
    {
        GameObject rowRoot = PrefabUtility.LoadPrefabContents(RowPath);
        if (rowRoot == null)
        {
            Debug.LogError($"[InventoryUINameStatusBarPatchMenu] Failed to load: {RowPath}");
            return;
        }

        try
        {
            UIItemListRow row = rowRoot.GetComponent<UIItemListRow>();
            if (row == null)
            {
                Debug.LogError("[InventoryUINameStatusBarPatchMenu] UIItemListRow missing.", rowRoot);
                return;
            }

            SerializedObject so = new(row);
            SerializedProperty nameProp = so.FindProperty("_nameText");
            TMP_Text nameText = nameProp != null
                ? nameProp.objectReferenceValue as TMP_Text
                : null;

            if (nameText == null)
            {
                Transform name = rowRoot.transform.Find("Name");
                if (name != null)
                    nameText = name.GetComponent<TMP_Text>();
            }

            if (nameText == null)
            {
                Debug.LogError("[InventoryUINameStatusBarPatchMenu] Name TMP missing.", rowRoot);
                return;
            }

            ItemNameStatusBar.Ensure(ref nameText);
            if (nameProp != null)
            {
                nameProp.objectReferenceValue = nameText;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            bool saved = PrefabUtility.SaveAsPrefabAsset(rowRoot, RowPath);
            if (!saved)
            {
                Debug.LogError($"[InventoryUINameStatusBarPatchMenu] Failed to save: {RowPath}");
                return;
            }

            Debug.Log($"[InventoryUINameStatusBarPatchMenu] Patched Name overlay bar on {RowPath}.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(rowRoot);
        }
    }
}
#endif
