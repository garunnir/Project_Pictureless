// ============================================================
// InventoryUIResizeHandlersPatchMenu — 기존 창 프리팹에 리사이즈 핸들 배열만 배선
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
                $"[InventoryUIResizeHandlersPatchMenu] Wired _resizeHandlers on {WindowPath}.");
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
