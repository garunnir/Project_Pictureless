// ============================================================
// InventoryUICanvasOverlaySetupMenu — Dist/MCP 오버레이 Setup (에이전트용)
// ============================================================

#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

static class InventoryUICanvasOverlaySetupMenu
{
    const string PrefabFolder = InventoryUIHierarchyBuilder.PrefabFolder;
    const string DragGhostPrefabPath = PrefabFolder + "/InventoryDragGhost.prefab";
    const string ScrollOverlayPrefabPath = PrefabFolder + "/InventoryScrollDragOverlay.prefab";
    const string ContextMenuPrefabPath = PrefabFolder + "/ItemContextMenu.prefab";
    const string ItemDetailPanelPrefabPath = PrefabFolder + "/InventoryItemDetailPanel.prefab";
    const string TextHoverPanelPrefabPath =
        "Assets/Dist/Visual/Prefabs/UIComponents/HUD/TextHoverPanel.prefab";
    const string ContextMenuButtonPath = InventoryUIHierarchyBuilder.PrefabFolder + "/ContextMenuButton.prefab";

    [MenuItem(DistMcpMenus.InventorySetupCanvasOverlays)]
    static void SetupCanvasOverlaysInOpenScene()
    {
        UIInventoryController controller = Object.FindAnyObjectByType<UIInventoryController>();
        if (controller == null)
        {
            Debug.LogError("[InventoryUICanvasOverlaySetupMenu] UIInventoryController not found in open scene.");
            return;
        }

        SerializedObject serializedController = new SerializedObject(controller);
        SerializedProperty canvasProperty = serializedController.FindProperty("_uiCanvas");
        Canvas canvas = canvasProperty.objectReferenceValue as Canvas;
        if (canvas == null)
            canvas = Object.FindAnyObjectByType<Canvas>();

        if (canvas == null)
        {
            Debug.LogError("[InventoryUICanvasOverlaySetupMenu] UICanvas not found.");
            return;
        }

        canvasProperty.objectReferenceValue = canvas;

        UICanvasLayerHost layerHost = canvas.GetComponent<UICanvasLayerHost>();
        if (layerHost == null)
            layerHost = Undo.AddComponent<UICanvasLayerHost>(canvas.gameObject);

        layerHost.EditorSetupLayerHierarchy();

        SerializedProperty layerHostProperty = serializedController.FindProperty("_layerHost");
        if (layerHostProperty != null)
            layerHostProperty.objectReferenceValue = layerHost;

        UIInventoryDragGhost dragGhostPrefab = EnsureDragGhostPrefab(canvas);
        GameObject scrollOverlayPrefab = EnsureScrollOverlayPrefab();
        UIItemContextMenu contextMenuPrefab = EnsureContextMenuPrefab();

        UIItemDragGhostService ghostService = canvas.GetComponent<UIItemDragGhostService>();
        if (ghostService == null)
            ghostService = Undo.AddComponent<UIItemDragGhostService>(canvas.gameObject);

        SerializedObject serializedGhost = new SerializedObject(ghostService);
        SetObjectRef(serializedGhost, "_prefab", dragGhostPrefab);
        SetObjectRef(serializedGhost, "_instance", null);
        SetObjectRef(serializedGhost, "_canvas", canvas);
        SetObjectRef(serializedGhost, "_layerHost", layerHost);
        serializedGhost.ApplyModifiedPropertiesWithoutUndo();

        UITextHoverPanel textHoverPrefab = EnsureTextHoverPanelPrefab();
        UITextHoverService textHoverService = canvas.GetComponent<UITextHoverService>();
        if (textHoverService == null)
            textHoverService = Undo.AddComponent<UITextHoverService>(canvas.gameObject);

        SerializedObject serializedTextHover = new SerializedObject(textHoverService);
        SetObjectRef(serializedTextHover, "_prefab", textHoverPrefab);
        SetObjectRef(serializedTextHover, "_instance", null);
        SetObjectRef(serializedTextHover, "_canvas", canvas);
        SetObjectRef(serializedTextHover, "_layerHost", layerHost);
        serializedTextHover.ApplyModifiedPropertiesWithoutUndo();

        UIInventoryItemDetailPanel itemDetailPanelPrefab = EnsureItemDetailPanelPrefab(canvas);

        SetObjectRef(serializedController, "_scrollDragOverlayPrefab", scrollOverlayPrefab);
        SetObjectRef(serializedController, "_contextMenuPrefab", contextMenuPrefab);
        SetObjectRef(serializedController, "_itemDetailPanelPrefab", itemDetailPanelPrefab);

        // Runtime instances are spawned from prefabs — clear scene instance slots.
        SetObjectRef(serializedController, "_scrollDragOverlay", null);
        SetObjectRef(serializedController, "_contextMenu", null);
        SetObjectRef(serializedController, "_itemDetailPanel", null);

        RemoveSceneEphemeralUi(canvas.transform);

        serializedController.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        Debug.Log(
            $"[InventoryUICanvasOverlaySetupMenu] Wired layer host + UIItemDragGhostService + UITextHoverService + ephemeral prefabs on '{canvas.name}' for '{controller.name}'.",
            controller);
    }

    static void SetObjectRef(SerializedObject so, string propertyName, Object value)
    {
        SerializedProperty property = so.FindProperty(propertyName);
        if (property == null)
        {
            Debug.LogError($"[InventoryUICanvasOverlaySetupMenu] Missing property '{propertyName}'.");
            return;
        }

        property.objectReferenceValue = value;
    }

    static UIInventoryDragGhost EnsureDragGhostPrefab(Canvas canvas)
    {
        EnsurePrefabFolder();
        // Rebuild so Image color/sprite match builder (alpha 0 / null sprite hide the ghost).
        UIInventoryDragGhost built = InventoryUIHierarchyBuilder.BuildDragGhostRoot(canvas);
        GameObject prefabRoot = PrefabUtility.SaveAsPrefabAsset(built.gameObject, DragGhostPrefabPath);
        Object.DestroyImmediate(built.gameObject);
        return prefabRoot != null ? prefabRoot.GetComponent<UIInventoryDragGhost>() : null;
    }

    static GameObject EnsureScrollOverlayPrefab()
    {
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(ScrollOverlayPrefabPath);
        if (existing != null)
            return existing;

        EnsurePrefabFolder();
        GameObject built = InventoryUIHierarchyBuilder.BuildScrollDragOverlayRoot();
        GameObject prefabRoot = PrefabUtility.SaveAsPrefabAsset(built, ScrollOverlayPrefabPath);
        Object.DestroyImmediate(built);
        return prefabRoot;
    }

    static UIItemContextMenu EnsureContextMenuPrefab()
    {
        EnsurePrefabFolder();
        // Rebuild so cascade panel/row templates match HierarchyBuilder.
        Button buttonPrefab = AssetDatabase.LoadAssetAtPath<Button>(ContextMenuButtonPath);
        UIItemContextMenu built = InventoryUIHierarchyBuilder.BuildContextMenuRoot(buttonPrefab);
        GameObject prefabRoot = PrefabUtility.SaveAsPrefabAsset(built.gameObject, ContextMenuPrefabPath);
        Object.DestroyImmediate(built.gameObject);
        return prefabRoot != null ? prefabRoot.GetComponent<UIItemContextMenu>() : null;
    }

    static UIInventoryItemDetailPanel EnsureItemDetailPanelPrefab(Canvas canvas)
    {
        UIInventoryItemDetailPanel existing =
            AssetDatabase.LoadAssetAtPath<UIInventoryItemDetailPanel>(ItemDetailPanelPrefabPath);
        if (existing != null && existing.GetComponent<UIHoverPanelShell>() != null)
            return existing;

        EnsurePrefabFolder();
        UIInventoryItemDetailPanel built = InventoryUIHierarchyBuilder.BuildItemDetailPanelRoot(canvas);
        GameObject prefabRoot = PrefabUtility.SaveAsPrefabAsset(built.gameObject, ItemDetailPanelPrefabPath);
        Object.DestroyImmediate(built.gameObject);
        return prefabRoot != null ? prefabRoot.GetComponent<UIInventoryItemDetailPanel>() : null;
    }

    static UITextHoverPanel EnsureTextHoverPanelPrefab()
    {
        string folder = "Assets/Dist/Visual/Prefabs/UIComponents/HUD";
        if (!AssetDatabase.IsValidFolder("Assets/Dist/Visual/Prefabs/UIComponents"))
            AssetDatabase.CreateFolder("Assets/Dist/Visual/Prefabs", "UIComponents");
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets/Dist/Visual/Prefabs/UIComponents", "HUD");

        GameObject root = new GameObject(
            "TextHoverPanel",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        root.layer = LayerMask.NameToLayer("UI");
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0f, 1f);
        rect.sizeDelta = new Vector2(240f, 80f);

        Image bg = root.GetComponent<Image>();
        bg.color = new Color(0.1f, 0.12f, 0.16f, 0.98f);
        bg.raycastTarget = false;

        ContentSizeFitter fitter = root.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        VerticalLayoutGroup layout = root.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(8, 8, 8, 8);
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        GameObject bodyGo = new GameObject("Body", typeof(RectTransform), typeof(CanvasRenderer));
        bodyGo.layer = LayerMask.NameToLayer("UI");
        bodyGo.transform.SetParent(root.transform, false);
        TextMeshProUGUI body = bodyGo.AddComponent<TextMeshProUGUI>();
        body.fontSize = PlayerStatusUIFactory.FontSizeBody;
        body.alignment = TextAlignmentOptions.TopLeft;
        body.enableWordWrapping = true;
        body.overflowMode = TextOverflowModes.Overflow;
        body.raycastTarget = false;
        DistUiFont.Apply(body);
        LayoutElement bodyLe = bodyGo.AddComponent<LayoutElement>();
        bodyLe.preferredWidth = 224f;

        UIHoverPanelShell shell = root.AddComponent<UIHoverPanelShell>();
        UITextHoverPanel panel = root.AddComponent<UITextHoverPanel>();
        panel.Wire(body);

        var shellSo = new SerializedObject(shell);
        shellSo.FindProperty("_rect").objectReferenceValue = rect;
        shellSo.ApplyModifiedPropertiesWithoutUndo();

        var panelSo = new SerializedObject(panel);
        panelSo.FindProperty("_shell").objectReferenceValue = shell;
        panelSo.FindProperty("_rect").objectReferenceValue = rect;
        panelSo.FindProperty("_bodyText").objectReferenceValue = body;
        panelSo.ApplyModifiedPropertiesWithoutUndo();

        GameObject prefabRoot = PrefabUtility.SaveAsPrefabAsset(root, TextHoverPanelPrefabPath);
        Object.DestroyImmediate(root);
        return prefabRoot != null ? prefabRoot.GetComponent<UITextHoverPanel>() : null;
    }

    static void EnsurePrefabFolder()
    {
        if (!AssetDatabase.IsValidFolder(PrefabFolder))
        {
            Debug.LogError($"[InventoryUICanvasOverlaySetupMenu] Prefab folder missing: {PrefabFolder}");
        }
    }

    static void RemoveSceneEphemeralUi(Transform canvasRoot)
    {
        string[] names =
        {
            "InventoryDragGhost",
            "InventoryScrollDragOverlay",
            "ItemContextMenu",
            "InventoryItemDetailPanel",
            "TextHoverPanel",
        };

        for (int n = 0; n < names.Length; n++)
            DestroyNamedRecursive(canvasRoot, names[n]);
    }

    static void DestroyNamedRecursive(Transform root, string objectName)
    {
        // Collect first to avoid modifying hierarchy while iterating.
        var matches = new System.Collections.Generic.List<GameObject>();
        CollectNamed(root, objectName, matches);
        for (int i = 0; i < matches.Count; i++)
        {
            Undo.DestroyObjectImmediate(matches[i]);
            Debug.Log($"[InventoryUICanvasOverlaySetupMenu] Removed scene ephemeral UI '{objectName}'.", root);
        }
    }

    static void CollectNamed(Transform root, string objectName, System.Collections.Generic.List<GameObject> results)
    {
        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name == objectName)
                results.Add(child.gameObject);
            CollectNamed(child, objectName, results);
        }
    }
}
#endif
