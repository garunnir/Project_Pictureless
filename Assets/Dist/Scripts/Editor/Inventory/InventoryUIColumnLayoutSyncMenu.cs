// ============================================================
// InventoryUIColumnLayoutSyncMenu — Dist/MCP 열 geometry Sync (에이전트용)
// ============================================================

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

static class InventoryUIColumnLayoutSyncMenu
{
    const string WindowPath = InventoryUIHierarchyBuilder.PrefabFolder + "/Grp_InventoryListWindow.prefab";
    const string RowPath = InventoryUIHierarchyBuilder.PrefabFolder + "/Grp_ItemListRow.prefab";

    [MenuItem(DistMcpMenus.InventorySyncListColumnLayout)]
    static void SyncListColumnLayout()
    {
        InventoryListColumnLayoutSettings settings = InventoryListColumnLayoutSettingsUtility.LoadOrCreateSettings();
        bool rowOk = SyncRowLayout(settings);
        bool windowOk = SyncWindowLayout(settings);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (rowOk && windowOk)
        {
            Debug.Log(
                "[InventoryUIColumnLayoutSyncMenu] Synced row + window column layout " +
                $"(Settings: {InventoryListColumnLayoutSettingsUtility.SettingsAssetPath}).");
        }
    }

    static bool SyncRowLayout(InventoryListColumnLayoutSettings settings)
    {
        GameObject rowRoot = PrefabUtility.LoadPrefabContents(RowPath);
        if (rowRoot == null)
        {
            Debug.LogError($"[InventoryUIColumnLayoutSyncMenu] Failed to load: {RowPath}");
            return false;
        }

        try
        {
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(rowRoot);
            InventoryListColumnLayoutSettingsUtility.EnsureLineLayout(rowRoot.transform, settings, dataRow: true);
            bool saved = PrefabUtility.SaveAsPrefabAsset(rowRoot, RowPath);
            if (!saved)
            {
                Debug.LogError($"[InventoryUIColumnLayoutSyncMenu] Failed to save row prefab: {RowPath}");
                return false;
            }

            Debug.Log(
                $"[InventoryUIColumnLayoutSyncMenu] Synced row layout on {RowPath} " +
                $"(fontCategory={settings.FontCategory}, rowHeight={settings.RowHeight}, " +
                $"categoryWidth={settings.CategoryWidth}).");
            return true;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(rowRoot);
        }
    }

    static bool SyncWindowLayout(InventoryListColumnLayoutSettings settings)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(WindowPath);
        if (root == null)
        {
            Debug.LogError($"[InventoryUIColumnLayoutSyncMenu] Failed to load: {WindowPath}");
            return false;
        }

        try
        {
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(root);
            InventoryUIHierarchyBuilder.PatchExistingWindowListColumnLayout(root, settings);
            bool saved = PrefabUtility.SaveAsPrefabAsset(root, WindowPath);
            if (!saved)
            {
                Debug.LogError($"[InventoryUIColumnLayoutSyncMenu] Failed to save window prefab: {WindowPath}");
                return false;
            }

            return true;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }
}
#endif
