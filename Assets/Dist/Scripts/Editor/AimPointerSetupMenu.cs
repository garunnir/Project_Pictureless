// ============================================================
// AimPointerSetupMenu — Dist/MCP 조준 포인터 (센터 프리팹 + SDF 링)
// ============================================================

#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

static class AimPointerSetupMenu
{
    const string PrefabFolder = "Assets/Dist/Visual/Prefabs/UIComponents/HUD";
    const string PrefabPath = PrefabFolder + "/AimPointer.prefab";
    const string MaterialFolder = "Assets/Dist/Visual/Materials/UI";
    const string MaterialPath = MaterialFolder + "/UIAimRing.mat";
    const string ShaderName = "Dist/UI/AimRing";
    const string SpriteFolder = "Assets/Dist/Visual/Sprites/UI/HUD";
    const string DotSpritePath = SpriteFolder + "/AimPointerDot.png";
    const string InstanceName = "AimPointer";

    const float CenterSize = 6f;
    const float DefaultRingDiameter = 48f;
    const float StrokePx = 2f;
    const float SoftFill = 0.12f;

    [MenuItem(DistMcpMenus.CombatCreateAimPointerPrefabIfMissing)]
    static void CreatePrefabIfMissing()
    {
        EnsureFolders();
        EnsureDotSprite();
        EnsureMaterial();
        UIAimPointer existing = AssetDatabase.LoadAssetAtPath<UIAimPointer>(PrefabPath);
        if (existing != null)
        {
            Debug.Log($"[AimPointerSetupMenu] Prefab already exists: {PrefabPath}", existing);
            Selection.activeObject = existing;
            return;
        }

        SaveNewPrefab();
    }

    [MenuItem(DistMcpMenus.CombatPatchAimPointerCircle)]
    static void PatchAimPointerCircle()
    {
        EnsureFolders();
        EnsureDotSprite();
        EnsureMaterial();

        UIAimPointer existing = AssetDatabase.LoadAssetAtPath<UIAimPointer>(PrefabPath);
        if (existing != null)
        {
            string tempPath = PrefabFolder + "/_AimPointerPatchTemp.prefab";
            UIAimPointer built = BuildPrefabRoot();
            PrefabUtility.SaveAsPrefabAsset(built.gameObject, tempPath);
            Object.DestroyImmediate(built.gameObject);

            AssetDatabase.DeleteAsset(PrefabPath);
            AssetDatabase.MoveAsset(tempPath, PrefabPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[AimPointerSetupMenu] Patched SDF-ring AimPointer: {PrefabPath}");
        }
        else
        {
            SaveNewPrefab();
        }

        Canvas canvas = ResolveUiCanvas();
        if (canvas == null)
        {
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<UIAimPointer>(PrefabPath);
            return;
        }

        UICanvasLayerHost layerHost = canvas.GetComponent<UICanvasLayerHost>();
        if (layerHost == null)
            return;

        Transform topMost = layerHost.GetLayerRoot(UICanvasLayer.TopMost);
        Transform old = topMost != null ? topMost.Find(InstanceName) : null;
        if (old != null)
            Undo.DestroyObjectImmediate(old.gameObject);

        UIAimPointer prefab = AssetDatabase.LoadAssetAtPath<UIAimPointer>(PrefabPath);
        UIAimPointer instance = EnsureInstance(layerHost, prefab);
        if (instance != null)
        {
            SerializedObject so = new SerializedObject(instance);
            so.FindProperty("_rootCanvas").objectReferenceValue = canvas;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        if (!Application.isPlaying)
            EditorSceneManager.SaveOpenScenes();

        Selection.activeObject = AssetDatabase.LoadAssetAtPath<UIAimPointer>(PrefabPath);
        Debug.Log("[AimPointerSetupMenu] Scene AimPointer replaced (center + SDF ring).", instance);
    }

    [MenuItem(DistMcpMenus.CombatSetupAimPointerInOpenScene)]
    static void SetupInOpenScene()
    {
        Canvas canvas = ResolveUiCanvas();
        if (canvas == null)
        {
            Debug.LogError("[AimPointerSetupMenu] Canvas not found.");
            return;
        }

        UICanvasLayerHost layerHost = canvas.GetComponent<UICanvasLayerHost>();
        if (layerHost == null)
            layerHost = Undo.AddComponent<UICanvasLayerHost>(canvas.gameObject);
        layerHost.EditorSetupLayerHierarchy();

        UIAimPointer prefab = AssetDatabase.LoadAssetAtPath<UIAimPointer>(PrefabPath);
        if (prefab == null)
        {
            Debug.LogError(
                $"[AimPointerSetupMenu] Prefab missing: {PrefabPath}. " +
                "Run " + DistMcpMenus.CombatPatchAimPointerCircle + " first.");
            return;
        }

        UIAimPointer instance = EnsureInstance(layerHost, prefab);
        if (instance == null)
            return;

        SerializedObject so = new SerializedObject(instance);
        so.FindProperty("_rootCanvas").objectReferenceValue = canvas;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        if (!Application.isPlaying)
            EditorSceneManager.SaveOpenScenes();

        Debug.Log("[AimPointerSetupMenu] AimPointer wired under TopMost.", instance);
    }

    static void SaveNewPrefab()
    {
        UIAimPointer pointer = BuildPrefabRoot();
        PrefabUtility.SaveAsPrefabAsset(pointer.gameObject, PrefabPath);
        Object.DestroyImmediate(pointer.gameObject);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[AimPointerSetupMenu] Created prefab: {PrefabPath}");
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<UIAimPointer>(PrefabPath);
    }

    static UIAimPointer BuildPrefabRoot()
    {
        Material ringMat = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        Sprite dot = AssetDatabase.LoadAssetAtPath<Sprite>(DotSpritePath);

        var go = new GameObject(
            InstanceName,
            typeof(RectTransform),
            typeof(CanvasGroup),
            typeof(UIAimPointer));
        RectTransform root = go.GetComponent<RectTransform>();
        root.anchorMin = new Vector2(0.5f, 0.5f);
        root.anchorMax = new Vector2(0.5f, 0.5f);
        root.pivot = new Vector2(0.5f, 0.5f);
        root.sizeDelta = new Vector2(64f, 64f);
        root.anchoredPosition = Vector2.zero;

        CanvasGroup group = go.GetComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;
        group.interactable = false;

        RectTransform ringRt = CreateUiImage(
            root,
            "Ring",
            null,
            DefaultRingDiameter,
            DefaultRingDiameter,
            Vector2.zero,
            Color.white);
        Image ringImage = ringRt.GetComponent<Image>();
        ringImage.material = ringMat;
        ringImage.sprite = null;
        ringImage.color = Color.white;
        // UI Image without sprite still needs a mesh — assign white sprite if null breaks.
        if (ringImage.sprite == null)
        {
            Sprite white = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            ringImage.sprite = white;
            ringImage.type = Image.Type.Simple;
            ringImage.preserveAspect = true;
        }

        RectTransform center = CreateUiImage(
            root,
            "Center",
            dot,
            CenterSize,
            CenterSize,
            Vector2.zero,
            Color.white);

        UIAimPointer pointer = go.GetComponent<UIAimPointer>();
        SerializedObject so = new SerializedObject(pointer);
        so.FindProperty("_root").objectReferenceValue = root;
        so.FindProperty("_canvasGroup").objectReferenceValue = group;
        so.FindProperty("_ring").objectReferenceValue = ringRt;
        so.FindProperty("_ringImage").objectReferenceValue = ringImage;
        so.FindProperty("_ringMaterialTemplate").objectReferenceValue = ringMat;
        so.FindProperty("_center").objectReferenceValue = center;
        so.FindProperty("_minRadiusPx").floatValue = 6f;
        so.FindProperty("_maxRadiusPx").floatValue = 240f;
        so.FindProperty("_strokePx").floatValue = StrokePx;
        so.FindProperty("_softFill").floatValue = SoftFill;
        so.ApplyModifiedPropertiesWithoutUndo();

        return pointer;
    }

    static RectTransform CreateUiImage(
        RectTransform parent,
        string name,
        Sprite sprite,
        float width,
        float height,
        Vector2 anchored,
        Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(width, height);
        rt.anchoredPosition = anchored;

        Image image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        image.preserveAspect = true;
        return rt;
    }

    static void EnsureMaterial()
    {
        Shader shader = Shader.Find(ShaderName);
        if (shader == null)
        {
            Debug.LogError($"[AimPointerSetupMenu] Shader missing: {ShaderName}");
            return;
        }

        Material mat = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, MaterialPath);
        }
        else
        {
            mat.shader = shader;
        }

        mat.SetColor("_Color", Color.white);
        mat.SetFloat("_Thickness", 0.06f);
        mat.SetFloat("_SoftFill", SoftFill);
        EditorUtility.SetDirty(mat);
        AssetDatabase.SaveAssets();
    }

    static void EnsureDotSprite()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Dist/Visual/Sprites/UI"))
            AssetDatabase.CreateFolder("Assets/Dist/Visual/Sprites", "UI");
        if (!AssetDatabase.IsValidFolder(SpriteFolder))
            AssetDatabase.CreateFolder("Assets/Dist/Visual/Sprites/UI", "HUD");

        if (File.Exists(DotSpritePath))
        {
            EnsureSpriteImportSettings(DotSpritePath);
            return;
        }

        const int size = 64;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        float center = (size - 1) * 0.5f;
        float outer = size * 0.35f;
        float outerSq = outer * outer;
        Color32 c = Color.white;
        Color32 clear = new Color32(0, 0, 0, 0);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                tex.SetPixel(x, y, dx * dx + dy * dy <= outerSq ? c : clear);
            }
        }

        tex.Apply();
        File.WriteAllBytes(DotSpritePath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(DotSpritePath, ImportAssetOptions.ForceUpdate);
        EnsureSpriteImportSettings(DotSpritePath);
    }

    static void EnsureSpriteImportSettings(string assetPath)
    {
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
            return;

        bool dirty = false;
        if (importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            dirty = true;
        }

        if (importer.mipmapEnabled)
        {
            importer.mipmapEnabled = false;
            dirty = true;
        }

        if (importer.alphaIsTransparency != true)
        {
            importer.alphaIsTransparency = true;
            dirty = true;
        }

        if (dirty)
            importer.SaveAndReimport();
    }

    static UIAimPointer EnsureInstance(UICanvasLayerHost layerHost, UIAimPointer prefab)
    {
        Transform topMost = layerHost.GetLayerRoot(UICanvasLayer.TopMost);
        Transform existing = topMost.Find(InstanceName);
        if (existing != null)
        {
            UIAimPointer pointer = existing.GetComponent<UIAimPointer>();
            if (pointer != null)
                return pointer;

            Debug.LogError(
                $"[AimPointerSetupMenu] '{InstanceName}' lacks UIAimPointer.",
                existing);
            return null;
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab.gameObject, topMost);
        if (instance == null)
            return null;

        Undo.RegisterCreatedObjectUndo(instance, "Instantiate AimPointer");
        instance.name = InstanceName;
        return instance.GetComponent<UIAimPointer>();
    }

    static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Dist/Visual/Prefabs/UIComponents"))
            AssetDatabase.CreateFolder("Assets/Dist/Visual/Prefabs", "UIComponents");
        if (!AssetDatabase.IsValidFolder(PrefabFolder))
            AssetDatabase.CreateFolder("Assets/Dist/Visual/Prefabs/UIComponents", "HUD");
        if (!AssetDatabase.IsValidFolder("Assets/Dist/Visual/Materials"))
            AssetDatabase.CreateFolder("Assets/Dist/Visual", "Materials");
        if (!AssetDatabase.IsValidFolder(MaterialFolder))
            AssetDatabase.CreateFolder("Assets/Dist/Visual/Materials", "UI");
        if (!AssetDatabase.IsValidFolder("Assets/Dist/Visual/View/Shaders/UI"))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Dist/Visual/View/Shaders"))
                AssetDatabase.CreateFolder("Assets/Dist/Visual/View", "Shaders");
            AssetDatabase.CreateFolder("Assets/Dist/Visual/View/Shaders", "UI");
        }
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
