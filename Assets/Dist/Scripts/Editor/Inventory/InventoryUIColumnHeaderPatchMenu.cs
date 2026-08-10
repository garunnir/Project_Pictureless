// ============================================================
// InventoryUIColumnHeaderPatchMenu — Dist/MCP 컬럼 헤더 Patch (에이전트용)
// ============================================================

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

static class InventoryUIColumnHeaderPatchMenu
{
    const string WindowPath = InventoryUIHierarchyBuilder.PrefabFolder + "/Grp_InventoryListWindow.prefab";

    [MenuItem(DistMcpMenus.InventoryPatchWindowColumnHeader)]
    static void PatchWindowColumnHeader()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(WindowPath);
        if (root == null)
        {
            Debug.LogError($"[InventoryUIColumnHeaderPatchMenu] Failed to load: {WindowPath}");
            return;
        }

        try
        {
            InventoryUIHierarchyBuilder.PatchExistingWindowColumnHeader(root);
            PrefabUtility.SaveAsPrefabAsset(root, WindowPath);
            Debug.Log(
                $"[InventoryUIColumnHeaderPatchMenu] Patched column header on {WindowPath} " +
                "(Area_InvInfo preserved; viewport top inset applied).");
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
