// ============================================================
// PlayerStatusUIFactory — 상태창 UI 계층 런타임/에디터 공통 생성
// ============================================================

using Garunnir.Runtime.Gameplay.Data;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class PlayerStatusUIFactory
{
    public const string DefaultUIFontPath = "Assets/Dist/Scripts/UI/Font/Katuri SDF.asset";
    const string BodyPartSpriteFolder =
        "Assets/Dist/Visual/Sprites/UI/PlayerStatus/";
    const string HeadSpritePath = BodyPartSpriteFolder + "PlayerStatus_Head.png";
    const string TorsoSpritePath = BodyPartSpriteFolder + "PlayerStatus_Torso.png";
    const string ArmLSpritePath = BodyPartSpriteFolder + "PlayerStatus_ArmL.png";
    const string ArmRSpritePath = BodyPartSpriteFolder + "PlayerStatus_ArmR.png";
    const string LegLSpritePath = BodyPartSpriteFolder + "PlayerStatus_LegL.png";
    const string LegRSpritePath = BodyPartSpriteFolder + "PlayerStatus_LegR.png";
    const string MoodCatalogAssetPath =
        "Assets/Dist/SOData/Gameplay/PlayerStatus/PlayerStatusMoodIconCatalog.asset";
    const string OutlineSpriteFolder = "Assets/Dist/Visual/Sprites/UI/Outline/";
    const string MoodFillSpritePath = OutlineSpriteFolder + "fill.png";
    const string MoodMaskSpritePath = OutlineSpriteFolder + "outlineMask.png";
    const string MoodOutlineSpritePath = OutlineSpriteFolder + "outline.png";

    public static readonly Vector2 SummaryPanelSize = new(240f, 40f);
    public static readonly Vector2 WindowSize = new(360f, 480f);
    public static readonly Vector2 MoodSlotSize = new(32f, 32f);
    public const float MoodSlotSpacing = 4f;
    public const float MoodFrontInset = 4f;
    public static readonly Vector2 DetailSize = new(240f, 320f);
    static readonly Vector2 BodyDiagramSize = new(147f, 220f);
    public const float RowHeight = 28f;
    public const float HeaderHeight = 32f;
    public const float ResizeEdgeThickness = 8f;
    public const int FontSizeBody = 14;
    public const int FontSizeHeader = 18;

    static readonly Color PanelColor = new(0.12f, 0.12f, 0.12f, 0.95f);
    static readonly Color RowColor = new(0.18f, 0.18f, 0.18f, 1f);
    static readonly Color FillColor = new(0.55f, 0.2f, 0.2f, 1f);
    static readonly Color FillBgColor = new(0.08f, 0.08f, 0.08f, 1f);
    static readonly Color DetailColor = new(0.1f, 0.12f, 0.16f, 0.98f);

    static readonly BodyPartGraphicSpec[] BodyPartGraphicSpecs =
    {
        new(
            BodyPartIds.Torso,
            "Img_Torso",
            TorsoSpritePath,
            new Vector2(0.35f, 0.35f),
            new Vector2(0.65f, 0.74f)),
        new(
            BodyPartIds.LegR,
            "Img_LegR",
            LegRSpritePath,
            new Vector2(0.24f, 0.01f),
            new Vector2(0.48f, 0.35f)),
        new(
            BodyPartIds.LegL,
            "Img_LegL",
            LegLSpritePath,
            new Vector2(0.52f, 0.01f),
            new Vector2(0.76f, 0.35f)),
        new(
            BodyPartIds.ArmR,
            "Img_ArmR",
            ArmRSpritePath,
            new Vector2(0.12f, 0.42f),
            new Vector2(0.34f, 0.74f)),
        new(
            BodyPartIds.ArmL,
            "Img_ArmL",
            ArmLSpritePath,
            new Vector2(0.66f, 0.42f),
            new Vector2(0.88f, 0.74f)),
        new(
            BodyPartIds.Head,
            "Img_Head",
            HeadSpritePath,
            new Vector2(0.36f, 0.78f),
            new Vector2(0.64f, 0.99f))
    };

    public static UICharacterWindow CreateWindowRoot()
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
        UIWindowDragHandler dragHandler = header.AddComponent<UIWindowDragHandler>();
        dragHandler.Initialize(root.GetComponent<RectTransform>(), null);
        TMP_Text title = CreateTmp("Title", header.transform, FontSizeHeader, TextAlignmentOptions.MidlineLeft);
        Stretch(title.rectTransform, 8f, 8f, 4f, 4f);

        RectTransform bodyDiagramRoot = CreateBodyPartDiagram(content);

        TMP_Text vitals = CreateTmpBlock("Vitals", content, 72f);
        TMP_Text skills = CreateTmpBlock("Skills", content, 72f);

        GameObject debugGo = CreateRect("DebugSeverArmL", content, new Color(0.35f, 0.2f, 0.2f, 1f));
        debugGo.AddComponent<LayoutElement>().preferredHeight = 28f;
        Button debugBtn = debugGo.AddComponent<Button>();
        TMP_Text debugLabel = CreateTmp("Label", debugGo.transform, FontSizeBody, TextAlignmentOptions.Center);
        Stretch(debugLabel.rectTransform, 4f, 4f, 2f, 2f);
        debugLabel.text = "Sever L-Arm";

        UIPlayerStatusDetailPanel detail = CreateDetailPanel(root.transform);

        UIWindowResizeHandles resizeHandles = root.AddComponent<UIWindowResizeHandles>();
        resizeHandles.SetHandleWidth(ResizeEdgeThickness);
        resizeHandles.Initialize(
            root.GetComponent<RectTransform>(),
            null,
            new Vector2(PlayerStatusWindowLayout.MinWidth, PlayerStatusWindowLayout.MinHeight),
            PlayerStatusWindowLayout.GetMaxSize(null));

        UICharacterWindow window = root.AddComponent<UICharacterWindow>();
        window.Wire(
            title,
            bodyDiagramRoot,
            vitals,
            skills,
            debugBtn,
            debugLabel,
            detail,
            dragHandler);

        detail.Hide();
        return window;
    }

    public static UIPlayerStatusSummaryPanel CreateSummaryRoot()
    {
        GameObject root = CreateRect("Grp_PlayerStatusSummary", null, Color.clear);
        root.GetComponent<Image>().raycastTarget = false;
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(1f, 1f);
        rootRect.anchorMax = new Vector2(1f, 1f);
        rootRect.pivot = new Vector2(1f, 1f);
        rootRect.anchoredPosition = new Vector2(-12f, -12f);
        rootRect.sizeDelta = SummaryPanelSize;

        GameObject slotRootGo = CreateRect("SlotRoot", root.transform, Color.clear);
        slotRootGo.GetComponent<Image>().raycastTarget = false;
        RectTransform slotRoot = slotRootGo.GetComponent<RectTransform>();
        Stretch(slotRoot, 0f, 0f, 0f, 0f);
        HorizontalLayoutGroup hlg = slotRootGo.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = MoodSlotSpacing;
        hlg.childAlignment = TextAnchor.MiddleRight;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        UIPlayerStatusMoodIconSlot slotPrefab = CreateMoodIconSlot(slotRoot, "SlotTemplate");
        slotPrefab.gameObject.SetActive(false);

        GameObject tooltipGo = CreateRect("Tooltip", root.transform, new Color(0.08f, 0.08f, 0.08f, 0.95f));
        RectTransform tooltipRect = tooltipGo.GetComponent<RectTransform>();
        tooltipRect.anchorMin = new Vector2(1f, 1f);
        tooltipRect.anchorMax = new Vector2(1f, 1f);
        tooltipRect.pivot = new Vector2(1f, 0f);
        tooltipRect.sizeDelta = new Vector2(180f, 28f);
        tooltipRect.anchoredPosition = new Vector2(0f, 8f);
        tooltipGo.SetActive(false);

        TMP_Text tooltipText = CreateTmp("Text", tooltipGo.transform, FontSizeBody, TextAlignmentOptions.Center);
        Stretch(tooltipText.rectTransform, 6f, 6f, 4f, 4f);
        tooltipText.enableWordWrapping = true;

        UIPlayerStatusSummaryPanel panel = root.AddComponent<UIPlayerStatusSummaryPanel>();
        panel.Wire(
            slotRoot,
            slotPrefab,
            LoadMoodCatalog(),
            tooltipRect,
            tooltipText);

        root.SetActive(false);
        return panel;
    }

    static UIPlayerStatusMoodIconSlot CreateMoodIconSlot(Transform parent, string name)
    {
        GameObject slotGo = CreateRect(name, parent, Color.clear);
        slotGo.GetComponent<Image>().raycastTarget = true;
        LayoutElement layout = slotGo.AddComponent<LayoutElement>();
        layout.preferredWidth = MoodSlotSize.x;
        layout.preferredHeight = MoodSlotSize.y;

        GameObject shakeGo = CreateRect("ShakeRoot", slotGo.transform, Color.clear);
        shakeGo.GetComponent<Image>().raycastTarget = false;
        RectTransform shakeRoot = shakeGo.GetComponent<RectTransform>();
        Stretch(shakeRoot, 0f, 0f, 0f, 0f);

        Image maskImage = CreateRect("Img_Mask", shakeRoot, Color.white).GetComponent<Image>();
        maskImage.raycastTarget = false;
        maskImage.sprite = LoadUiSprite(MoodMaskSpritePath);
        maskImage.preserveAspect = true;
        Stretch(maskImage.rectTransform, 0f, 0f, 0f, 0f);
        Mask mask = maskImage.gameObject.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        Image fill = CreateRect("img_Fill", maskImage.transform, Color.white).GetComponent<Image>();
        fill.raycastTarget = false;
        fill.sprite = LoadUiSprite(MoodFillSpritePath);
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        fill.fillAmount = 1f;
        Stretch(fill.rectTransform, 0f, 0f, 0f, 0f);

        Image icon = CreateRect("Img_Icon", maskImage.transform, Color.white).GetComponent<Image>();
        icon.raycastTarget = false;
        icon.preserveAspect = true;
        Stretch(
            icon.rectTransform,
            MoodFrontInset,
            MoodFrontInset,
            MoodFrontInset,
            MoodFrontInset);

        Image outline = CreateRect("Img_Outline", shakeRoot, Color.white).GetComponent<Image>();
        outline.raycastTarget = false;
        outline.sprite = LoadUiSprite(MoodOutlineSpritePath);
        outline.preserveAspect = true;
        Stretch(outline.rectTransform, 0f, 0f, 0f, 0f);

        UIPlayerStatusMoodIconSlot slot = slotGo.AddComponent<UIPlayerStatusMoodIconSlot>();
        slot.Wire(fill, icon, shakeRoot);
        return slot;
    }

    static RectTransform CreateBodyPartDiagram(Transform parent)
    {
        GameObject area = CreateRect("Area_BodyDiagram", parent, Color.clear);
        area.GetComponent<Image>().raycastTarget = false;
        area.AddComponent<LayoutElement>().preferredHeight = BodyDiagramSize.y;

        var canvasGo = new GameObject("BodyDiagramCanvas", typeof(RectTransform));
        canvasGo.transform.SetParent(area.transform, false);
        canvasGo.layer = LayerMask.NameToLayer("UI");
        RectTransform canvas = canvasGo.GetComponent<RectTransform>();
        canvas.anchorMin = new Vector2(0.5f, 0.5f);
        canvas.anchorMax = new Vector2(0.5f, 0.5f);
        canvas.pivot = new Vector2(0.5f, 0.5f);
        canvas.anchoredPosition = Vector2.zero;
        canvas.sizeDelta = BodyDiagramSize;

        for (int i = 0; i < BodyPartGraphicSpecs.Length; i++)
            CreateBodyPartGraphic(canvas, BodyPartGraphicSpecs[i]);

        return area.GetComponent<RectTransform>();
    }

    static void CreateBodyPartGraphic(
        RectTransform canvas,
        BodyPartGraphicSpec spec)
    {
        GameObject visualGo = CreateRect(spec.VisualName, canvas, Color.white);
        Image visual = visualGo.GetComponent<Image>();
        visual.sprite = LoadBodyPartSprite(spec.SpritePath);
        visual.preserveAspect = true;
        visual.raycastTarget = false;
        Stretch(visual.rectTransform, 0f, 0f, 0f, 0f);

        GameObject hitGo = CreateRect(
            spec.VisualName.Replace("Img_", "Hit_"),
            canvas,
            Color.clear);
        Image hitImage = hitGo.GetComponent<Image>();
        hitImage.raycastTarget = true;
        RectTransform hitRect = hitGo.GetComponent<RectTransform>();
        hitRect.anchorMin = spec.HitAnchorMin;
        hitRect.anchorMax = spec.HitAnchorMax;
        hitRect.offsetMin = Vector2.zero;
        hitRect.offsetMax = Vector2.zero;

        UIPlayerStatusBodyPartGraphic graphic =
            hitGo.AddComponent<UIPlayerStatusBodyPartGraphic>();
        graphic.Wire(visual, spec.PartId);
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

        TMP_Text condition = CreateTmp(
            "Condition",
            row.transform,
            FontSizeBody,
            TextAlignmentOptions.MidlineRight);
        condition.gameObject.AddComponent<LayoutElement>().preferredWidth = 72f;

        UIPlayerStatusBodyPartRow view = row.AddComponent<UIPlayerStatusBodyPartRow>();
        view.Wire(name, condition, fill, row.GetComponent<Image>());
        return view;
    }

    static UIPlayerStatusDetailPanel CreateDetailPanel(Transform parent)
    {
        GameObject panel = CreateRect("DetailPanel", parent, DetailColor);
        RectTransform rect = panel.GetComponent<RectTransform>();
        // Hover Placement SSOT: center anchors so UIPopupPositioner local coords match.
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0f, 1f);
        rect.sizeDelta = DetailSize;
        rect.anchoredPosition = Vector2.zero;

        Image panelImage = panel.GetComponent<Image>();
        if (panelImage != null)
            panelImage.raycastTarget = false;

        TMP_Text body = CreateTmp("Body", panel.transform, FontSizeBody, TextAlignmentOptions.TopLeft);
        Stretch(body.rectTransform, 8f, 8f, 8f, 8f);
        body.enableWordWrapping = true;
        body.overflowMode = TextOverflowModes.Overflow;
        body.raycastTarget = false;

        UIPlayerStatusDetailPanel view = panel.AddComponent<UIPlayerStatusDetailPanel>();
        view.Wire(body);
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

    static Sprite LoadBodyPartSprite(string path) => LoadUiSprite(path);

    static Sprite LoadUiSprite(string path)
    {
#if UNITY_EDITOR
        Sprite sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
            Debug.LogError($"[PlayerStatusUIFactory] Sprite not found: {path}");
        return sprite;
#else
        return null;
#endif
    }

    static PlayerStatusMoodIconCatalog LoadMoodCatalog()
    {
#if UNITY_EDITOR
        return UnityEditor.AssetDatabase.LoadAssetAtPath<PlayerStatusMoodIconCatalog>(MoodCatalogAssetPath);
#else
        return null;
#endif
    }

    readonly struct BodyPartGraphicSpec
    {
        public readonly string PartId;
        public readonly string VisualName;
        public readonly string SpritePath;
        public readonly Vector2 HitAnchorMin;
        public readonly Vector2 HitAnchorMax;

        public BodyPartGraphicSpec(
            string partId,
            string visualName,
            string spritePath,
            Vector2 hitAnchorMin,
            Vector2 hitAnchorMax)
        {
            PartId = partId;
            VisualName = visualName;
            SpritePath = spritePath;
            HitAnchorMin = hitAnchorMin;
            HitAnchorMax = hitAnchorMax;
        }
    }
}
