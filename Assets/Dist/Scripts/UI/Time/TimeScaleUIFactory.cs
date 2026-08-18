// ============================================================
// TimeScaleUIFactory — 배속 HUD 우상단 스트립 생성 (프리팹/MCP용)
// ============================================================

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class TimeScaleUIFactory
{
    public const string DefaultUIFontPath = TimeUIFactory.DefaultUIFontPath;

    public static UITimeScaleHudPanel CreateHudRoot()
    {
        GameObject root = CreateRect("Hud_TimeScale", null, TimeScaleHudLayout.PanelColor);
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(1f, 1f);
        rootRect.anchorMax = new Vector2(1f, 1f);
        rootRect.pivot = new Vector2(1f, 1f);
        rootRect.anchoredPosition = TimeScaleHudLayout.AnchoredPosition;
        rootRect.sizeDelta = TimeScaleHudLayout.PanelSize;

        GameObject headerGo = CreateRect("Area_Header", root.transform, new Color(0.2f, 0.2f, 0.24f, 0.95f));
        RectTransform headerRect = headerGo.GetComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0f, 1f);
        headerRect.anchorMax = new Vector2(1f, 1f);
        headerRect.pivot = new Vector2(0.5f, 1f);
        headerRect.sizeDelta = new Vector2(0f, TimeUIFactory.HeaderHeight);
        CanvasGroup headerGroup = headerGo.AddComponent<CanvasGroup>();
        headerGroup.alpha = 0f;
        headerGroup.blocksRaycasts = false;
        headerGo.GetComponent<Image>().raycastTarget = false;

        UIWindowDragHandler headerDrag = headerGo.AddComponent<UIWindowDragHandler>();
        headerDrag.Initialize(rootRect, null);
        headerDrag.SetProximityRevealEnabled(false);

        GameObject layoutHitGo = CreateRect("Area_LayoutHit", root.transform, Color.clear);
        Stretch(layoutHitGo.GetComponent<RectTransform>(), 0f, 0f, 0f, 0f);
        layoutHitGo.GetComponent<Image>().raycastTarget = false;
        CanvasGroup layoutGroup = layoutHitGo.AddComponent<CanvasGroup>();
        layoutGroup.alpha = 0f;
        layoutGroup.blocksRaycasts = false;
        layoutGroup.interactable = false;
        UIWindowDragHandler layoutDrag = layoutHitGo.AddComponent<UIWindowDragHandler>();
        layoutDrag.Initialize(rootRect, null);
        layoutDrag.SetVisualActive(false);

        GameObject stripGo = CreateRect("Area_ButtonStrip", root.transform, Color.clear);
        RectTransform stripRect = stripGo.GetComponent<RectTransform>();
        Stretch(stripRect, 4f, 4f, TimeUIFactory.HeaderHeight + 2f, 4f);
        stripGo.GetComponent<Image>().raycastTarget = false;
        HorizontalLayoutGroup hlg = stripGo.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = TimeScaleHudLayout.ButtonSpacing;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        Button pauseBtn = CreateModeButton(stripGo.transform, "Btn_Pause");
        Button normalBtn = CreateModeButton(stripGo.transform, "Btn_Normal");
        Button doubleBtn = CreateModeButton(stripGo.transform, "Btn_Double");
        Button smartBtn = CreateModeButton(stripGo.transform, "Btn_Smart");

        UIWindowResizeHandles resizeHandles = root.AddComponent<UIWindowResizeHandles>();
        resizeHandles.SetHandleWidth(TimeUIFactory.ResizeEdgeThickness);
        resizeHandles.SetProximityReveal(true);
        resizeHandles.Initialize(
            rootRect,
            null,
            TimeScaleHudLayout.PanelSize,
            TimeUIFactory.MaxPanelSize);

        UIWindowResizeProximity proximity = root.AddComponent<UIWindowResizeProximity>();
        proximity.SetDragHeader(headerDrag);
        proximity.SetResizeHandlers(resizeHandles.Handlers);
        proximity.SetProximityEnabled(false);
        proximity.SetResizeHandlesActive(false);

        root.AddComponent<UIOverlayWindow>();

        HudLayoutParticipant participant = root.AddComponent<HudLayoutParticipant>();
        participant.Wire(
            TimeScaleHudLayout.ParticipantId,
            headerDrag,
            layoutDrag,
            null,
            resizeHandles,
            proximity,
            null);

        UITimeScaleHudPanel panel = root.AddComponent<UITimeScaleHudPanel>();
        panel.Wire(pauseBtn, normalBtn, doubleBtn, smartBtn, headerDrag, layoutDrag, resizeHandles, proximity);
        return panel;
    }

    static Button CreateModeButton(Transform parent, string name)
    {
        GameObject go = CreateRect(name, parent, TimeScaleHudLayout.NormalColor);
        LayoutElement layout = go.AddComponent<LayoutElement>();
        layout.preferredWidth = TimeScaleHudLayout.ButtonSize;
        layout.preferredHeight = TimeScaleHudLayout.ButtonSize;
        go.GetComponent<Image>().raycastTarget = true;

        Button button = go.AddComponent<Button>();
        TMP_Text label = CreateTmp("Label", go.transform, TimeScaleHudLayout.FontSize, TextAlignmentOptions.Center);
        Stretch(label.rectTransform, 2f, 2f, 0f, 0f);
        return button;
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
        return DistUiFont.Get();
#endif
    }
}
