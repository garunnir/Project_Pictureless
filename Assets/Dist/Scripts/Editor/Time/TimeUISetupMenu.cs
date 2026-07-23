// ============================================================
// TimeUISetupMenu — 시계 HUD 씬 배선·Play 검증 메뉴
// ============================================================

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

static class TimeUISetupMenu
{
    const string PrefabFolder = "Assets/Dist/Visual/Prefabs/UIComponents/Time";
    const string DisplayPrefabPath = PrefabFolder + "/Grp_TimeDisplay.prefab";
    const string SoGameplayFolder = "Assets/Dist/SOData/Gameplay";
    const string SoTimeFolder = SoGameplayFolder + "/Time";
    const string SettingsAssetPath = SoTimeFolder + "/WorldClockSettings.asset";
    const string PauseTestKey = "verify_world_pause";

    [MenuItem("Dist/Time/Ensure World Clock Settings Asset")]
    static void EnsureSettingsAssetMenu()
    {
        WorldClockSettings settings = EnsureSettingsAsset();
        Debug.Log($"[TimeUISetupMenu] Settings ready: {AssetDatabase.GetAssetPath(settings)}", settings);
    }

    [MenuItem("Dist/Time/Setup Canvas In Open Scene")]
    static void SetupCanvasInOpenScene()
    {
        Canvas canvas = ResolveUiCanvas();
        if (canvas == null)
        {
            Debug.LogError("[TimeUISetupMenu] Canvas not found.");
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
            Debug.LogError("[TimeUISetupMenu] InputManager parent (System root) not found.");
            return;
        }

        WorldClockSettings settings = EnsureSettingsAsset();
        TimeScaleService scaleService = EnsureComponentOnChild<TimeScaleService>(
            systemRoot,
            "TimeScaleService");
        WorldClock clock = EnsureComponentOnChild<WorldClock>(systemRoot, "WorldClock");

        SerializedObject clockSo = new(clock);
        clockSo.FindProperty("_settings").objectReferenceValue = settings;
        clockSo.ApplyModifiedPropertiesWithoutUndo();

        TimeUIBridge bridge = canvas.GetComponent<TimeUIBridge>();
        if (bridge == null)
            bridge = Undo.AddComponent<TimeUIBridge>(canvas.gameObject);

        UITimeDisplayController controller =
            Object.FindAnyObjectByType<UITimeDisplayController>();
        if (controller == null)
        {
            GameObject go = new("TimeDisplayController");
            Undo.RegisterCreatedObjectUndo(go, "Create TimeDisplayController");
            go.transform.SetParent(systemRoot, false);
            controller = Undo.AddComponent<UITimeDisplayController>(go);
        }
        else if (controller.transform.parent != systemRoot)
        {
            Undo.SetTransformParent(
                controller.transform,
                systemRoot,
                "Move TimeDisplayController Under System");
            controller.transform.localPosition = Vector3.zero;
            controller.transform.localRotation = Quaternion.identity;
            controller.transform.localScale = Vector3.one;
        }

        UITimeDisplayPanel prefab =
            AssetDatabase.LoadAssetAtPath<UITimeDisplayPanel>(DisplayPrefabPath);
        if (prefab == null)
        {
            Debug.LogError(
                $"[TimeUISetupMenu] Prefab missing: {DisplayPrefabPath}. " +
                "Hand-author or restore the prefab — do not full-bake over layout.");
            return;
        }

        SerializedObject controllerSo = new(controller);
        controllerSo.FindProperty("_panelPrefab").objectReferenceValue = prefab;
        controllerSo.FindProperty("_uiCanvas").objectReferenceValue = canvas;
        controllerSo.FindProperty("_layerHost").objectReferenceValue = layerHost;
        controllerSo.FindProperty("_panel").objectReferenceValue = null;
        controllerSo.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log(
            "[TimeUISetupMenu] TimeScaleService + WorldClock + Time HUD wired.",
            controller);
        _ = scaleService;
        _ = bridge;
    }

    [MenuItem("Dist/Time/Verify Clock Advance (Play Mode)")]
    static void VerifyClockAdvance()
    {
        if (!EditorApplication.isPlaying)
        {
            Debug.LogError("[TimeUISetupMenu] Verification requires Play Mode.");
            return;
        }

        WorldClock clock = WorldClock.Instance;
        TimeScaleService scales = TimeScaleService.Instance;
        if (clock == null || scales == null)
        {
            Debug.LogError(
                "[TimeUISetupMenu] WorldClock or TimeScaleService missing. " +
                "Run Dist/Time/Setup Canvas In Open Scene.");
            return;
        }

        scales.Pop(PauseTestKey);
        scales.Pop("verify_ff");

        var host = new GameObject("TimeVerifyRunner");
        host.hideFlags = HideFlags.HideAndDontSave;
        host.AddComponent<TimeVerifyRunner>().Run(clock, scales, PauseTestKey);
    }

    [MenuItem("Dist/Time/Verify Channel Math (Edit Mode)")]
    static void VerifyChannelMathEditMode()
    {
        WorldClockSettings settings = ScriptableObject.CreateInstance<WorldClockSettings>();
        try
        {
            bool nightWrap = settings.ResolvePeriod(22 * 60) == DayPeriod.Night &&
                             settings.ResolvePeriod(2 * 60) == DayPeriod.Night;
            bool dawn = settings.ResolvePeriod(5 * 60) == DayPeriod.Dawn;
            bool day = settings.ResolvePeriod(12 * 60) == DayPeriod.Day;
            bool dusk = settings.ResolvePeriod(18 * 60) == DayPeriod.Dusk;
            string formatted = TimeDisplayFormat.Format(3, 9, 5);
            bool formatOk = formatted == "Day 3  09:05";

            bool ok = nightWrap && dawn && day && dusk && formatOk;
            string msg =
                $"[Time] period nightWrap={nightWrap} dawn={dawn} day={day} dusk={dusk} " +
                $"format='{formatted}' => {(ok ? "PASS" : "FAIL")}";
            if (ok)
                Debug.Log(msg);
            else
                Debug.LogError(msg);
        }
        finally
        {
            Object.DestroyImmediate(settings);
        }
    }

    static WorldClockSettings EnsureSettingsAsset()
    {
        EnsureSoFolder();
        WorldClockSettings settings =
            AssetDatabase.LoadAssetAtPath<WorldClockSettings>(SettingsAssetPath);
        if (settings != null)
            return settings;

        settings = ScriptableObject.CreateInstance<WorldClockSettings>();
        AssetDatabase.CreateAsset(settings, SettingsAssetPath);
        AssetDatabase.SaveAssets();
        return settings;
    }

    static T EnsureComponentOnChild<T>(Transform parent, string childName) where T : Component
    {
        T existing = Object.FindAnyObjectByType<T>();
        if (existing != null)
        {
            if (existing.transform.parent != parent)
            {
                Undo.SetTransformParent(existing.transform, parent, $"Move {childName}");
                existing.transform.localPosition = Vector3.zero;
                existing.transform.localRotation = Quaternion.identity;
                existing.transform.localScale = Vector3.one;
            }

            return existing;
        }

        Transform child = parent.Find(childName);
        GameObject go;
        if (child != null)
        {
            go = child.gameObject;
        }
        else
        {
            go = new GameObject(childName);
            Undo.RegisterCreatedObjectUndo(go, $"Create {childName}");
            go.transform.SetParent(parent, false);
        }

        T component = go.GetComponent<T>();
        if (component == null)
            component = Undo.AddComponent<T>(go);
        return component;
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
            Canvas c = canvases[i];
            if (c == null)
                continue;
            if (c.GetComponent<UICanvasLayerHost>() == null)
                continue;
            if (c.name.IndexOf("Debug", System.StringComparison.OrdinalIgnoreCase) >= 0)
                continue;
            return c;
        }

        return Object.FindAnyObjectByType<Canvas>();
    }

    static void EnsureSoFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Dist/SOData"))
            AssetDatabase.CreateFolder("Assets/Dist", "SOData");
        if (!AssetDatabase.IsValidFolder(SoGameplayFolder))
            AssetDatabase.CreateFolder("Assets/Dist/SOData", "Gameplay");
        if (!AssetDatabase.IsValidFolder(SoTimeFolder))
            AssetDatabase.CreateFolder(SoGameplayFolder, "Time");
    }
}

/// <summary>Play Mode only — WaitForSecondsRealtime로 시계 진행/정지를 검증.</summary>
sealed class TimeVerifyRunner : MonoBehaviour
{
    public void Run(WorldClock clock, TimeScaleService scales, string pauseKey)
    {
        StartCoroutine(RunRoutine(clock, scales, pauseKey));
    }

    System.Collections.IEnumerator RunRoutine(
        WorldClock clock,
        TimeScaleService scales,
        string pauseKey)
    {
        scales.Push("verify_ff", TimeScaleChannel.World, 120f);
        int day0 = clock.DayIndex;
        int minute0 = clock.MinuteOfDay;
        yield return new WaitForSecondsRealtime(0.05f);

        bool advanced =
            clock.DayIndex > day0 ||
            clock.MinuteOfDay != minute0;
        scales.Pop("verify_ff");

        scales.Push(pauseKey, TimeScaleChannel.World, 0f);
        int pausedDay = clock.DayIndex;
        int pausedMinute = clock.MinuteOfDay;
        yield return new WaitForSecondsRealtime(0.1f);
        bool pausedHeld =
            clock.DayIndex == pausedDay &&
            clock.MinuteOfDay == pausedMinute;
        scales.Pop(pauseKey);

        scales.Push("bullet", TimeScaleChannel.World, 0.25f);
        scales.Push("bullet", TimeScaleChannel.Player, 1f);
        bool bulletOk =
            Mathf.Approximately(scales.GetScale(TimeScaleChannel.World), 0.25f) &&
            Mathf.Approximately(scales.GetScale(TimeScaleChannel.Player), 1f) &&
            Mathf.Approximately(scales.GetScale(TimeScaleChannel.Realtime), 1f);
        scales.Pop("bullet");

        bool ok = advanced && pausedHeld && bulletOk;
        string msg =
            $"[TimeUISetupMenu] advanced={advanced} pausedHeld={pausedHeld} " +
            $"bulletOk={bulletOk} => {(ok ? "PASS" : "FAIL")}";
        if (ok)
            Debug.Log(msg);
        else
            Debug.LogError(msg);

        Destroy(gameObject);
    }
}
#endif
