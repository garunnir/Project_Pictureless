// ============================================================
// LifeThreatHudSetupMenu — Dist/MCP 생명 위험 HUD 비니엣 프리팹·씬 Setup
// ============================================================

#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

static class LifeThreatHudSetupMenu
{
    const string PrefabFolder = "Assets/Dist/Visual/Prefabs/UIComponents/HUD";
    const string PrefabPath = PrefabFolder + "/Hud_LifeThreat.prefab";
    const string SpriteFolder = "Assets/Dist/Visual/Sprites/UI/HUD";
    const string VignetteSpritePath = SpriteFolder + "/LifeThreatVignette.png";
    const string InstanceName = "Hud_LifeThreat";

    const int VignetteSize = 256;
    const float VignetteInner = 0.35f;

    [MenuItem(DistMcpMenus.HudCreateLifeThreatPrefabIfMissing)]
    static void CreatePrefabIfMissing()
    {
        EnsureFolders();
        EnsureVignetteSprite();

        UIHudLifeThreatOverlay existing =
            AssetDatabase.LoadAssetAtPath<UIHudLifeThreatOverlay>(PrefabPath);
        if (existing != null)
        {
            Debug.Log($"[LifeThreatHudSetupMenu] Prefab already exists: {PrefabPath}", existing);
            Selection.activeObject = existing;
            return;
        }

        SaveNewPrefab();
    }

    [MenuItem(DistMcpMenus.HudSetupLifeThreatOverlayInOpenScene)]
    static void SetupInOpenScene()
    {
        Canvas canvas = ResolveUiCanvas();
        if (canvas == null)
        {
            Debug.LogError("[LifeThreatHudSetupMenu] Canvas not found.");
            return;
        }

        UICanvasLayerHost layerHost = canvas.GetComponent<UICanvasLayerHost>();
        if (layerHost == null)
            layerHost = Undo.AddComponent<UICanvasLayerHost>(canvas.gameObject);
        layerHost.EditorSetupLayerHierarchy();

        UIHudLifeThreatOverlay prefab =
            AssetDatabase.LoadAssetAtPath<UIHudLifeThreatOverlay>(PrefabPath);
        if (prefab == null)
        {
            Debug.LogError(
                $"[LifeThreatHudSetupMenu] Prefab missing: {PrefabPath}. " +
                "Run " + DistMcpMenus.HudCreateLifeThreatPrefabIfMissing + " first.");
            return;
        }

        UIHudLifeThreatOverlay instance = EnsureHudInstance(layerHost, prefab);
        if (instance == null)
            return;

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        if (!Application.isPlaying)
            EditorSceneManager.SaveOpenScenes();

        Debug.Log("[LifeThreatHudSetupMenu] Life threat overlay wired under HUD (first sibling).", instance);
    }

    static void SaveNewPrefab()
    {
        UIHudLifeThreatOverlay overlay = BuildPrefabRoot();
        PrefabUtility.SaveAsPrefabAsset(overlay.gameObject, PrefabPath);
        Object.DestroyImmediate(overlay.gameObject);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[LifeThreatHudSetupMenu] Created prefab: {PrefabPath}");
        Selection.activeObject =
            AssetDatabase.LoadAssetAtPath<UIHudLifeThreatOverlay>(PrefabPath);
    }

    static UIHudLifeThreatOverlay BuildPrefabRoot()
    {
        Sprite vignette = AssetDatabase.LoadAssetAtPath<Sprite>(VignetteSpritePath);

        var go = new GameObject(
            InstanceName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(CanvasGroup),
            typeof(Image),
            typeof(UIHudLifeThreatOverlay));

        RectTransform root = go.GetComponent<RectTransform>();
        StretchFullScreen(root);

        CanvasGroup group = go.GetComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;
        group.interactable = false;

        Image image = go.GetComponent<Image>();
        image.sprite = vignette;
        image.type = Image.Type.Simple;
        image.raycastTarget = false;
        image.color = new Color(0.85f, 0.08f, 0.06f, 1f);
        image.preserveAspect = false;

        UIHudLifeThreatOverlay overlay = go.GetComponent<UIHudLifeThreatOverlay>();
        SerializedObject so = new SerializedObject(overlay);
        so.FindProperty("_canvasGroup").objectReferenceValue = group;
        so.FindProperty("_vignetteImage").objectReferenceValue = image;
        so.ApplyModifiedPropertiesWithoutUndo();

        return overlay;
    }

    static void StretchFullScreen(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;
    }

    static UIHudLifeThreatOverlay EnsureHudInstance(
        UICanvasLayerHost layerHost,
        UIHudLifeThreatOverlay prefab)
    {
        Transform hud = layerHost.GetLayerRoot(UICanvasLayer.HUD);
        Transform existing = hud.Find(InstanceName);
        if (existing != null)
        {
            UIHudLifeThreatOverlay overlay = existing.GetComponent<UIHudLifeThreatOverlay>();
            if (overlay != null)
            {
                existing.SetAsFirstSibling();
                return overlay;
            }

            Debug.LogError(
                $"[LifeThreatHudSetupMenu] '{InstanceName}' lacks UIHudLifeThreatOverlay.",
                existing);
            return null;
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab.gameObject, hud);
        if (instance == null)
            return null;

        Undo.RegisterCreatedObjectUndo(instance, "Instantiate Life Threat HUD");
        instance.name = InstanceName;
        instance.transform.SetAsFirstSibling();
        return instance.GetComponent<UIHudLifeThreatOverlay>();
    }

    static void EnsureVignetteSprite()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Dist/Visual/Sprites/UI"))
            AssetDatabase.CreateFolder("Assets/Dist/Visual/Sprites", "UI");
        if (!AssetDatabase.IsValidFolder(SpriteFolder))
            AssetDatabase.CreateFolder("Assets/Dist/Visual/Sprites/UI", "HUD");

        if (File.Exists(VignetteSpritePath))
        {
            EnsureSpriteImportSettings(VignetteSpritePath);
            return;
        }

        var tex = new Texture2D(VignetteSize, VignetteSize, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        float center = (VignetteSize - 1) * 0.5f;
        Color32 white = Color.white;
        Color32 clear = new Color32(255, 255, 255, 0);
        for (int y = 0; y < VignetteSize; y++)
        {
            for (int x = 0; x < VignetteSize; x++)
            {
                float dx = (x - center) / center;
                float dy = (y - center) / center;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.Clamp01((dist - VignetteInner) / (1f - VignetteInner));
                byte a = (byte)(alpha * 255f);
                tex.SetPixel(x, y, a > 0 ? new Color32(255, 255, 255, a) : clear);
            }
        }

        tex.Apply();
        File.WriteAllBytes(VignetteSpritePath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(VignetteSpritePath, ImportAssetOptions.ForceUpdate);
        EnsureSpriteImportSettings(VignetteSpritePath);
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

    static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Dist/Visual/Prefabs/UIComponents"))
            AssetDatabase.CreateFolder("Assets/Dist/Visual/Prefabs", "UIComponents");
        if (!AssetDatabase.IsValidFolder(PrefabFolder))
            AssetDatabase.CreateFolder("Assets/Dist/Visual/Prefabs/UIComponents", "HUD");
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
