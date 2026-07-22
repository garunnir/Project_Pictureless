// ============================================================
// InventoryUIColumnHeaderPatchMenu — 창 프리팹에 컬럼 헤더만 패치 (전체 rebake 금지)
// ============================================================

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

static class InventoryUIColumnHeaderPatchMenu
{
    const string WindowPath = InventoryUIHierarchyBuilder.PrefabFolder + "/Grp_InventoryListWindow.prefab";

    [MenuItem("Dist/Inventory/Patch Window Column Header")]
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
