// ============================================================
// InventoryUICanvasOverlaySetupMenu — Layer 그룹 + 일시 UI 프리팹 배선
// ============================================================

#if UNITY_EDITOR
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
    const string ContextMenuButtonPath = InventoryUIHierarchyBuilder.PrefabFolder + "/ContextMenuButton.prefab";

    [MenuItem("Dist/Inventory/Setup Canvas Overlays In Open Scene")]
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

        SetObjectRef(serializedController, "_dragGhostPrefab", dragGhostPrefab);
        SetObjectRef(serializedController, "_scrollDragOverlayPrefab", scrollOverlayPrefab);
        SetObjectRef(serializedController, "_contextMenuPrefab", contextMenuPrefab);

        // Runtime instances are spawned from prefabs — clear scene instance slots.
        SetObjectRef(serializedController, "_dragGhost", null);
        SetObjectRef(serializedController, "_scrollDragOverlay", null);
        SetObjectRef(serializedController, "_contextMenu", null);

        RemoveSceneEphemeralUi(canvas.transform);

        serializedController.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        Debug.Log(
            $"[InventoryUICanvasOverlaySetupMenu] Wired layer host + ephemeral prefabs on '{canvas.name}' for '{controller.name}'.",
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
        var existing = AssetDatabase.LoadAssetAtPath<UIItemContextMenu>(ContextMenuPrefabPath);
        if (existing != null)
            return existing;

        EnsurePrefabFolder();
        Button buttonPrefab = AssetDatabase.LoadAssetAtPath<Button>(ContextMenuButtonPath);
        UIItemContextMenu built = InventoryUIHierarchyBuilder.BuildContextMenuRoot(buttonPrefab);
        GameObject prefabRoot = PrefabUtility.SaveAsPrefabAsset(built.gameObject, ContextMenuPrefabPath);
        Object.DestroyImmediate(built.gameObject);
        return prefabRoot.GetComponent<UIItemContextMenu>();
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
