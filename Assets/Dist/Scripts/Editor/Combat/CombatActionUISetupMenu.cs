// ============================================================
// CombatActionUISetupMenu — 전투 액션 HUD 씬 배선 (프리팹 로드만)
// ============================================================

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

static class CombatActionUISetupMenu
{
    const string PrefabFolder = "Assets/Dist/Visual/Prefabs/UIComponents/Combat";
    const string DisplayPrefabPath = PrefabFolder + "/Hud_CombatAction.prefab";

    [MenuItem("Dist/Combat/Setup Combat Action HUD In Open Scene")]
    static void SetupCanvasInOpenScene()
    {
        Canvas canvas = ResolveUiCanvas();
        if (canvas == null)
        {
            Debug.LogError("[CombatActionUISetupMenu] Canvas not found.");
            return;
        }

        UICanvasLayerHost layerHost = canvas.GetComponent<UICanvasLayerHost>();
        if (layerHost == null)
            layerHost = Undo.AddComponent<UICanvasLayerHost>(canvas.gameObject);
        layerHost.EditorSetupLayerHierarchy();

        InputManager inputManager = Object.FindAnyObjectByType<InputManager>();
        Transform systemRoot = inputManager != null ? inputManager.transform.parent : null;
        if (systemRoot == null)
        {
            Debug.LogError("[CombatActionUISetupMenu] InputManager parent (System root) not found.");
            return;
        }

        CombatActionUIBridge bridge = canvas.GetComponent<CombatActionUIBridge>();
        if (bridge == null)
            bridge = Undo.AddComponent<CombatActionUIBridge>(canvas.gameObject);

        UICombatActionController controller =
            Object.FindAnyObjectByType<UICombatActionController>();
        if (controller == null)
        {
            GameObject go = new("CombatActionDisplayController");
            Undo.RegisterCreatedObjectUndo(go, "Create CombatActionDisplayController");
            go.transform.SetParent(systemRoot, false);
            controller = Undo.AddComponent<UICombatActionController>(go);
        }

        UICombatActionPanel prefab =
            AssetDatabase.LoadAssetAtPath<UICombatActionPanel>(DisplayPrefabPath);
        if (prefab == null)
        {
            Debug.LogError(
                $"[CombatActionUISetupMenu] Prefab missing: {DisplayPrefabPath}. " +
                "Hand-author or restore the prefab — do not full-bake over layout.");
            return;
        }

        UICombatActionPanel panel = EnsureHudPrefabInstance(layerHost, prefab, "Hud_CombatAction");
        if (panel == null)
            return;

        SerializedObject so = new(controller);
        so.FindProperty("_panel").objectReferenceValue = panel;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[CombatActionUISetupMenu] Combat action HUD wired.", panel);
    }

    static UICombatActionPanel EnsureHudPrefabInstance(
        UICanvasLayerHost layerHost,
        UICombatActionPanel prefab,
        string instanceName)
    {
        Transform hudRoot = layerHost.GetLayerRoot(UICanvasLayer.HUD);
        if (hudRoot == null)
        {
            Debug.LogError("[CombatActionUISetupMenu] Hud layer root missing.");
            return null;
        }

        Transform existing = hudRoot.Find(instanceName);
        if (existing != null)
        {
            UICombatActionPanel panel = existing.GetComponent<UICombatActionPanel>();
            if (panel != null)
                return panel;
            Debug.LogError(
                $"[CombatActionUISetupMenu] {instanceName} exists without UICombatActionPanel.",
                existing);
            return null;
        }

        UICombatActionPanel instance = (UICombatActionPanel)PrefabUtility.InstantiatePrefab(
            prefab,
            hudRoot);
        instance.name = instanceName;
        Undo.RegisterCreatedObjectUndo(instance.gameObject, "Instantiate Combat Action HUD");
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
}
#endif
