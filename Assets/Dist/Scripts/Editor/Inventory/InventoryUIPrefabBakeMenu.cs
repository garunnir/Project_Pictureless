// ============================================================
// InventoryUIPrefabBakeMenu — 인벤 UI 프리팹 베이크 (단일 레이아웃)
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

        Object.DestroyImmediate(rowRoot.gameObject);
        Object.DestroyImmediate(slotRoot.gameObject);

        // Do NOT rebuild Grp_InventoryListWindow from scratch — that wipes Area_InvInfo
        // and other hand-authored chrome. Scrollbars: Dist/Inventory/Patch Window Scrollbars.
        if (AssetDatabase.LoadAssetAtPath<GameObject>(WindowPath) == null)
        {
            Debug.LogWarning(
                $"[InventoryUIPrefabBakeMenu] Window prefab missing at {WindowPath}. " +
                "Create via BuildWindowRoot once, then use Patch Window Scrollbars.");
        }
        else
        {
            Debug.Log(
                "[InventoryUIPrefabBakeMenu] Row/slot baked. Window left untouched " +
                "(use Dist/Inventory/Patch Window Scrollbars for scrollbars).");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
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
