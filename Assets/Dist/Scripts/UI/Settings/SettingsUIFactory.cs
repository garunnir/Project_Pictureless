// ============================================================
// SettingsUIFactory — 세팅 Overlay 계층 생성 (프리팹/MCP용)
// ============================================================

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class SettingsUIFactory
{
    public const string DefaultUIFontPath = TimeUIFactory.DefaultUIFontPath;

    public static UISettingsWindow CreateWindowRoot()
    {
        GameObject root = CreateRect("Grp_SettingsWindow", null, SettingsWindowLayout.PanelColor);
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0f, 0.5f);
        rootRect.anchorMax = new Vector2(0f, 0.5f);
        rootRect.pivot = new Vector2(0f, 0.5f);
        rootRect.anchoredPosition = SettingsWindowLayout.AnchoredPosition;
        rootRect.sizeDelta = new Vector2(SettingsWindowLayout.PanelWidth, SettingsWindowLayout.PanelHeight);

        GameObject headerGo = CreateRect("Area_Header", root.transform, SettingsWindowLayout.HeaderColor);
        RectTransform headerRect = headerGo.GetComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0f, 1f);
        headerRect.anchorMax = new Vector2(1f, 1f);
        headerRect.pivot = new Vector2(0.5f, 1f);
        headerRect.anchoredPosition = Vector2.zero;
        headerRect.sizeDelta = new Vector2(0f, SettingsWindowLayout.HeaderHeight);
        headerGo.GetComponent<Image>().raycastTarget = true;

        UIWindowDragHandler dragHandler = headerGo.AddComponent<UIWindowDragHandler>();
        dragHandler.Initialize(rootRect, null);

        TMP_Text title = CreateTmp(
            "Txt_Title",
            headerGo.transform,
            SettingsWindowLayout.FontSizeHeader,
            TextAlignmentOptions.MidlineLeft);
        Stretch(title.rectTransform, SettingsWindowLayout.ChromePadding, 48f, 0f, 0f);

        GameObject bodyGo = CreateRect("Area_Body", root.transform, Color.clear);
        RectTransform bodyRect = bodyGo.GetComponent<RectTransform>();
        Stretch(bodyRect, 0f, 0f, SettingsWindowLayout.HeaderHeight, 0f);
        bodyGo.GetComponent<Image>().raycastTarget = false;

        GameObject categoriesGo = CreateRect("Area_Categories", bodyGo.transform, SettingsWindowLayout.CategoryColor);
        RectTransform categoriesRect = categoriesGo.GetComponent<RectTransform>();
        categoriesRect.anchorMin = new Vector2(0f, 0f);
        categoriesRect.anchorMax = new Vector2(0f, 1f);
        categoriesRect.pivot = new Vector2(0f, 0.5f);
        categoriesRect.anchoredPosition = Vector2.zero;
        categoriesRect.sizeDelta = new Vector2(SettingsWindowLayout.CategoryColumnWidth, 0f);

        Button graphicsButton = CreateCategoryButton(categoriesGo.transform, "Btn_Graphics");

        GameObject contentGo = CreateRect("Area_Content", bodyGo.transform, SettingsWindowLayout.ContentColor);
        RectTransform contentRect = contentGo.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 0f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.offsetMin = new Vector2(SettingsWindowLayout.CategoryColumnWidth, 0f);
        contentRect.offsetMax = Vector2.zero;

        GameObject graphicsPage = CreateRect("Page_Graphics", contentGo.transform, Color.clear);
        Stretch(graphicsPage.GetComponent<RectTransform>(), SettingsWindowLayout.ChromePadding);
        graphicsPage.GetComponent<Image>().raycastTarget = false;

        VerticalLayoutGroup stack = graphicsPage.AddComponent<VerticalLayoutGroup>();
        stack.childAlignment = TextAnchor.UpperLeft;
        stack.spacing = SettingsWindowLayout.ToggleStackSpacing;
        stack.childControlWidth = true;
        stack.childControlHeight = true;
        stack.childForceExpandWidth = true;
        stack.childForceExpandHeight = false;

        Toggle hudToggle = CreateToggle(graphicsPage.transform, "Toggle_HudLayoutAdjust", 0f, SettingsWindowLayout.FontSizeBody);
        Toggle hudTimeToggle = CreateToggle(graphicsPage.transform, "Toggle_HudTime", SettingsWindowLayout.HudPopupToggleInset, SettingsWindowLayout.FontSizeHudPopup);
        Toggle hudTimeScaleToggle = CreateToggle(graphicsPage.transform, "Toggle_HudTimeScale", SettingsWindowLayout.HudPopupToggleInset, SettingsWindowLayout.FontSizeHudPopup);
        Toggle hudMessageLogToggle = CreateToggle(graphicsPage.transform, "Toggle_HudMessageLog", SettingsWindowLayout.HudPopupToggleInset, SettingsWindowLayout.FontSizeHudPopup);
        Toggle hudSummaryToggle = CreateToggle(graphicsPage.transform, "Toggle_HudSummary", SettingsWindowLayout.HudPopupToggleInset, SettingsWindowLayout.FontSizeHudPopup);

        root.AddComponent<UIOverlayWindow>();
        UISettingsWindow window = root.AddComponent<UISettingsWindow>();
        window.Wire(
            rootRect,
            dragHandler,
            title,
            graphicsButton,
            graphicsPage,
            hudToggle,
            hudTimeToggle,
            hudTimeScaleToggle,
            hudMessageLogToggle,
            hudSummaryToggle);
        return window;
    }

    static Button CreateCategoryButton(Transform parent, string name)
    {
        GameObject go = CreateRect(name, parent, SettingsWindowLayout.CategoryColor);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -SettingsWindowLayout.ChromePadding);
        rect.sizeDelta = new Vector2(-SettingsWindowLayout.ChromePadding * 2f, 28f);
        go.GetComponent<Image>().raycastTarget = true;

        Button button = go.AddComponent<Button>();
        TMP_Text label = CreateTmp("Label", go.transform, SettingsWindowLayout.FontSizeBody, TextAlignmentOptions.Center);
        Stretch(label.rectTransform, 4f, 4f, 0f, 0f);
        return button;
    }

    static Toggle CreateToggle(Transform parent, string name, float insetLeft, int fontSize)
    {
        GameObject row = CreateRect(name, parent, Color.clear);
        LayoutElement layout = row.AddComponent<LayoutElement>();
        layout.preferredHeight = SettingsWindowLayout.ToggleRowHeight;
        layout.minHeight = SettingsWindowLayout.ToggleRowHeight;
        row.GetComponent<Image>().raycastTarget = true;

        GameObject boxGo = CreateRect("Box", row.transform, SettingsWindowLayout.CategoryColor);
        RectTransform boxRect = boxGo.GetComponent<RectTransform>();
        boxRect.anchorMin = new Vector2(0f, 0.5f);
        boxRect.anchorMax = new Vector2(0f, 0.5f);
        boxRect.pivot = new Vector2(0f, 0.5f);
        boxRect.anchoredPosition = new Vector2(insetLeft, 0f);
        boxRect.sizeDelta = new Vector2(20f, 20f);
        boxGo.GetComponent<Image>().raycastTarget = true;

        GameObject checkGo = CreateRect("Check", boxGo.transform, new Color(0.5f, 0.75f, 1f, 1f));
        Stretch(checkGo.GetComponent<RectTransform>(), 4f, 4f, 4f, 4f);

        Toggle toggle = row.AddComponent<Toggle>();
        toggle.targetGraphic = boxGo.GetComponent<Image>();
        toggle.graphic = checkGo.GetComponent<Image>();
        toggle.isOn = true;

        TMP_Text label = CreateTmp("Label", row.transform, fontSize, TextAlignmentOptions.MidlineLeft);
        RectTransform labelRect = label.rectTransform;
        labelRect.anchorMin = new Vector2(0f, 0f);
        labelRect.anchorMax = new Vector2(1f, 1f);
        labelRect.offsetMin = new Vector2(insetLeft + 28f, 0f);
        labelRect.offsetMax = Vector2.zero;
        return toggle;
    }

    static GameObject CreateRect(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        if (parent != null)
            go.transform.SetParent(parent, false);
        go.layer = LayerMask.NameToLayer("UI");
        Image image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return go;
    }

    static TMP_Text CreateTmp(string name, Transform parent, float fontSize, TextAlignmentOptions align)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        go.layer = LayerMask.NameToLayer("UI");
        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.font = LoadDefaultFont();
        tmp.fontSize = fontSize;
        tmp.color = Color.white;
        tmp.alignment = align;
        tmp.raycastTarget = false;
        return tmp;
    }

    static void Stretch(RectTransform rect, float padding) =>
        Stretch(rect, padding, padding, padding, padding);

    static void Stretch(RectTransform rect, float left, float right, float top, float bottom)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
    }

    static TMP_FontAsset LoadDefaultFont()
    {
#if UNITY_EDITOR
        return UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(DefaultUIFontPath);
#else
        return DistUiFont.Get();
#endif
    }
}
