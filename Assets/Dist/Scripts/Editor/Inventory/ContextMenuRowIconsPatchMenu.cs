// ============================================================
// ContextMenuRowIconsPatchMenu — Dist/MCP 컨텍스트 메뉴 행 아이콘 Patch (에이전트용)
// ============================================================

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

static class ContextMenuRowIconsPatchMenu
{
    static readonly string[] PrefabPaths =
    {
        InventoryUIHierarchyBuilder.PrefabFolder + "/ItemContextMenu.prefab",
        InventoryUIHierarchyBuilder.PrefabFolder + "/TileObjectContextMenu.prefab",
        "Assets/Dist/Resources/UI/TileObjectContextMenu.prefab",
    };

    [MenuItem(DistMcpMenus.ContextMenuPatchRowIcons)]
    static void PatchRowIcons()
    {
        int prefabsPatched = 0;
        int rowsPatched = 0;
        for (int i = 0; i < PrefabPaths.Length; i++)
        {
            int rows = PatchPrefab(PrefabPaths[i]);
            if (rows < 0)
                continue;

            prefabsPatched++;
            rowsPatched += rows;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log(
            $"[ContextMenuRowIconsPatchMenu] Patched {rowsPatched} row(s) on {prefabsPatched} prefab(s).");
    }

    static int PatchPrefab(string path)
    {
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing == null)
        {
            Debug.LogWarning($"[ContextMenuRowIconsPatchMenu] Missing prefab: {path}");
            return -1;
        }

        GameObject root = PrefabUtility.LoadPrefabContents(path);
        if (root == null)
        {
            Debug.LogError($"[ContextMenuRowIconsPatchMenu] Failed to load: {path}");
            return -1;
        }

        try
        {
            int rows = InventoryUIHierarchyBuilder.PatchContextMenuRowIcons(root);
            if (rows == 0)
            {
                Debug.LogWarning($"[ContextMenuRowIconsPatchMenu] No UIContextMenuItemRow on {path}.");
                return 0;
            }

            bool saved = PrefabUtility.SaveAsPrefabAsset(root, path);
            if (!saved)
            {
                Debug.LogError($"[ContextMenuRowIconsPatchMenu] Failed to save: {path}");
                return -1;
            }

            Debug.Log($"[ContextMenuRowIconsPatchMenu] Patched {rows} row(s) on {path}.");
            return rows;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }
}
#endif
