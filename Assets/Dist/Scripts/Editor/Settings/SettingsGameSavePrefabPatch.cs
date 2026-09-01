// ============================================================
// SettingsGameSavePrefabPatch — Game 카테고리·저장/불러오기 버튼 Patch
// ============================================================

#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

static class SettingsGameSavePrefabPatch
{
    const string WindowPrefabPath = "Assets/Dist/Visual/Prefabs/UIComponents/Settings/Grp_SettingsWindow.prefab";

    [MenuItem(DistMcpMenus.SettingsPatchGameSaveButtons)]
    static void PatchGameSaveButtons()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(WindowPrefabPath);
        if (root == null)
        {
            Debug.LogError($"[SettingsGameSavePrefabPatch] Prefab missing: {WindowPrefabPath}");
            return;
        }

        try
        {
            Transform categories = root.transform.Find("Area_Body/Area_Categories");
            Transform content = root.transform.Find("Area_Body/Area_Content");
            if (categories == null || content == null)
            {
                Debug.LogError("[SettingsGameSavePrefabPatch] Settings body hierarchy missing.", root);
                return;
            }

            Button gameButton = EnsureCategoryButton(
                categories,
                "Btn_Game",
                SettingsWindowLayout.ChromePadding +
                SettingsWindowLayout.CategoryButtonHeight +
                SettingsWindowLayout.CategoryButtonSpacing);

            Transform gamePageTransform = content.Find("Page_Game");
            GameObject gamePage = gamePageTransform != null
                ? gamePageTransform.gameObject
                : CreateGamePage(content);

            Button saveButton = EnsureGameActionButton(gamePage.transform, "Btn_Save");
            Button loadButton = EnsureGameActionButton(gamePage.transform, "Btn_Load");
            gamePage.SetActive(false);

            WireSettingsWindow(root, gameButton, gamePage, saveButton, loadButton);

            PrefabUtility.SaveAsPrefabAsset(root, WindowPrefabPath);
            Debug.Log("[SettingsGameSavePrefabPatch] Game save/load buttons patched.", root);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    static void WireSettingsWindow(GameObject root, Button gameButton, GameObject gamePage, Button saveButton, Button loadButton)
    {
        UISettingsWindow window = root.GetComponent<UISettingsWindow>();
        if (window == null)
            return;

        Transform graphicsPage = root.transform.Find("Area_Body/Area_Content/Page_Graphics");
        window.Wire(
            root.transform as RectTransform,
            root.transform.Find("Area_Header")?.GetComponent<UIWindowDragHandler>(),
            root.transform.Find("Area_Header/Txt_Title")?.GetComponent<TMP_Text>(),
            root.transform.Find("Area_Body/Area_Categories/Btn_Graphics")?.GetComponent<Button>(),
            graphicsPage != null ? graphicsPage.gameObject : null,
            gameButton,
            gamePage,
            saveButton,
            loadButton,
            FindToggle(graphicsPage, "Toggle_HudLayoutAdjust"),
            FindToggle(graphicsPage, "Toggle_HudTime"),
            FindToggle(graphicsPage, "Toggle_HudTimeScale"),
            FindToggle(graphicsPage, "Toggle_HudMessageLog"),
            FindToggle(graphicsPage, "Toggle_HudSummary"));
    }

    static GameObject CreateGamePage(Transform content)
    {
        GameObject gamePage = CreateRect("Page_Game", content, Color.clear);
        RectTransform pageRect = gamePage.GetComponent<RectTransform>();
        pageRect.anchorMin = Vector2.zero;
        pageRect.anchorMax = Vector2.one;
        pageRect.offsetMin = new Vector2(SettingsWindowLayout.ChromePadding, SettingsWindowLayout.ChromePadding);
        pageRect.offsetMax = new Vector2(-SettingsWindowLayout.ChromePadding, -SettingsWindowLayout.ChromePadding);

        VerticalLayoutGroup stack = gamePage.AddComponent<VerticalLayoutGroup>();
        stack.childAlignment = TextAnchor.UpperLeft;
        stack.spacing = SettingsWindowLayout.GameActionButtonSpacing;
        stack.childControlWidth = true;
        stack.childControlHeight = true;
        stack.childForceExpandWidth = true;
        stack.childForceExpandHeight = false;

        EnsureGameActionButton(gamePage.transform, "Btn_Save");
        EnsureGameActionButton(gamePage.transform, "Btn_Load");
        return gamePage;
    }

    static Button EnsureCategoryButton(Transform categories, string name, float topOffset)
    {
        Transform existing = categories.Find(name);
        if (existing != null)
            return existing.GetComponent<Button>();

        GameObject go = CreateRect(name, categories, SettingsWindowLayout.CategoryColor);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -topOffset);
        rect.sizeDelta = new Vector2(
            -SettingsWindowLayout.ChromePadding * 2f,
            SettingsWindowLayout.CategoryButtonHeight);
        go.GetComponent<Image>().raycastTarget = true;

        Button button = go.AddComponent<Button>();
        TMP_Text label = CreateTmp("Label", go.transform);
        Stretch(label.rectTransform, 4f, 4f, 0f, 0f);
        return button;
    }

    static Button EnsureGameActionButton(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
            return ConfigureActionButton(existing.gameObject);

        GameObject go = CreateRect(name, parent, SettingsWindowLayout.CategoryColor);
        return ConfigureActionButton(go);
    }

    static Button ConfigureActionButton(GameObject go)
    {
        LayoutElement layout = go.GetComponent<LayoutElement>();
        if (layout == null)
            layout = go.AddComponent<LayoutElement>();
        layout.preferredHeight = SettingsWindowLayout.GameActionButtonHeight;
        layout.minHeight = SettingsWindowLayout.GameActionButtonHeight;
        go.GetComponent<Image>().raycastTarget = true;

        Button button = go.GetComponent<Button>();
        if (button == null)
            button = go.AddComponent<Button>();

        TMP_Text label = go.GetComponentInChildren<TMP_Text>(true);
        if (label == null)
        {
            label = CreateTmp("Label", go.transform);
            Stretch(label.rectTransform, 4f, 4f, 0f, 0f);
        }

        return button;
    }

    static Toggle FindToggle(Transform parent, string name)
    {
        if (parent == null)
            return null;
        Transform child = parent.Find(name);
        return child != null ? child.GetComponent<Toggle>() : null;
    }

    static GameObject CreateRect(string name, Transform parent, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        go.layer = LayerMask.NameToLayer("UI");
        Image image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return go;
    }

    static TMP_Text CreateTmp(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        go.layer = LayerMask.NameToLayer("UI");
        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(SettingsUIFactory.DefaultUIFontPath);
        tmp.fontSize = SettingsWindowLayout.FontSizeBody;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        return tmp;
    }

    static void Stretch(RectTransform rect, float left, float right, float top, float bottom)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
    }
}
#endif
