// ============================================================
// TimeUISetupMenu — Dist/MCP 시계 HUD Setup·Patch·Ensure (에이전트용)
// ============================================================

#if UNITY_EDITOR
using IsoTilemap;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

static class TimeUISetupMenu
{
    const string PrefabFolder = "Assets/Dist/Visual/Prefabs/UIComponents/Time";
    const string DisplayPrefabPath = PrefabFolder + "/Grp_TimeDisplay.prefab";
    const string SettingsAssetPath = WorldClockSettings.DefaultAssetPath;
    const string WeatherSettingsAssetPath = WorldWeatherSettings.DefaultAssetPath;

    [MenuItem(DistMcpMenus.TimeEnsureWorldClockSettings)]
    static void EnsureSettingsAssetMenu()
    {
        WorldClockSettings settings = EnsureSettingsAsset();
        Debug.Log($"[TimeUISetupMenu] Settings ready: {AssetDatabase.GetAssetPath(settings)}", settings);
    }

    [MenuItem(DistMcpMenus.TimeEnsureWorldWeatherSettings)]
    static void EnsureWeatherSettingsAssetMenu()
    {
        WorldWeatherSettings settings = EnsureWeatherSettingsAsset();
        Debug.Log(
            $"[TimeUISetupMenu] Weather settings ready: {AssetDatabase.GetAssetPath(settings)}",
            settings);
    }

    [MenuItem(DistMcpMenus.TimeEnsureWorldWeatherInOpenScene)]
    static void EnsureWorldWeatherInOpenScene()
    {
        Transform systemRoot = SystemHierarchySetup.ResolveSystemRoot();
        if (systemRoot == null)
        {
            Debug.LogError("[TimeUISetupMenu] InputManager parent (System root) not found.");
            return;
        }

        Transform timeRoot = SystemHierarchySetup.EnsureCategory(
            systemRoot,
            SystemHierarchySetup.Time);
        EnsureWorldWeatherStack(timeRoot);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[TimeUISetupMenu] WorldWeatherHost + WorldEnvironmentPresenter wired.", timeRoot);
    }

    /// <summary>
    /// 구 핸들 자식 제거 후 UIWindowResizeHandles + Proximity 배선.
    /// </summary>
    [MenuItem(DistMcpMenus.TimePatchDisplayResizeHandles)]
    static void PatchDisplayResizeHandles()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(DisplayPrefabPath);
        if (root == null)
        {
            Debug.LogError($"[TimeUISetupMenu] Failed to load: {DisplayPrefabPath}");
            return;
        }

        try
        {
            UIWindowResizeHandles host = UIWindowResizeHandlesPrefabPatch.Apply(
                root,
                TimeUIFactory.ResizeEdgeThickness,
                proximityReveal: true,
                TimeUIFactory.MinPanelSize,
                TimeUIFactory.MaxPanelSize);

            UIWindowResizeProximity proximity = root.GetComponent<UIWindowResizeProximity>();
            if (proximity == null)
                proximity = root.AddComponent<UIWindowResizeProximity>();

            var proxSo = new SerializedObject(proximity);
            proxSo.FindProperty("_enabled").boolValue = true;
            proxSo.FindProperty("_window").objectReferenceValue = root.transform as RectTransform;
            UIWindowDragHandler drag = root.GetComponentInChildren<UIWindowDragHandler>(true);
            if (drag != null)
                proxSo.FindProperty("_dragHeader").objectReferenceValue = drag;
            proxSo.ApplyModifiedPropertiesWithoutUndo();

            UITimeDisplayPanel panel = root.GetComponent<UITimeDisplayPanel>();
            if (panel != null)
            {
                var panelSo = new SerializedObject(panel);
                SerializedProperty handlesProp = panelSo.FindProperty("_resizeHandles");
                if (handlesProp != null)
                    handlesProp.objectReferenceValue = host;
                SerializedProperty proxProp = panelSo.FindProperty("_resizeProximity");
                if (proxProp != null)
                    proxProp.objectReferenceValue = proximity;
                if (drag != null)
                {
                    SerializedProperty dragProp = panelSo.FindProperty("_dragHandler");
                    if (dragProp != null)
                        dragProp.objectReferenceValue = drag;
                }

                panelSo.ApplyModifiedPropertiesWithoutUndo();
            }

            PrefabUtility.SaveAsPrefabAsset(root, DisplayPrefabPath);
            Debug.Log($"[TimeUISetupMenu] Applied UIWindowResizeHandles on {DisplayPrefabPath}.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    [MenuItem(DistMcpMenus.TimeSetupCanvas)]
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

        Transform systemRoot = SystemHierarchySetup.ResolveSystemRoot();
        if (systemRoot == null)
        {
            Debug.LogError("[TimeUISetupMenu] InputManager parent (System root) not found.");
            return;
        }

        Transform timeRoot = SystemHierarchySetup.EnsureCategory(
            systemRoot,
            SystemHierarchySetup.Time);

        WorldClockSettings settings = EnsureSettingsAsset();
        TimeScaleService scaleService = EnsureComponentOnChild<TimeScaleService>(
            timeRoot,
            "TimeScaleService");
        WorldClock clock = EnsureComponentOnChild<WorldClock>(timeRoot, "WorldClock");

        SerializedObject clockSo = new(clock);
        clockSo.FindProperty("_settings").objectReferenceValue = settings;
        clockSo.ApplyModifiedPropertiesWithoutUndo();

        EnsureWorldWeatherStack(timeRoot);

        TimeUIBridge bridge = canvas.GetComponent<TimeUIBridge>();
        if (bridge == null)
            bridge = Undo.AddComponent<TimeUIBridge>(canvas.gameObject);

        UITimeDisplayController controller =
            Object.FindAnyObjectByType<UITimeDisplayController>();
        if (controller == null)
        {
            GameObject go = new("TimeDisplayController");
            Undo.RegisterCreatedObjectUndo(go, "Create TimeDisplayController");
            go.transform.SetParent(timeRoot, false);
            controller = Undo.AddComponent<UITimeDisplayController>(go);
        }
        else
        {
            SystemHierarchySetup.EnsureChildUnder(
                timeRoot,
                controller.transform,
                "Move TimeDisplayController Under System/Time");
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

        UITimeDisplayPanel panel = EnsureHudPrefabInstance(
            layerHost,
            prefab,
            "Grp_TimeDisplay");
        if (panel == null)
            return;

        SerializedObject controllerSo = new(controller);
        controllerSo.FindProperty("_panel").objectReferenceValue = panel;
        controllerSo.FindProperty("_uiCanvas").objectReferenceValue = canvas;
        controllerSo.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log(
            "[TimeUISetupMenu] TimeScaleService + WorldClock + WorldWeather + Time HUD scene instance wired.",
            panel);
        _ = scaleService;
        _ = bridge;
    }

    static void EnsureWorldWeatherStack(Transform timeRoot)
    {
        WorldWeatherSettings weatherSettings = EnsureWeatherSettingsAsset();
        WorldWeatherHost weatherHost =
            EnsureComponentOnChild<WorldWeatherHost>(timeRoot, "WorldWeatherHost");
        SerializedObject weatherSo = new(weatherHost);
        weatherSo.FindProperty("_settings").objectReferenceValue = weatherSettings;
        weatherSo.ApplyModifiedPropertiesWithoutUndo();

        WorldEnvironmentPresenter presenter =
            EnsureComponentOnChild<WorldEnvironmentPresenter>(timeRoot, "WorldEnvironmentPresenter");
        Light sun = Object.FindAnyObjectByType<Light>();
        if (sun != null && sun.type != LightType.Directional)
        {
            Light[] lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            sun = null;
            for (int i = 0; i < lights.Length; i++)
            {
                if (lights[i] != null && lights[i].type == LightType.Directional)
                {
                    sun = lights[i];
                    break;
                }
            }
        }

        ParticleSystem rain = EnsureWeatherParticle(presenter.transform, "Vfx_Rain", new Color(0.55f, 0.7f, 1f, 0.7f), 80f);
        EnsureRainFloorLanding(rain);
        ParticleSystem wind = EnsureWeatherParticle(presenter.transform, "Vfx_Wind", new Color(0.85f, 0.85f, 0.9f, 0.35f), 40f);
        ParticleSystem snow = EnsureWeatherParticle(presenter.transform, "Vfx_Snow", Color.white, 50f);

        SerializedObject presenterSo = new(presenter);
        if (sun != null)
            presenterSo.FindProperty("_sunLight").objectReferenceValue = sun;
        presenterSo.FindProperty("_rainVfx").objectReferenceValue = rain;
        presenterSo.FindProperty("_windVfx").objectReferenceValue = wind;
        presenterSo.FindProperty("_snowVfx").objectReferenceValue = snow;
        presenterSo.ApplyModifiedPropertiesWithoutUndo();
    }

    static ParticleSystem EnsureWeatherParticle(Transform parent, string childName, Color startColor, float rate)
    {
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

        ParticleSystem ps = go.GetComponent<ParticleSystem>();
        if (ps == null)
            ps = Undo.AddComponent<ParticleSystem>(go);

        ParticleSystem.MainModule main = ps.main;
        main.loop = true;
        main.playOnAwake = false;
        main.startLifetime = 1.2f;
        main.startSize = 0.08f;
        main.startColor = startColor;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 400;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = rate;

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(8f, 0.2f, 8f);

        ParticleSystem.VelocityOverLifetimeModule velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        if (childName.IndexOf("Snow", System.StringComparison.Ordinal) >= 0)
            velocity.y = new ParticleSystem.MinMaxCurve(-1.2f);
        else if (childName.IndexOf("Wind", System.StringComparison.Ordinal) >= 0)
        {
            velocity.x = new ParticleSystem.MinMaxCurve(2.5f);
            velocity.y = new ParticleSystem.MinMaxCurve(-0.2f);
        }
        else
            velocity.y = new ParticleSystem.MinMaxCurve(-6f);

        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        return ps;
    }

    /// <summary>
    /// Rain PS에 Splash 자식 + Sub Emitter(Manual·Death) + <see cref="MapParticleFloorLanding"/>을 보장합니다.
    /// 착지 kill은 Manual Trigger; 자연사는 Death.
    /// </summary>
    static void EnsureRainFloorLanding(ParticleSystem rain)
    {
        if (rain == null)
            return;

        Transform splashT = rain.transform.Find("Splash");
        GameObject splashGo;
        if (splashT != null)
        {
            splashGo = splashT.gameObject;
        }
        else
        {
            splashGo = new GameObject("Splash");
            Undo.RegisterCreatedObjectUndo(splashGo, "Create Rain Splash");
            splashGo.transform.SetParent(rain.transform, false);
        }

        ParticleSystem splashPs = splashGo.GetComponent<ParticleSystem>();
        if (splashPs == null)
            splashPs = Undo.AddComponent<ParticleSystem>(splashGo);

        ParticleSystem.MainModule splashMain = splashPs.main;
        splashMain.loop = false;
        splashMain.playOnAwake = false;
        splashMain.startLifetime = 0.35f;
        splashMain.startSize = 0.15f;
        splashMain.startColor = new Color(0.7f, 0.85f, 1f, 0.55f);
        splashMain.simulationSpace = ParticleSystemSimulationSpace.World;
        splashMain.maxParticles = 128;

        ParticleSystem.EmissionModule splashEmission = splashPs.emission;
        splashEmission.rateOverTime = 0f;

        ParticleSystem.CollisionModule splashCol = splashPs.collision;
        splashCol.enabled = false;

        ParticleSystem.CollisionModule rainCol = rain.collision;
        rainCol.enabled = false;

        ParticleSystem.SubEmittersModule sub = rain.subEmitters;
        sub.enabled = true;
        bool hasManual = false;
        bool hasDeath = false;
        for (int i = 0; i < sub.subEmittersCount; i++)
        {
            if (sub.GetSubEmitterSystem(i) != splashPs)
                continue;

            ParticleSystemSubEmitterType t = sub.GetSubEmitterType(i);
            if (t == ParticleSystemSubEmitterType.Manual)
                hasManual = true;
            else if (t == ParticleSystemSubEmitterType.Death)
                hasDeath = true;
        }

        if (!hasManual)
        {
            sub.AddSubEmitter(
                splashPs,
                ParticleSystemSubEmitterType.Manual,
                ParticleSystemSubEmitterProperties.InheritNothing);
        }

        if (!hasDeath)
        {
            sub.AddSubEmitter(
                splashPs,
                ParticleSystemSubEmitterType.Death,
                ParticleSystemSubEmitterProperties.InheritNothing);
        }

        splashPs.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        MapParticleFloorLanding landing = rain.GetComponent<MapParticleFloorLanding>();
        if (landing == null)
            landing = Undo.AddComponent<MapParticleFloorLanding>(rain.gameObject);

        SerializedObject so = new(landing);
        so.FindProperty("_mode").enumValueIndex = (int)MapParticleLandingMode.KillOnLand;
        so.FindProperty("_maxLandingsPerFrame").intValue =
            MapParticleFloorLandingConsts.DefaultMaxLandingsPerFrame;
        so.FindProperty("_surfaceYOffset").floatValue =
            MapParticleFloorLandingConsts.DefaultSurfaceYOffset;
        SerializedProperty sysProp = so.FindProperty("_systems");
        sysProp.arraySize = 1;
        sysProp.GetArrayElementAtIndex(0).objectReferenceValue = rain;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static T EnsureHudPrefabInstance<T>(
        UICanvasLayerHost layerHost,
        T prefab,
        string instanceName) where T : Component
    {
        Transform hud = layerHost.GetLayerRoot(UICanvasLayer.HUD);
        Transform existing = hud.Find(instanceName);
        if (existing != null)
        {
            T panel = existing.GetComponent<T>();
            if (panel != null)
                return panel;

            Debug.LogError(
                $"[TimeUISetupMenu] '{instanceName}' under HUD lacks {typeof(T).Name}.",
                existing);
            return null;
        }

        T underHud = hud.GetComponentInChildren<T>(true);
        if (underHud != null)
        {
            underHud.gameObject.name = instanceName;
            return underHud;
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab.gameObject, hud);
        if (instance == null)
        {
            Debug.LogError(
                $"[TimeUISetupMenu] PrefabUtility.InstantiatePrefab failed for {instanceName}.",
                prefab);
            return null;
        }

        Undo.RegisterCreatedObjectUndo(instance, $"Place {instanceName}");
        instance.name = instanceName;
        Debug.Log(
            $"[TimeUISetupMenu] Placed HUD instance '{instanceName}' under {hud.name}.",
            instance);
        return instance.GetComponent<T>();
    }

    static WorldClockSettings EnsureSettingsAsset() =>
        DistScriptableObjectEnsure.LoadOrCreate<WorldClockSettings>(SettingsAssetPath);

    static WorldWeatherSettings EnsureWeatherSettingsAsset() =>
        DistScriptableObjectEnsure.LoadOrCreate<WorldWeatherSettings>(WeatherSettingsAssetPath);

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
}
#endif
