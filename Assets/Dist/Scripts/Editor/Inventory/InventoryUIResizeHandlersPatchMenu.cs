// ============================================================
// InventoryUIResizeHandlersPatchMenu — 구 핸들 제거 + UIWindowResizeHandles 부착
// ============================================================

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

static class InventoryUIResizeHandlersPatchMenu
{
    const string WindowPath = InventoryUIHierarchyBuilder.PrefabFolder + "/Grp_InventoryListWindow.prefab";

    [MenuItem("Dist/Inventory/Patch Window Resize Handlers")]
    static void PatchWindowResizeHandlers()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(WindowPath);
        if (root == null)
        {
            Debug.LogError($"[InventoryUIResizeHandlersPatchMenu] Failed to load: {WindowPath}");
            return;
        }

        try
        {
            InventoryUIHierarchyBuilder.PatchExistingWindowResizeHandlers(root);
            PrefabUtility.SaveAsPrefabAsset(root, WindowPath);
            Debug.Log(
                $"[InventoryUIResizeHandlersPatchMenu] Applied UIWindowResizeHandles on {WindowPath}.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}
#endif
