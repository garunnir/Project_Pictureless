// ============================================================
// SettingsUISetupMenu — Dist/MCP 세팅 창 Setup (로드만, bake 금지)
// ============================================================

#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

static class SettingsUISetupMenu
{
    const string PrefabFolder = "Assets/Dist/Visual/Prefabs/UIComponents/Settings";
    const string WindowPrefabPath = PrefabFolder + "/Grp_SettingsWindow.prefab";
    const string ControllerObjectName = "SettingsController";

    [MenuItem(DistMcpMenus.SettingsCreatePrefabIfMissing)]
    static void CreatePrefabIfMissing()
    {
        EnsureFolder();
        UISettingsWindow existing =
            AssetDatabase.LoadAssetAtPath<UISettingsWindow>(WindowPrefabPath);
        if (existing != null)
        {
            Debug.Log($"[SettingsUISetupMenu] Prefab already exists: {WindowPrefabPath}", existing);
            Selection.activeObject = existing;
            return;
        }

        UISettingsWindow window = SettingsUIFactory.CreateWindowRoot();
        GameObject root = window.gameObject;
        UIWindowChromeBarPrefabPatch.Apply(
            root,
            createHeaderIfMissing: false,
            addFoldedTitle: false,
            foldedTitleText: null);
        PrefabUtility.SaveAsPrefabAsset(root, WindowPrefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[SettingsUISetupMenu] Created prefab: {WindowPrefabPath}");
        Selection.activeObject =
            AssetDatabase.LoadAssetAtPath<UISettingsWindow>(WindowPrefabPath);
    }

    [MenuItem(DistMcpMenus.SettingsSetupCanvas)]
    static void SetupCanvasInOpenScene()
    {
        Canvas canvas = ResolveUiCanvas();
        if (canvas == null)
        {
            Debug.LogError("[SettingsUISetupMenu] Canvas not found.");
            return;
        }

        UICanvasLayerHost layerHost = canvas.GetComponent<UICanvasLayerHost>();
        if (layerHost == null)
            layerHost = Undo.AddComponent<UICanvasLayerHost>(canvas.gameObject);
        layerHost.EditorSetupLayerHierarchy();

        Transform systemRoot = SystemHierarchySetup.ResolveSystemRoot();
        if (systemRoot == null)
        {
            Debug.LogError("[SettingsUISetupMenu] InputManager parent (System root) not found.");
            return;
        }

        Transform settingsRoot = SystemHierarchySetup.EnsureCategory(
            systemRoot,
            SystemHierarchySetup.Settings);

        UISettingsWindow prefab =
            AssetDatabase.LoadAssetAtPath<UISettingsWindow>(WindowPrefabPath);
        if (prefab == null)
        {
            Debug.LogError(
                $"[SettingsUISetupMenu] Prefab missing: {WindowPrefabPath}. " +
                "Run " + DistMcpMenus.SettingsCreatePrefabIfMissing + " first.");
            return;
        }

        UISettingsController controller = Object.FindAnyObjectByType<UISettingsController>();
        if (controller == null)
        {
            GameObject go = new(ControllerObjectName);
            Undo.RegisterCreatedObjectUndo(go, "Create SettingsController");
            go.transform.SetParent(settingsRoot, false);
            controller = Undo.AddComponent<UISettingsController>(go);
        }
        else
        {
            SystemHierarchySetup.EnsureChildUnder(
                settingsRoot,
                controller.transform,
                "Move SettingsController Under System/Settings");
        }

        EnsureCanvasServices(canvas);

        SerializedObject so = new(controller);
        so.FindProperty("_uiCanvas").objectReferenceValue = canvas;
        so.FindProperty("_layerHost").objectReferenceValue = layerHost;
        so.FindProperty("_windowPrefab").objectReferenceValue = prefab;
        so.FindProperty("_window").objectReferenceValue = null;
        so.ApplyModifiedPropertiesWithoutUndo();

        MergeLocalizationKeys();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[SettingsUISetupMenu] Settings controller wired.", controller);
    }

    [MenuItem(DistMcpMenus.SettingsMergeLocalizationKeys)]
    static void MergeLocalizationKeysMenu() => MergeLocalizationKeys();

    static void EnsureCanvasServices(Canvas canvas)
    {
        if (!canvas.TryGetComponent(out UiCancelRouter _))
            Undo.AddComponent<UiCancelRouter>(canvas.gameObject);
        if (!canvas.TryGetComponent(out UiContextMenuCancelConsumer _))
            Undo.AddComponent<UiContextMenuCancelConsumer>(canvas.gameObject);
    }

    static void MergeLocalizationKeys()
    {
        LocalizationTable table =
            AssetDatabase.LoadAssetAtPath<LocalizationTable>(LocalizationTable.AssetPath);
        if (table == null)
        {
            Debug.LogError(
                "[SettingsUISetupMenu] UI_ko table missing. " +
                "Run " + DistMcpMenus.LocalizationSelectOrCreateUiKo + ".");
            return;
        }

        var map = new Dictionary<string, string>(System.StringComparer.Ordinal);
        for (int i = 0; i < table.Entries.Count; i++)
        {
            LocalizationTable.Entry e = table.Entries[i];
            if (e != null && !string.IsNullOrEmpty(e.key))
                map[e.key] = e.text ?? string.Empty;
        }

        void Put(string key, string text)
        {
            if (!map.ContainsKey(key))
                map[key] = text;
        }

        Put("Settings.WindowTitle", "설정");
        Put("Settings.Category.Graphics", "그래픽");
        Put("Settings.HudLayoutAdjust", "HUD 조정");
        Put("Settings.Hud.Time", "시계");
        Put("Settings.Hud.TimeScale", "배속");
        Put("Settings.Hud.MessageLog", "메시지 로그");
        Put("Settings.Hud.Summary", "상태 요약");
        Put("TimeScale.Pause", "||");
        Put("TimeScale.Normal", "1x");
        Put("TimeScale.Double", "2x");
        Put("TimeScale.Smart", "Auto");

        var list = new List<LocalizationTable.Entry>(map.Count);
        foreach (KeyValuePair<string, string> kv in map)
            list.Add(new LocalizationTable.Entry { key = kv.Key, text = kv.Value });

        table.EditorSetEntries(list);
        EditorUtility.SetDirty(table);
        AssetDatabase.SaveAssets();
        Debug.Log($"[SettingsUISetupMenu] Localization merged ({map.Count} keys).");
    }

    static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Dist/Visual/Prefabs/UIComponents"))
            AssetDatabase.CreateFolder("Assets/Dist/Visual/Prefabs", "UIComponents");
        if (!AssetDatabase.IsValidFolder(PrefabFolder))
            AssetDatabase.CreateFolder("Assets/Dist/Visual/Prefabs/UIComponents", "Settings");
    }

    static Canvas ResolveUiCanvas()
    {
        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas c = canvases[i];
            if (c != null && c.GetComponent<UICanvasLayerHost>() != null)
                return c;
        }

        return Object.FindAnyObjectByType<Canvas>();
    }
}
#endif
