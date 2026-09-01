// ============================================================
// CharacterActionGaugePatchMenu — Dist/MCP 플레이어 행동 게이지 Patch
// ============================================================

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class CharacterActionGaugePatchMenu
{
    const string PrefabFolder = "Assets/Dist/Visual/Prefabs/UIComponents/Character";

    [MenuItem(DistMcpMenus.CharacterPatchActionGaugeOnPlayer)]
    public static void PatchActionGaugeOnPlayer()
    {
        PlayerInventoryRuntime runtime = Object.FindAnyObjectByType<PlayerInventoryRuntime>();
        if (runtime == null)
        {
            Debug.LogError("[CharacterActionGaugePatchMenu] PlayerInventoryRuntime not found.");
            return;
        }

        GameObject player = runtime.gameObject;
        Undo.RegisterCompleteObjectUndo(player, "Patch Action Gauge On Player");

        if (player.GetComponent<CharacterActionHost>() == null)
            Undo.AddComponent<CharacterActionHost>(player);

        GameObject prefab = EnsurePrefab();
        if (prefab == null)
        {
            Debug.LogError("[CharacterActionGaugePatchMenu] Gauge prefab missing.");
            return;
        }

        Transform existing = player.transform.Find(CharacterActionGaugeLayout.RootName);
        if (existing == null)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, player.transform);
            instance.name = CharacterActionGaugeLayout.RootName;
            Undo.RegisterCreatedObjectUndo(instance, "Patch Action Gauge Instance");
        }

        EditorUtility.SetDirty(player);
        if (!Application.isPlaying)
            EditorSceneManager.SaveOpenScenes();
        Debug.Log("[CharacterActionGaugePatchMenu] Action gauge patched on player.", player);
    }

    static GameObject EnsurePrefab()
    {
        EnsureFolder();
        GameObject existing =
            AssetDatabase.LoadAssetAtPath<GameObject>(CharacterActionGaugeLayout.PrefabPath);
        if (existing != null)
            return existing;

        GameObject root = new(CharacterActionGaugeLayout.RootName);
        RectTransform rootRt = root.AddComponent<RectTransform>();
        rootRt.sizeDelta = CharacterActionGaugeLayout.Size;
        root.transform.localPosition = new Vector3(0f, CharacterActionGaugeLayout.LocalY, 0f);
        root.transform.localScale = Vector3.one * CharacterActionGaugeLayout.WorldScale;

        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 20;

        WorldBillboard billboard = root.AddComponent<WorldBillboard>();
        SerializedObject billboardSo = new(billboard);
        billboardSo.FindProperty("_billboardEnabled").boolValue = true;
        billboardSo.FindProperty("_timeChannel").enumValueIndex = (int)TimeScaleChannel.Realtime;
        billboardSo.ApplyModifiedPropertiesWithoutUndo();

        UICharacterActionGauge gauge = root.AddComponent<UICharacterActionGauge>();

        GameObject fillGo = new(CharacterActionGaugeLayout.FillName);
        fillGo.transform.SetParent(root.transform, false);
        RectTransform fillRt = fillGo.AddComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = Vector2.zero;
        fillRt.offsetMax = Vector2.zero;
        fillGo.AddComponent<CanvasRenderer>();
        Image fill = fillGo.AddComponent<Image>();
        fill.raycastTarget = false;
        fill.color = CharacterActionGaugeLayout.FillColor;
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        fill.fillAmount = 0f;
        Sprite sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
        if (sprite != null)
            fill.sprite = sprite;

        SerializedObject so = new(gauge);
        so.FindProperty("_fill").objectReferenceValue = fill;
        so.FindProperty("_canvas").objectReferenceValue = canvas;
        so.ApplyModifiedPropertiesWithoutUndo();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, CharacterActionGaugeLayout.PrefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return prefab;
    }

    static void EnsureFolder()
    {
        if (AssetDatabase.IsValidFolder(PrefabFolder))
            return;
        if (!AssetDatabase.IsValidFolder("Assets/Dist/Visual/Prefabs/UIComponents"))
            return;
        AssetDatabase.CreateFolder("Assets/Dist/Visual/Prefabs/UIComponents", "Character");
    }
}
#endif
