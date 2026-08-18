// ============================================================
// CraftingUISetupMenu — Dist/MCP 제작 창 Setup (로드만, bake 금지)
// ============================================================

#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

static class CraftingUISetupMenu
{
    const string LauncherGroupName = "Grp_InventoryLaunchers";
    const string LauncherButtonName = "Btn_CraftingLauncher";
    const string ControllerObjectName = "CraftingController";
    const string WindowPrefabPath =
        "Assets/Dist/Visual/Prefabs/UIComponents/Crafting/Grp_CraftingWindow.prefab";

    [MenuItem(DistMcpMenus.CraftingSetupCanvas)]
    static void SetupCanvasInOpenScene()
    {
        Canvas canvas = ResolveUiCanvas();
        if (canvas == null)
        {
            Debug.LogError("[CraftingUISetupMenu] Canvas not found.");
            return;
        }

        UICanvasLayerHost layerHost = canvas.GetComponent<UICanvasLayerHost>();
        if (layerHost == null)
            layerHost = Undo.AddComponent<UICanvasLayerHost>(canvas.gameObject);
        layerHost.EditorSetupLayerHierarchy();

        Transform systemRoot = SystemHierarchySetup.ResolveSystemRoot();
        if (systemRoot == null)
        {
            Debug.LogError("[CraftingUISetupMenu] InputManager parent (System root) not found.");
            return;
        }

        Transform craftingRoot = SystemHierarchySetup.EnsureCategory(
            systemRoot,
            SystemHierarchySetup.Crafting);

        UICraftingWindow prefab =
            AssetDatabase.LoadAssetAtPath<UICraftingWindow>(WindowPrefabPath);
        if (prefab == null)
        {
            Debug.LogError(
                $"[CraftingUISetupMenu] Prefab missing: {WindowPrefabPath}. Setup does not bake.");
            return;
        }

        UICraftingController controller = Object.FindAnyObjectByType<UICraftingController>();
        if (controller == null)
        {
            GameObject go = new(ControllerObjectName);
            Undo.RegisterCreatedObjectUndo(go, "Create CraftingController");
            go.transform.SetParent(craftingRoot, false);
            controller = Undo.AddComponent<UICraftingController>(go);
        }
        else
        {
            SystemHierarchySetup.EnsureChildUnder(
                craftingRoot,
                controller.transform,
                "Move CraftingController Under System/Crafting");
        }

        SerializedObject so = new(controller);
        so.FindProperty("_uiCanvas").objectReferenceValue = canvas;
        so.FindProperty("_layerHost").objectReferenceValue = layerHost;
        so.FindProperty("_windowPrefab").objectReferenceValue = prefab;
        so.FindProperty("_window").objectReferenceValue = null;

        CraftingWindowLauncher launcher = EnsureHudLauncher(layerHost, controller);
        so.FindProperty("_launcher").objectReferenceValue = launcher;
        so.ApplyModifiedPropertiesWithoutUndo();

        MergeLocalizationKeys();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[CraftingUISetupMenu] Controller + HUD launcher wired.", controller);
    }

    [MenuItem(DistMcpMenus.CraftingMergeLocalizationKeys)]
    static void MergeLocalizationKeysMenu() => MergeLocalizationKeys();

    static CraftingWindowLauncher EnsureHudLauncher(
        UICanvasLayerHost layerHost,
        UICraftingController controller)
    {
        Transform hud = layerHost.GetLayerRoot(UICanvasLayer.HUD);
        Transform group = FindNamed(hud, LauncherGroupName);
        if (group == null)
        {
            GameObject groupGo = new(LauncherGroupName, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(groupGo, "Create inventory launchers group");
            groupGo.transform.SetParent(hud, false);
            groupGo.layer = LayerMask.NameToLayer("UI");
            RectTransform groupRt = groupGo.GetComponent<RectTransform>();
            groupRt.anchorMin = new Vector2(0f, 1f);
            groupRt.anchorMax = new Vector2(0f, 1f);
            groupRt.pivot = new Vector2(0f, 1f);
            groupRt.anchoredPosition = new Vector2(12f, -12f);
            groupRt.sizeDelta = new Vector2(200f, 40f);
            HorizontalLayoutGroup hlg = groupGo.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8f;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            group = groupGo.transform;
        }

        Transform existing = group.Find(LauncherButtonName);
        GameObject buttonGo;
        if (existing != null)
        {
            buttonGo = existing.gameObject;
        }
        else
        {
            buttonGo = new GameObject(
                LauncherButtonName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));
            Undo.RegisterCreatedObjectUndo(buttonGo, "Create crafting launcher");
            buttonGo.transform.SetParent(group, false);
            buttonGo.layer = LayerMask.NameToLayer("UI");
            buttonGo.GetComponent<Image>().color = new Color(0.9f, 0.75f, 0.45f, 0.85f);
            LayoutElement le = buttonGo.AddComponent<LayoutElement>();
            le.minWidth = 40f;
            le.minHeight = 40f;
            le.preferredWidth = 40f;
            le.preferredHeight = 40f;
        }

        CraftingWindowLauncher launcher = buttonGo.GetComponent<CraftingWindowLauncher>();
        if (launcher == null)
            launcher = Undo.AddComponent<CraftingWindowLauncher>(buttonGo);

        SerializedObject launcherSo = new(launcher);
        launcherSo.FindProperty("_controller").objectReferenceValue = controller;
        launcherSo.FindProperty("_button").objectReferenceValue = buttonGo.GetComponent<Button>();
        launcherSo.FindProperty("_iconImage").objectReferenceValue = buttonGo.GetComponent<Image>();
        launcherSo.ApplyModifiedPropertiesWithoutUndo();
        return launcher;
    }

    static Transform FindNamed(Transform root, string name)
    {
        if (root == null)
            return null;

        Transform direct = root.Find(name);
        if (direct != null)
            return direct;

        Transform[] all = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != null && all[i].name == name)
                return all[i];
        }

        return null;
    }

    static void MergeLocalizationKeys()
    {
        LocalizationTable table =
            AssetDatabase.LoadAssetAtPath<LocalizationTable>(LocalizationTable.AssetPath);
        if (table == null)
        {
            Debug.LogError(
                "[CraftingUISetupMenu] UI_ko table missing. Run " +
                DistMcpMenus.LocalizationSelectOrCreateUiKo + ".");
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

        Put("Crafting.Title", "제작");
        Put("Crafting.TitleOn", "제작 — {0}");
        Put("Crafting.SearchPlaceholder", "검색");
        Put("Crafting.Craft", "제작");
        Put("Crafting.All", "전체");
        Put("Crafting.Favourites", "즐겨찾기");
        Put("Crafting.RequiredItems", "필요 재료");
        Put("Crafting.Outputs", "결과");
        Put("Crafting.TimeRequired", "소요 시간: {0}");
        Put("Crafting.TimeRemaining", "남은 시간: {0}");
        Put("Crafting.DurationFormat", "{0}m {1}s");
        Put("Crafting.Max", "MAX");
        Put("Crafting.OutputCountFormat", "{0}");
        Put("Crafting.SkillLine", "{0}  {1}/{2}");
        Put("Crafting.TimeMinutes", "{0}분");
        Put("Crafting.BookKnown", "레시피 습득");
        Put("Crafting.CannotCraft", "재료·도구·스킬이 부족합니다");
        Put("Crafting.QualityFormat", "{0} lv.{1}");
        Put("Crafting.QualityCountFormat", "{0}/lv.{1}");
        Put("Crafting.QualityAltFormat", "{0} lv.{1}");
        Put("Crafting.CountFormat", "{0}/{1}");

        var list = new List<LocalizationTable.Entry>(map.Count);
        foreach (KeyValuePair<string, string> kv in map)
            list.Add(new LocalizationTable.Entry { key = kv.Key, text = kv.Value });

        table.EditorSetEntries(list);
        EditorUtility.SetDirty(table);
        AssetDatabase.SaveAssets();
        Debug.Log($"[CraftingUISetupMenu] Localization merged ({map.Count} keys).");
    }

    static Canvas ResolveUiCanvas()
    {
        PlayerStatusUIBridge bridge = Object.FindAnyObjectByType<PlayerStatusUIBridge>();
        if (bridge != null)
        {
            Canvas fromBridge = bridge.GetComponent<Canvas>();
            if (fromBridge != null)
                return fromBridge;
        }

        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];
            if (canvas == null)
                continue;
            if (canvas.renderMode == RenderMode.WorldSpace)
                continue;
            if (canvas.GetComponent<UICanvasLayerHost>() == null)
                continue;
            if (canvas.name.IndexOf("Debug", System.StringComparison.OrdinalIgnoreCase) >= 0)
                continue;
            return canvas;
        }

        return null;
    }
}
#endif
