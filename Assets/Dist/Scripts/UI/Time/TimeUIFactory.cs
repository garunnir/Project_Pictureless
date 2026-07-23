// ============================================================
// TimeUIFactory — 시계 HUD 계층 런타임/에디터 공통 생성
// ============================================================

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class TimeUIFactory
{
    public const string DefaultUIFontPath = "Assets/Dist/Scripts/UI/Font/Katuri SDF.asset";

    public static readonly Vector2 PanelSize = new(200f, 40f);
    public static readonly Vector2 AnchoredPosition = new(0f, -12f);
    public static readonly Vector2 MinPanelSize = new(120f, 28f);
    public static readonly Vector2 MaxPanelSize = new(480f, 96f);
    public const int FontSize = 16;
    public const float HeaderHeight = 16f;
    public const float ResizeEdgeThickness = 8f;
    public const float ResizeCornerSize = 14f;
    public const float ResizeProximityPadding = UIWindowResizeProximity.DefaultProximityPadding;

    static readonly Color PanelColor = new(0.12f, 0.12f, 0.12f, 0.75f);
    static readonly Color HeaderColor = new(0.2f, 0.2f, 0.24f, 0.95f);
    static readonly Color ResizeHandleColor = new(1f, 1f, 1f, 0.85f);

    public static UITimeDisplayPanel CreateDisplayRoot()
    {
        GameObject root = CreateRect("Grp_TimeDisplay", null, PanelColor);
        Image rootImage = root.GetComponent<Image>();
        rootImage.raycastTarget = false;

        RectTransform rootRect = root.GetComponent<RectTransform>();
        // Top-center: avoids PlayerStatus summary (top-right) and launcher (top-left).
        rootRect.anchorMin = new Vector2(0.5f, 1f);
        rootRect.anchorMax = new Vector2(0.5f, 1f);
        rootRect.pivot = new Vector2(0.5f, 1f);
        rootRect.anchoredPosition = AnchoredPosition;
        rootRect.sizeDelta = PanelSize;

        GameObject headerGo = CreateRect("Area_Header", root.transform, HeaderColor);
        RectTransform headerRect = headerGo.GetComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0f, 1f);
        headerRect.anchorMax = new Vector2(1f, 1f);
        headerRect.pivot = new Vector2(0.5f, 1f);
        headerRect.anchoredPosition = Vector2.zero;
        headerRect.sizeDelta = new Vector2(0f, HeaderHeight);

        CanvasGroup headerGroup = headerGo.AddComponent<CanvasGroup>();
        headerGroup.alpha = 0f;
        headerGroup.blocksRaycasts = false;
        headerGroup.interactable = false;
        headerGo.GetComponent<Image>().raycastTarget = false;

        UIWindowDragHandler dragHandler = headerGo.AddComponent<UIWindowDragHandler>();
        dragHandler.SetRevealedAlpha(UIWindowDragHandler.DefaultRevealedAlpha);

        TMP_Text timeText = CreateTmp(
            "Txt_Time",
            root.transform,
            FontSize,
            TextAlignmentOptions.Center);
        Stretch(timeText.rectTransform, 8f, 8f, HeaderHeight + 2f, 4f);
        timeText.text = TimeDisplayFormat.Format(1, 0, 0);

        CreateResizeHandles(root.transform);
        UIWindowResizeProximity proximity = root.AddComponent<UIWindowResizeProximity>();
        proximity.SetDragHeader(dragHandler);

        UITimeDisplayPanel panel = root.AddComponent<UITimeDisplayPanel>();
        panel.Wire(timeText, dragHandler, proximity);
        return panel;
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

        CanvasGroup group = handle.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;
        group.interactable = false;
        handle.GetComponent<Image>().raycastTarget = false;

        UIWindowResizeHandler resizeHandler = handle.AddComponent<UIWindowResizeHandler>();
        resizeHandler.SetEdge(edge);
        resizeHandler.SetRevealedAlpha(UIWindowResizeHandler.DefaultRevealedAlpha);
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
