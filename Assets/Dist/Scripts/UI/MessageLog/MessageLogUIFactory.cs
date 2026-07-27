// ============================================================
// MessageLogUIFactory — 메시지 로그 HUD 계층 생성 (프리팹 작성용)
// ============================================================

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class MessageLogUIFactory
{
    public const string DefaultUIFontPath = "Assets/Dist/Scripts/UI/Font/Katuri SDF.asset";

    public static readonly Vector2 PanelSize = new(420f, 120f);
    public static readonly Vector2 AnchoredPosition = new(12f, 12f);
    public const int FontSize = 13;
    public const float LineSpacing = -4f;

    static readonly Color PanelColor = new(0.08f, 0.08f, 0.1f, 0.72f);

    public static UIMessageLogPanel CreateDisplayRoot()
    {
        GameObject root = CreateRect("Hud_MessageLog", null, PanelColor);
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0f, 0f);
        rootRect.anchorMax = new Vector2(0f, 0f);
        rootRect.pivot = new Vector2(0f, 0f);
        rootRect.anchoredPosition = AnchoredPosition;
        rootRect.sizeDelta = PanelSize;

        GameObject viewport = CreateRect("Viewport", root.transform, new Color(1f, 1f, 1f, 0.02f));
        RectTransform viewportRect = viewport.GetComponent<RectTransform>();
        Stretch(viewportRect, 6f, 6f, 6f, 6f);
        Mask mask = viewport.AddComponent<Mask>();
        mask.showMaskGraphic = false;
        Image viewportImage = viewport.GetComponent<Image>();
        viewportImage.raycastTarget = true;

        GameObject content = new GameObject(
            "Content",
            typeof(RectTransform),
            typeof(ContentSizeFitter));
        content.transform.SetParent(viewport.transform, false);
        content.layer = LayerMask.NameToLayer("UI");
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 0f);
        contentRect.anchorMax = new Vector2(1f, 0f);
        contentRect.pivot = new Vector2(0.5f, 0f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0f, 0f);
        ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        TMP_Text logText = CreateTmp(
            "Txt_MessageLog",
            content.transform,
            FontSize,
            TextAlignmentOptions.BottomLeft);
        Stretch(logText.rectTransform, 0f, 0f, 0f, 0f);
        logText.rectTransform.anchorMin = new Vector2(0f, 0f);
        logText.rectTransform.anchorMax = new Vector2(1f, 0f);
        logText.rectTransform.pivot = new Vector2(0.5f, 0f);
        logText.textWrappingMode = TextWrappingModes.Normal;
        logText.overflowMode = TextOverflowModes.Overflow;
        logText.richText = true;
        logText.lineSpacing = LineSpacing;
        logText.text = string.Empty;

        ContentSizeFitter textFitter = logText.gameObject.AddComponent<ContentSizeFitter>();
        textFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        textFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scroll = root.AddComponent<ScrollRect>();
        scroll.viewport = viewportRect;
        scroll.content = contentRect;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 24f;

        UIMessageLogPanel panel = root.AddComponent<UIMessageLogPanel>();
        panel.Wire(scroll, logText);
        return panel;
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

    static TMP_Text CreateTmp(
        string name,
        Transform parent,
        float fontSize,
        TextAlignmentOptions align)
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
