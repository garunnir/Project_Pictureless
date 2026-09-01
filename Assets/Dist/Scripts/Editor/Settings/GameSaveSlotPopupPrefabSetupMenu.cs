// ============================================================
// GameSaveSlotPopupPrefabSetupMenu — 슬롯 팝업 프리팹 Ensure (MCP)
// ============================================================

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

static class GameSaveSlotPopupPrefabSetupMenu
{
    [MenuItem(DistMcpMenus.SettingsCreateGameSaveSlotPopupPrefabIfMissing)]
    static void CreatePrefabIfMissing()
    {
        SettingsUISetupMenu.EnsureSettingsFolder();

        UIGameSaveSlotPopup existing =
            AssetDatabase.LoadAssetAtPath<UIGameSaveSlotPopup>(GameSaveSlotPopupFactory.DefaultPrefabPath);
        if (existing != null)
        {
            Debug.Log(
                $"[GameSaveSlotPopupPrefabSetupMenu] Prefab already exists: {GameSaveSlotPopupFactory.DefaultPrefabPath}",
                existing);
            Selection.activeObject = existing;
            return;
        }

        UIGameSaveSlotPopup popup = GameSaveSlotPopupFactory.CreatePopupRoot();
        GameObject root = popup.gameObject;
        PrefabUtility.SaveAsPrefabAsset(root, GameSaveSlotPopupFactory.DefaultPrefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[GameSaveSlotPopupPrefabSetupMenu] Created prefab: {GameSaveSlotPopupFactory.DefaultPrefabPath}");
        Selection.activeObject =
            AssetDatabase.LoadAssetAtPath<UIGameSaveSlotPopup>(GameSaveSlotPopupFactory.DefaultPrefabPath);
    }
}
#endif
