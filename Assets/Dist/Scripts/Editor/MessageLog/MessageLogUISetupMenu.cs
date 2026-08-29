// ============================================================
// MessageLogUISetupMenu — Dist/MCP 메시지 로그 Setup·Ensure (에이전트용)
// ============================================================

#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

static class MessageLogUISetupMenu
{
    const string PrefabFolder = "Assets/Dist/Visual/Prefabs/UIComponents/MessageLog";
    const string DisplayPrefabPath = PrefabFolder + "/Hud_MessageLog.prefab";

    [MenuItem(DistMcpMenus.MessageLogCreatePrefabIfMissing)]
    static void CreatePrefabIfMissing()
    {
        EnsureFolder();
        UIMessageLogPanel existing =
            AssetDatabase.LoadAssetAtPath<UIMessageLogPanel>(DisplayPrefabPath);
        if (existing != null)
        {
            Debug.Log(
                $"[MessageLogUISetupMenu] Prefab already exists: {DisplayPrefabPath}",
                existing);
            Selection.activeObject = existing;
            return;
        }

        UIMessageLogPanel panel = MessageLogUIFactory.CreateDisplayRoot();
        GameObject root = panel.gameObject;
        PrefabUtility.SaveAsPrefabAsset(root, DisplayPrefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[MessageLogUISetupMenu] Created prefab: {DisplayPrefabPath}");
        Selection.activeObject =
            AssetDatabase.LoadAssetAtPath<UIMessageLogPanel>(DisplayPrefabPath);
    }

    [MenuItem(DistMcpMenus.MessageLogSetupHud)]
    static void SetupCanvasInOpenScene()
    {
        Canvas canvas = ResolveUiCanvas();
        if (canvas == null)
        {
            Debug.LogError("[MessageLogUISetupMenu] Canvas not found.");
            return;
        }

        UICanvasLayerHost layerHost = canvas.GetComponent<UICanvasLayerHost>();
        if (layerHost == null)
            layerHost = Undo.AddComponent<UICanvasLayerHost>(canvas.gameObject);
        layerHost.EditorSetupLayerHierarchy();

        Transform systemRoot = SystemHierarchySetup.ResolveSystemRoot();
        if (systemRoot == null)
        {
            Debug.LogError("[MessageLogUISetupMenu] InputManager parent (System root) not found.");
            return;
        }

        Transform msgRoot = SystemHierarchySetup.EnsureCategory(
            systemRoot,
            SystemHierarchySetup.Msg);

        MessageLogUIBridge bridge = canvas.GetComponent<MessageLogUIBridge>();
        if (bridge == null)
            bridge = Undo.AddComponent<MessageLogUIBridge>(canvas.gameObject);

        MessageLogPlayerCombatSink sink =
            Object.FindAnyObjectByType<MessageLogPlayerCombatSink>();
        if (sink == null)
        {
            GameObject sinkGo = new("MessageLogPlayerCombatSink");
            Undo.RegisterCreatedObjectUndo(sinkGo, "Create MessageLogPlayerCombatSink");
            sinkGo.transform.SetParent(msgRoot, false);
            Undo.AddComponent<MessageLogPlayerCombatSink>(sinkGo);
        }
        else
        {
            SystemHierarchySetup.EnsureChildUnder(
                msgRoot,
                sink.transform,
                "Move MessageLogPlayerCombatSink Under System/Msg");
        }

        MessageLogPlayerEncumbranceSink encumbranceSink =
            Object.FindAnyObjectByType<MessageLogPlayerEncumbranceSink>();
        if (encumbranceSink == null)
        {
            GameObject encGo = new("MessageLogPlayerEncumbranceSink");
            Undo.RegisterCreatedObjectUndo(encGo, "Create MessageLogPlayerEncumbranceSink");
            encGo.transform.SetParent(msgRoot, false);
            Undo.AddComponent<MessageLogPlayerEncumbranceSink>(encGo);
        }
        else
        {
            SystemHierarchySetup.EnsureChildUnder(
                msgRoot,
                encumbranceSink.transform,
                "Move MessageLogPlayerEncumbranceSink Under System/Msg");
        }

        UIMessageLogController controller =
            Object.FindAnyObjectByType<UIMessageLogController>();
        if (controller == null)
        {
            GameObject go = new("MessageLogDisplayController");
            Undo.RegisterCreatedObjectUndo(go, "Create MessageLogDisplayController");
            go.transform.SetParent(msgRoot, false);
            controller = Undo.AddComponent<UIMessageLogController>(go);
        }
        else
        {
            SystemHierarchySetup.EnsureChildUnder(
                msgRoot,
                controller.transform,
                "Move MessageLogDisplayController Under System/Msg");
        }

        UIMessageLogPanel prefab =
            AssetDatabase.LoadAssetAtPath<UIMessageLogPanel>(DisplayPrefabPath);
        if (prefab == null)
        {
            Debug.LogError(
                $"[MessageLogUISetupMenu] Prefab missing: {DisplayPrefabPath}. " +
                "Run " + DistMcpMenus.MessageLogCreatePrefabIfMissing + " — " +
                "do not full-bake over layout.");
            return;
        }

        UIMessageLogPanel panel = EnsureHudPrefabInstance(layerHost, prefab, "Hud_MessageLog");
        if (panel == null)
            return;

        SerializedObject so = new(controller);
        so.FindProperty("_panel").objectReferenceValue = panel;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[MessageLogUISetupMenu] Message log HUD wired.", panel);
    }

    [MenuItem(DistMcpMenus.MessageLogMergeLocalizationKeys)]
    static void MergeLocalizationKeysMenu() => MergeLocalizationKeys();

    static void MergeLocalizationKeys()
    {
        LocalizationTable table =
            AssetDatabase.LoadAssetAtPath<LocalizationTable>(LocalizationTable.AssetPath);
        if (table == null)
        {
            Debug.LogError(
                "[MessageLogUISetupMenu] UI_ko table missing. " +
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

        Put("msg.combat.player_hit", "{0}에 {1}의 피해를 입었다.");
        Put("msg.combat.surprise_dealt", "기습이 적중했다.");
        Put("msg.combat.surprise_taken", "기습을 당했다.");
        Put("msg.combat.surprise_neck", "목이 노려졌다.");
        Put("msg.combat.surprise_stun", "기습에 정신을 잃었다.");
        Put("msg.status.defeat_body", "치명상을 입고 쓰러졌다.");
        Put("msg.status.defeat_collapse", "정신이 무너져 쓰러졌다.");
        Put("msg.status.encumbrance_immobile", "너무 무거워서 움직일 수 없다.");
        Put("MessageLog.Title", "메시지");

        var list = new List<LocalizationTable.Entry>(map.Count);
        foreach (KeyValuePair<string, string> kv in map)
            list.Add(new LocalizationTable.Entry { key = kv.Key, text = kv.Value });

        table.EditorSetEntries(list);
        EditorUtility.SetDirty(table);
        AssetDatabase.SaveAssets();
        Debug.Log($"[MessageLogUISetupMenu] Localization merged ({map.Count} keys).");
    }

    static UIMessageLogPanel EnsureHudPrefabInstance(
        UICanvasLayerHost layerHost,
        UIMessageLogPanel prefab,
        string instanceName)
    {
        Transform hudRoot = layerHost.GetLayerRoot(UICanvasLayer.HUD);
        if (hudRoot == null)
        {
            Debug.LogError("[MessageLogUISetupMenu] Hud layer root missing.");
            return null;
        }

        Transform existing = hudRoot.Find(instanceName);
        if (existing != null)
        {
            UIMessageLogPanel panel = existing.GetComponent<UIMessageLogPanel>();
            if (panel != null)
                return panel;
            Debug.LogError(
                $"[MessageLogUISetupMenu] {instanceName} exists without UIMessageLogPanel.",
                existing);
            return null;
        }

        UIMessageLogPanel instance = (UIMessageLogPanel)PrefabUtility.InstantiatePrefab(
            prefab,
            hudRoot);
        instance.name = instanceName;
        Undo.RegisterCreatedObjectUndo(instance.gameObject, "Instantiate Message Log HUD");
        return instance;
    }

    static Canvas ResolveUiCanvas()
    {
        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];
            if (canvas != null && canvas.renderMode != RenderMode.WorldSpace)
                return canvas;
        }

        return null;
    }

    static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Dist/Visual/Prefabs/UIComponents"))
            AssetDatabase.CreateFolder("Assets/Dist/Visual/Prefabs", "UIComponents");
        if (!AssetDatabase.IsValidFolder(PrefabFolder))
            AssetDatabase.CreateFolder("Assets/Dist/Visual/Prefabs/UIComponents", "MessageLog");
    }
}
#endif
