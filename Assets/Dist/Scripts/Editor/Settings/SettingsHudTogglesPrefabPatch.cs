// ============================================================
// SettingsHudTogglesPrefabPatch — Graphics 페이지 HUD 팝업 토글 Patch
// ============================================================

#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

static class SettingsHudTogglesPrefabPatch
{
    const string WindowPrefabPath = "Assets/Dist/Visual/Prefabs/UIComponents/Settings/Grp_SettingsWindow.prefab";

    [MenuItem(DistMcpMenus.SettingsPatchHudPopupToggles)]
    static void PatchHudPopupToggles()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(WindowPrefabPath);
        if (root == null)
        {
            Debug.LogError($"[SettingsHudTogglesPrefabPatch] Prefab missing: {WindowPrefabPath}");
            return;
        }

        try
        {
            Transform graphicsPage = root.transform.Find("Area_Body/Area_Content/Page_Graphics");
            if (graphicsPage == null)
            {
                Debug.LogError("[SettingsHudTogglesPrefabPatch] Page_Graphics not found.", root);
                return;
            }

            EnsureToggleStack(graphicsPage);
            WireSettingsWindow(root, graphicsPage);

            PrefabUtility.SaveAsPrefabAsset(root, WindowPrefabPath);
            Debug.Log("[SettingsHudTogglesPrefabPatch] HUD popup toggles patched.", root);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    static void WireSettingsWindow(GameObject root, Transform graphicsPage)
    {
        UISettingsWindow window = root.GetComponent<UISettingsWindow>();
        if (window == null)
            return;

        window.Wire(
            root.transform as RectTransform,
            root.transform.Find("Area_Header")?.GetComponent<UIWindowDragHandler>(),
            root.transform.Find("Area_Header/Txt_Title")?.GetComponent<TMP_Text>(),
            root.transform.Find("Area_Body/Area_Categories/Btn_Graphics")?.GetComponent<Button>(),
            graphicsPage.gameObject,
            FindToggle(graphicsPage, "Toggle_HudLayoutAdjust"),
            FindToggle(graphicsPage, "Toggle_HudTime"),
            FindToggle(graphicsPage, "Toggle_HudTimeScale"),
            FindToggle(graphicsPage, "Toggle_HudMessageLog"),
            FindToggle(graphicsPage, "Toggle_HudSummary"));
    }

    static void EnsureToggleStack(Transform graphicsPage)
    {
        VerticalLayoutGroup stack = graphicsPage.GetComponent<VerticalLayoutGroup>();
        if (stack == null)
            stack = graphicsPage.gameObject.AddComponent<VerticalLayoutGroup>();

        stack.childAlignment = TextAnchor.UpperLeft;
        stack.spacing = SettingsWindowLayout.ToggleStackSpacing;
        stack.childControlWidth = true;
        stack.childControlHeight = true;
        stack.childForceExpandWidth = true;
        stack.childForceExpandHeight = false;

        EnsureToggle(graphicsPage, "Toggle_HudLayoutAdjust", 0f, SettingsWindowLayout.FontSizeBody);
        EnsureToggle(graphicsPage, "Toggle_HudTime", SettingsWindowLayout.HudPopupToggleInset, SettingsWindowLayout.FontSizeHudPopup);
        EnsureToggle(graphicsPage, "Toggle_HudTimeScale", SettingsWindowLayout.HudPopupToggleInset, SettingsWindowLayout.FontSizeHudPopup);
        EnsureToggle(graphicsPage, "Toggle_HudMessageLog", SettingsWindowLayout.HudPopupToggleInset, SettingsWindowLayout.FontSizeHudPopup);
        EnsureToggle(graphicsPage, "Toggle_HudSummary", SettingsWindowLayout.HudPopupToggleInset, SettingsWindowLayout.FontSizeHudPopup);
    }

    static void EnsureToggle(Transform parent, string name, float insetLeft, int fontSize)
    {
        Transform existing = parent.Find(name);
        Toggle toggle = existing != null ? existing.GetComponent<Toggle>() : CreateToggleRow(parent, name, insetLeft);
        if (toggle == null)
            return;

        LayoutElement layout = toggle.GetComponent<LayoutElement>();
        if (layout == null)
            layout = toggle.gameObject.AddComponent<LayoutElement>();
        layout.preferredHeight = SettingsWindowLayout.ToggleRowHeight;
        layout.minHeight = SettingsWindowLayout.ToggleRowHeight;

        TMP_Text label = toggle.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            label.fontSize = fontSize;
            label.enableAutoSizing = false;
        }
    }

    static Toggle CreateToggleRow(Transform parent, string name, float insetLeft)
    {
        GameObject row = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement), typeof(Toggle));
        row.transform.SetParent(parent, false);
        row.layer = LayerMask.NameToLayer("UI");

        Image rowImage = row.GetComponent<Image>();
        rowImage.color = Color.clear;
        rowImage.raycastTarget = true;

        GameObject boxGo = new GameObject("Box", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        boxGo.transform.SetParent(row.transform, false);
        boxGo.layer = LayerMask.NameToLayer("UI");
        RectTransform boxRect = boxGo.GetComponent<RectTransform>();
        boxRect.anchorMin = new Vector2(0f, 0.5f);
        boxRect.anchorMax = new Vector2(0f, 0.5f);
        boxRect.pivot = new Vector2(0f, 0.5f);
        boxRect.anchoredPosition = new Vector2(insetLeft, 0f);
        boxRect.sizeDelta = new Vector2(20f, 20f);
        Image boxImage = boxGo.GetComponent<Image>();
        boxImage.color = SettingsWindowLayout.CategoryColor;
        boxImage.raycastTarget = true;

        GameObject checkGo = new GameObject("Check", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        checkGo.transform.SetParent(boxGo.transform, false);
        checkGo.layer = LayerMask.NameToLayer("UI");
        RectTransform checkRect = checkGo.GetComponent<RectTransform>();
        checkRect.anchorMin = Vector2.zero;
        checkRect.anchorMax = Vector2.one;
        checkRect.offsetMin = new Vector2(4f, 4f);
        checkRect.offsetMax = new Vector2(-4f, -4f);
        Image checkImage = checkGo.GetComponent<Image>();
        checkImage.color = new Color(0.5f, 0.75f, 1f, 1f);

        GameObject labelGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        labelGo.transform.SetParent(row.transform, false);
        labelGo.layer = LayerMask.NameToLayer("UI");
        RectTransform labelRect = labelGo.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(insetLeft + 28f, 0f);
        labelRect.offsetMax = Vector2.zero;

        TextMeshProUGUI label = labelGo.GetComponent<TextMeshProUGUI>();
        label.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(SettingsUIFactory.DefaultUIFontPath);
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.MidlineLeft;

        Toggle toggle = row.GetComponent<Toggle>();
        toggle.targetGraphic = boxImage;
        toggle.graphic = checkImage;
        toggle.isOn = true;
        return toggle;
    }

    static Toggle FindToggle(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        return child != null ? child.GetComponent<Toggle>() : null;
    }
}
#endif
