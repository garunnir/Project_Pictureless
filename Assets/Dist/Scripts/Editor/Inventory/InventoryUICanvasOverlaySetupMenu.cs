// ============================================================
// InventoryUICanvasOverlaySetupMenu — UICanvas 드래그 고스트·오버레이 배선
// ============================================================

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

static class InventoryUICanvasOverlaySetupMenu
{
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

        UIInventoryDragGhost dragGhost = EnsureDragGhost(canvas, serializedController);
        GameObject scrollOverlay = EnsureScrollOverlay(canvas, serializedController);

        serializedController.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        Debug.Log(
            $"[InventoryUICanvasOverlaySetupMenu] Wired canvas overlays on '{canvas.name}' for '{controller.name}'. " +
            $"dragGhost={(dragGhost != null ? dragGhost.name : "missing")}, overlay={(scrollOverlay != null ? scrollOverlay.name : "missing")}.",
            controller);
    }

    static UIInventoryDragGhost EnsureDragGhost(Canvas canvas, SerializedObject serializedController)
    {
        SerializedProperty dragGhostProperty = serializedController.FindProperty("_dragGhost");
        UIInventoryDragGhost dragGhost = dragGhostProperty.objectReferenceValue as UIInventoryDragGhost;

        if (dragGhost == null)
        {
            Transform existing = canvas.transform.Find("InventoryDragGhost");
            if (existing != null)
                existing.TryGetComponent(out dragGhost);
        }

        if (dragGhost == null)
        {
            dragGhost = InventoryUIHierarchyBuilder.BuildDragGhostRoot(canvas);
            dragGhost.transform.SetParent(canvas.transform, false);
            dragGhost.transform.SetAsLastSibling();
            Undo.RegisterCreatedObjectUndo(dragGhost.gameObject, "Create Inventory Drag Ghost");
        }

        dragGhostProperty.objectReferenceValue = dragGhost;
        return dragGhost;
    }

    static GameObject EnsureScrollOverlay(Canvas canvas, SerializedObject serializedController)
    {
        SerializedProperty overlayProperty = serializedController.FindProperty("_scrollDragOverlay");
        GameObject overlay = overlayProperty.objectReferenceValue as GameObject;

        if (overlay == null)
        {
            Transform existing = canvas.transform.Find("InventoryScrollDragOverlay");
            if (existing != null)
                overlay = existing.gameObject;
        }

        if (overlay == null)
        {
            overlay = InventoryUIHierarchyBuilder.BuildScrollDragOverlayRoot();
            overlay.transform.SetParent(canvas.transform, false);
            overlay.transform.SetAsLastSibling();
            Undo.RegisterCreatedObjectUndo(overlay, "Create Inventory Scroll Drag Overlay");
        }

        overlayProperty.objectReferenceValue = overlay;
        return overlay;
    }
}
#endif
