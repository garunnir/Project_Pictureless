// ============================================================
// InventoryUIResizeHandlersPatchMenu — Dist/MCP 리사이즈 핸들 Patch (에이전트용)
// ============================================================

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

static class InventoryUIResizeHandlersPatchMenu
{
    const string WindowPath = InventoryUIHierarchyBuilder.PrefabFolder + "/Grp_InventoryListWindow.prefab";

    [MenuItem(DistMcpMenus.InventoryPatchWindowResizeHandlers)]
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
