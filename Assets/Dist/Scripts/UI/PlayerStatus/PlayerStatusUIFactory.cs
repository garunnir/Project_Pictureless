// ============================================================
// PlayerStatusUIFactory — 상태창 UI 계층 런타임/에디터 공통 생성
// ============================================================

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class PlayerStatusUIFactory
{
    public const string DefaultUIFontPath = "Assets/Dist/Scripts/UI/Font/Katuri SDF.asset";

    public static readonly Vector2 WindowSize = new(360f, 480f);
    public static readonly Vector2 DetailSize = new(240f, 320f);
    public const float RowHeight = 28f;
    public const float HeaderHeight = 32f;
    public const float ResizeEdgeThickness = 8f;
    public const float ResizeCornerSize = 14f;
    public const int FontSizeBody = 14;
    public const int FontSizeHeader = 18;

    static readonly Color PanelColor = new(0.12f, 0.12f, 0.12f, 0.95f);
    static readonly Color RowColor = new(0.18f, 0.18f, 0.18f, 1f);
    static readonly Color FillColor = new(0.55f, 0.2f, 0.2f, 1f);
    static readonly Color FillBgColor = new(0.08f, 0.08f, 0.08f, 1f);
    static readonly Color DetailColor = new(0.1f, 0.12f, 0.16f, 0.98f);
    static readonly Color ResizeHandleColor = new(1f, 1f, 1f, 0.02f);

    public static UIPlayerStatusWindow CreateWindowRoot()
    {
        GameObject root = CreateRect("Grp_PlayerStatusWindow", null, PanelColor);
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 0.5f);
        rootRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.sizeDelta = WindowSize;

        // VLG는 루트가 아니라 Area_Content에만 — resize/detail은 레이아웃 밖.
        GameObject contentGo = CreateRect("Area_Content", root.transform, Color.clear);
        contentGo.GetComponent<Image>().raycastTarget = false;
        Stretch(contentGo.GetComponent<RectTransform>(), 0f, 0f, 0f, 0f);
        VerticalLayoutGroup vlg = contentGo.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(10, 10, 10, 10);
        vlg.spacing = 8f;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        Transform content = contentGo.transform;

        GameObject header = CreateRect("Header", content, new Color(0.16f, 0.16f, 0.16f, 1f));
        header.AddComponent<LayoutElement>().preferredHeight = HeaderHeight;
        header.AddComponent<UIWindowDragHandler>();
        TMP_Text title = CreateTmp("Title", header.transform, FontSizeHeader, TextAlignmentOptions.MidlineLeft);
        Stretch(title.rectTransform, 8f, 8f, 4f, 4f);

        GameObject rowsRootGo = CreateRect("BodyPartRows", content, Color.clear);
        rowsRootGo.GetComponent<Image>().raycastTarget = false;
        rowsRootGo.AddComponent<LayoutElement>().preferredHeight = RowHeight * 6f + 8f;
        VerticalLayoutGroup rowsLayout = rowsRootGo.AddComponent<VerticalLayoutGroup>();
        rowsLayout.spacing = 4f;
        rowsLayout.childControlWidth = true;
        rowsLayout.childControlHeight = true;
        rowsLayout.childForceExpandWidth = true;
        rowsLayout.childForceExpandHeight = false;
        RectTransform rowsRoot = rowsRootGo.GetComponent<RectTransform>();

        for (int i = 0; i < 6; i++)
            CreateBodyPartRow(rowsRoot);

        TMP_Text vitals = CreateTmpBlock("Vitals", content, 72f);
        TMP_Text skills = CreateTmpBlock("Skills", content, 72f);

        GameObject debugGo = CreateRect("DebugSeverArmL", content, new Color(0.35f, 0.2f, 0.2f, 1f));
        debugGo.AddComponent<LayoutElement>().preferredHeight = 28f;
        Button debugBtn = debugGo.AddComponent<Button>();
        TMP_Text debugLabel = CreateTmp("Label", debugGo.transform, FontSizeBody, TextAlignmentOptions.Center);
        Stretch(debugLabel.rectTransform, 4f, 4f, 2f, 2f);
        debugLabel.text = "Sever L-Arm";

        UIPlayerStatusDetailPanel detail = CreateDetailPanel(root.transform);

        CreateResizeHandles(root.transform);

        UIPlayerStatusWindow window = root.AddComponent<UIPlayerStatusWindow>();
        window.Wire(
            title,
            rowsRoot,
            vitals,
            skills,
            debugBtn,
            debugLabel,
            detail,
            header.GetComponent<UIWindowDragHandler>());

        detail.Hide();
        return window;
    }

    static void CreateResizeHandles(Transform root)
    {
        CreateResizeHandle(root, "Area_ResizeHandle_Left", WindowResizeEdge.Left,
            new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f),
            new Vector2(ResizeEdgeThickness, 0f));
        CreateResizeHandle(root, "Area_ResizeHandle_Right", WindowResizeEdge.Right,
            new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f),
            new Vector2(ResizeEdgeThickness, 0f));
        CreateResizeHandle(root, "Area_ResizeHandle_Top", WindowResizeEdge.Top,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, ResizeEdgeThickness));
        CreateResizeHandle(root, "Area_ResizeHandle_Bottom", WindowResizeEdge.Bottom,
            new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, ResizeEdgeThickness));
        CreateResizeHandle(root, "Area_ResizeHandle_TopLeft", WindowResizeEdge.TopLeft,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(ResizeCornerSize, ResizeCornerSize));
        CreateResizeHandle(root, "Area_ResizeHandle_TopRight", WindowResizeEdge.TopRight,
            new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(ResizeCornerSize, ResizeCornerSize));
        CreateResizeHandle(root, "Area_ResizeHandle_BottomLeft", WindowResizeEdge.BottomLeft,
            new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f),
            new Vector2(ResizeCornerSize, ResizeCornerSize));
        CreateResizeHandle(root, "Area_ResizeHandle_BottomRight", WindowResizeEdge.BottomRight,
            new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f),
            new Vector2(ResizeCornerSize, ResizeCornerSize));
    }

    static void CreateResizeHandle(
        Transform parent,
        string name,
        WindowResizeEdge edge,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 sizeDelta)
    {
        GameObject handle = CreateRect(name, parent, ResizeHandleColor);
        RectTransform rect = handle.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = sizeDelta;

        UIWindowResizeHandler resizeHandler = handle.AddComponent<UIWindowResizeHandler>();
        resizeHandler.SetEdge(edge);
    }

    public static UIPlayerStatusBodyPartRow CreateBodyPartRow(Transform parent)
    {
        GameObject row = CreateRect("BodyPartRow", parent, RowColor);
        row.AddComponent<LayoutElement>().preferredHeight = RowHeight;
        HorizontalLayoutGroup h = row.AddComponent<HorizontalLayoutGroup>();
        h.padding = new RectOffset(6, 6, 2, 2);
        h.spacing = 6f;
        h.childAlignment = TextAnchor.MiddleLeft;
        h.childControlWidth = true;
        h.childControlHeight = true;
        h.childForceExpandWidth = false;
        h.childForceExpandHeight = true;

        TMP_Text name = CreateTmp("Name", row.transform, FontSizeBody, TextAlignmentOptions.MidlineLeft);
        name.gameObject.AddComponent<LayoutElement>().preferredWidth = 72f;

        GameObject barBg = CreateRect("BarBg", row.transform, FillBgColor);
        barBg.AddComponent<LayoutElement>().flexibleWidth = 1f;
        Image fill = CreateRect("Fill", barBg.transform, FillColor).GetComponent<Image>();
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        fill.fillAmount = 1f;
        Stretch(fill.rectTransform, 0f, 0f, 0f, 0f);

        TMP_Text hp = CreateTmp("Hp", row.transform, FontSizeBody, TextAlignmentOptions.MidlineRight);
        hp.gameObject.AddComponent<LayoutElement>().preferredWidth = 72f;

        UIPlayerStatusBodyPartRow view = row.AddComponent<UIPlayerStatusBodyPartRow>();
        view.Wire(name, hp, fill, row.GetComponent<Image>());
        return view;
    }

    static UIPlayerStatusDetailPanel CreateDetailPanel(Transform parent)
    {
        GameObject panel = CreateRect("DetailPanel", parent, DetailColor);
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.sizeDelta = DetailSize;
        rect.anchoredPosition = new Vector2(12f, 0f);

        TMP_Text body = CreateTmp("Body", panel.transform, FontSizeBody, TextAlignmentOptions.TopLeft);
        Stretch(body.rectTransform, 8f, 8f, 8f, 8f);
        body.enableWordWrapping = true;
        body.overflowMode = TextOverflowModes.Overflow;

        UIPlayerStatusDetailPanel view = panel.AddComponent<UIPlayerStatusDetailPanel>();
        view.Wire(body, rect);
        return view;
    }

    static TMP_Text CreateTmpBlock(string name, Transform parent, float height)
    {
        GameObject go = CreateRect(name, parent, Color.clear);
        go.GetComponent<Image>().raycastTarget = false;
        go.AddComponent<LayoutElement>().preferredHeight = height;
        TMP_Text text = CreateTmp("Text", go.transform, FontSizeBody, TextAlignmentOptions.TopLeft);
        Stretch(text.rectTransform, 0f, 0f, 0f, 0f);
        text.enableWordWrapping = true;
        return text;
    }

    static GameObject CreateRect(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        if (parent != null)
            go.transform.SetParent(parent, false);
        go.layer = LayerMask.NameToLayer("UI");
        Image image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = color.a > 0.01f;
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
        return TMP_Settings.defaultFontAsset;
#endif
    }
}
