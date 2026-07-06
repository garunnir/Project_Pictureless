// ============================================================
// InventoryUIPrefabBakeMenu — 인벤 UI 프리팹 3종 베이크 메뉴
// ============================================================

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

static class InventoryUIPrefabBakeMenu
{
    const string RowPath = InventoryUIHierarchyBuilder.PrefabFolder + "/Grp_ItemListRow.prefab";
    const string SlotPath = InventoryUIHierarchyBuilder.PrefabFolder + "/Grp_ContainerSlot.prefab";
    const string WindowPath = InventoryUIHierarchyBuilder.PrefabFolder + "/Grp_InventoryListWindow.prefab";

    [MenuItem("Dist/Inventory/Bake UI Prefabs")]
    static void BakePrefabs()
    {
        EnsureFolder();

        UIItemListRow rowRoot = InventoryUIHierarchyBuilder.BuildRowPrefabRoot();
        UIContainerSlot slotRoot = InventoryUIHierarchyBuilder.BuildSlotPrefabRoot();
        SavePrefab(rowRoot.gameObject, RowPath);
        SavePrefab(slotRoot.gameObject, SlotPath);

        UIItemListRow rowPrefab = AssetDatabase.LoadAssetAtPath<UIItemListRow>(RowPath);
        UIContainerSlot slotPrefab = AssetDatabase.LoadAssetAtPath<UIContainerSlot>(SlotPath);
        if (rowPrefab == null || slotPrefab == null)
        {
            Debug.LogError("[InventoryUIPrefabBakeMenu] Failed to load row/slot prefabs after save.");
            return;
        }

        UIInventoryListWindow windowRoot = InventoryUIHierarchyBuilder.BuildWindowRoot(rowPrefab, slotPrefab);
        SavePrefab(windowRoot.gameObject, WindowPath);

        Object.DestroyImmediate(rowRoot.gameObject);
        Object.DestroyImmediate(slotRoot.gameObject);
        Object.DestroyImmediate(windowRoot.gameObject);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[InventoryUIPrefabBakeMenu] Saved prefabs under {InventoryUIHierarchyBuilder.PrefabFolder}.");
    }

    static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Dist/Visual/Prefabs/UIComponents"))
            AssetDatabase.CreateFolder("Assets/Dist/Visual/Prefabs", "UIComponents");

        if (!AssetDatabase.IsValidFolder("Assets/Dist/Visual/Prefabs/UIComponents/Inventory"))
            AssetDatabase.CreateFolder("Assets/Dist/Visual/Prefabs/UIComponents", "Inventory");
    }

    static void SavePrefab(GameObject root, string path)
    {
        PrefabUtility.SaveAsPrefabAsset(root, path, out bool success);
        if (!success)
            Debug.LogError($"[InventoryUIPrefabBakeMenu] Failed to save prefab: {path}");
    }
}
#endif
