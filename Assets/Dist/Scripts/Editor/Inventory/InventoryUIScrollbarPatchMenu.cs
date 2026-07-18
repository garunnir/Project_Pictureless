// ============================================================
// InventoryUIScrollbarPatchMenu — 기존 창 프리팹에 스크롤바만 패치 (전체 rebake 금지)
// ============================================================

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

static class InventoryUIScrollbarPatchMenu
{
    const string WindowPath = InventoryUIHierarchyBuilder.PrefabFolder + "/Grp_InventoryListWindow.prefab";

    [MenuItem("Dist/Inventory/Patch Window Scrollbars")]
    static void PatchWindowScrollbars()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(WindowPath);
        if (root == null)
        {
            Debug.LogError($"[InventoryUIScrollbarPatchMenu] Failed to load: {WindowPath}");
            return;
        }

        try
        {
            InventoryUIHierarchyBuilder.PatchExistingWindowScrollbars(root);
            PrefabUtility.SaveAsPrefabAsset(root, WindowPath);
            Debug.Log($"[InventoryUIScrollbarPatchMenu] Patched scrollbars on {WindowPath} (Area_InvInfo preserved).");
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
