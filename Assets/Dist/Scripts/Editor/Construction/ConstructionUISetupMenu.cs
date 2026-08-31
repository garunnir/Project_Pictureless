// ============================================================
// ConstructionUISetupMenu — Dist/MCP 본편 건설 Setup (로드·배선)
// ============================================================

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using IsoTilemap;

static class ConstructionUISetupMenu
{
    const string PrefabFolder = "Assets/Dist/Visual/Prefabs/UIComponents/Construction";
    const string WindowPrefabPath = PrefabFolder + "/Wnd_Construction.prefab";
    const string LauncherButtonName = "Btn_ConstructionLauncher";
    const string ControllerObjectName = "ConstructionController";
    const string GridCursorObjectName = "GridCursorHost";

    [MenuItem(DistMcpMenus.ConstructionCreatePrefabIfMissing)]
    static void CreatePrefabIfMissing()
    {
        EnsureFolder();
        UIConstructionWindow existing =
            AssetDatabase.LoadAssetAtPath<UIConstructionWindow>(WindowPrefabPath);
        if (existing != null)
        {
            Debug.Log($"[ConstructionUISetupMenu] Prefab already exists: {WindowPrefabPath}", existing);
            Selection.activeObject = existing;
            return;
        }

        UIConstructionWindow window = ConstructionUIFactory.CreateWindowRoot();
        GameObject root = window.gameObject;
        // Row prefab must be part of asset — nest inactive under window before save
        UIConstructionRecipeRow row = root.GetComponentInChildren<UIConstructionRecipeRow>(true);
        PrefabUtility.SaveAsPrefabAsset(root, WindowPrefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[ConstructionUISetupMenu] Created prefab: {WindowPrefabPath}");
        Selection.activeObject =
            AssetDatabase.LoadAssetAtPath<UIConstructionWindow>(WindowPrefabPath);
    }

    [MenuItem(DistMcpMenus.ConstructionSetupCanvas)]
    static void SetupCanvasInOpenScene()
    {
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[ConstructionUISetupMenu] Canvas not found.");
            return;
        }

        UICanvasLayerHost layerHost = canvas.GetComponent<UICanvasLayerHost>();
        if (layerHost == null)
            layerHost = Undo.AddComponent<UICanvasLayerHost>(canvas.gameObject);
        layerHost.EditorSetupLayerHierarchy();

        Transform systemRoot = SystemHierarchySetup.ResolveSystemRoot();
        if (systemRoot == null)
        {
            Debug.LogError("[ConstructionUISetupMenu] InputManager parent (System root) not found.");
            return;
        }

        Transform constructionRoot = SystemHierarchySetup.EnsureCategory(
            systemRoot,
            SystemHierarchySetup.Construction);

        UIConstructionWindow prefab =
            AssetDatabase.LoadAssetAtPath<UIConstructionWindow>(WindowPrefabPath);
        if (prefab == null)
        {
            Debug.LogError(
                $"[ConstructionUISetupMenu] Prefab missing: {WindowPrefabPath}. " +
                "Run " + DistMcpMenus.ConstructionCreatePrefabIfMissing + " first.");
            return;
        }

        UIConstructionController controller =
            Object.FindAnyObjectByType<UIConstructionController>();
        if (controller == null)
        {
            GameObject go = new(ControllerObjectName);
            Undo.RegisterCreatedObjectUndo(go, "Create ConstructionController");
            go.transform.SetParent(constructionRoot, false);
            controller = Undo.AddComponent<UIConstructionController>(go);
        }
        else
        {
            SystemHierarchySetup.EnsureChildUnder(
                constructionRoot,
                controller.transform,
                "Move ConstructionController Under System/Construction");
        }

        SerializedObject so = new SerializedObject(controller);
        so.FindProperty("_windowPrefab").objectReferenceValue = prefab;
        so.FindProperty("_uiCanvas").objectReferenceValue = canvas;
        so.FindProperty("_layerHost").objectReferenceValue = layerHost;
        so.ApplyModifiedPropertiesWithoutUndo();

        ConstructionWindowLauncher launcher = EnsureHudLauncher(layerHost, controller);
        so = new SerializedObject(controller);
        so.FindProperty("_launcher").objectReferenceValue = launcher;
        so.ApplyModifiedPropertiesWithoutUndo();
        launcher.Bind(controller);

        EnsureGridCursorHost();

        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("[ConstructionUISetupMenu] Controller + HUD launcher + GridCursor wired.", controller);
    }

    [MenuItem(DistMcpMenus.ConstructionFixRuntimeEditor)]
    static void FixRuntimeEditorInOpenScene()
    {
        UIConstruction ui = Object.FindAnyObjectByType<UIConstruction>(FindObjectsInactive.Include);
        GridCursor cursor = Object.FindAnyObjectByType<GridCursor>(FindObjectsInactive.Include);
        TileMapManager map = Object.FindAnyObjectByType<TileMapManager>();
        TileMapController controller = Object.FindAnyObjectByType<TileMapController>();

        if (cursor == null)
        {
            Debug.LogError("[ConstructionUISetupMenu] GridCursor not found (create via Construction Setup).");
            return;
        }

        SerializedObject so = new SerializedObject(cursor);
        if (so.FindProperty("_tileMapManager") != null)
            so.FindProperty("_tileMapManager").objectReferenceValue = map;
        if (so.FindProperty("_controller") != null)
            so.FindProperty("_controller").objectReferenceValue = controller;
        so.ApplyModifiedPropertiesWithoutUndo();

        if (ui != null)
        {
            SerializedObject uiso = new SerializedObject(ui);
            if (uiso.FindProperty("_tileManager") != null)
                uiso.FindProperty("_tileManager").objectReferenceValue = map;
            if (uiso.FindProperty("_gridCursor") != null)
                uiso.FindProperty("_gridCursor").objectReferenceValue = cursor;
            uiso.ApplyModifiedPropertiesWithoutUndo();
        }

        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("[ConstructionUISetupMenu] Runtime editor TileMap refs wired.", cursor);
    }

    static ConstructionWindowLauncher EnsureHudLauncher(
        UICanvasLayerHost layerHost,
        UIConstructionController controller)
    {
        Transform hud = layerHost.GetLayerRoot(UICanvasLayer.HUD);
        Transform existing = hud != null ? hud.Find(LauncherButtonName) : null;
        GameObject buttonGo;
        if (existing != null)
        {
            buttonGo = existing.gameObject;
        }
        else
        {
            buttonGo = new GameObject(LauncherButtonName, typeof(RectTransform), typeof(Image), typeof(Button));
            Undo.RegisterCreatedObjectUndo(buttonGo, "Create Construction Launcher");
            buttonGo.transform.SetParent(hud, false);
            var rt = buttonGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(1f, 0f);
            rt.anchoredPosition = new Vector2(-24f, 120f);
            rt.sizeDelta = new Vector2(48f, 48f);
            buttonGo.GetComponent<Image>().color = new Color(0.35f, 0.55f, 0.4f, 0.9f);
        }

        ConstructionWindowLauncher launcher = buttonGo.GetComponent<ConstructionWindowLauncher>();
        if (launcher == null)
            launcher = Undo.AddComponent<ConstructionWindowLauncher>(buttonGo);

        SerializedObject so = new SerializedObject(launcher);
        so.FindProperty("_controller").objectReferenceValue = controller;
        so.FindProperty("_button").objectReferenceValue = buttonGo.GetComponent<Button>();
        so.FindProperty("_iconImage").objectReferenceValue = buttonGo.GetComponent<Image>();
        so.ApplyModifiedPropertiesWithoutUndo();
        return launcher;
    }

    static void EnsureGridCursorHost()
    {
        GridCursor existing = Object.FindAnyObjectByType<GridCursor>(FindObjectsInactive.Include);
        if (existing != null)
        {
            WireGridCursor(existing);
            return;
        }

        GameObject go = new(GridCursorObjectName);
        Undo.RegisterCreatedObjectUndo(go, "Create GridCursorHost");
        TilePlacementState state = go.AddComponent<TilePlacementState>();
        GridCursor cursor = go.AddComponent<GridCursor>();

        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visual.name = "CursorVisual";
        visual.transform.SetParent(go.transform, false);
        visual.transform.localScale = new Vector3(0.95f, 0.08f, 0.95f);
        Object.DestroyImmediate(visual.GetComponent<Collider>());
        visual.SetActive(false);

        SerializedObject so = new SerializedObject(cursor);
        so.FindProperty("_placementState").objectReferenceValue = state;
        so.FindProperty("_cursorVisual").objectReferenceValue = visual;
        so.ApplyModifiedPropertiesWithoutUndo();
        WireGridCursor(cursor);
    }

    static void WireGridCursor(GridCursor cursor)
    {
        TileMapManager map = Object.FindAnyObjectByType<TileMapManager>();
        TileMapController controller = Object.FindAnyObjectByType<TileMapController>();
        SerializedObject so = new SerializedObject(cursor);
        so.FindProperty("_tileMapManager").objectReferenceValue = map;
        so.FindProperty("_controller").objectReferenceValue = controller;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Dist/Visual/Prefabs/UIComponents"))
            AssetDatabase.CreateFolder("Assets/Dist/Visual/Prefabs", "UIComponents");
        if (!AssetDatabase.IsValidFolder(PrefabFolder))
            AssetDatabase.CreateFolder("Assets/Dist/Visual/Prefabs/UIComponents", "Construction");
    }
}
#endif
