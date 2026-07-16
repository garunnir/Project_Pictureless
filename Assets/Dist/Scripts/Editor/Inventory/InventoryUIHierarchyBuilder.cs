// ============================================================
// InventoryUIHierarchyBuilder — 인벤 UI 프리팹 계층 생성 (Editor 전용)
// ============================================================

#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

static class InventoryUIHierarchyBuilder
{
    internal const string PrefabFolder = "Assets/Dist/Visual/Prefabs/UIComponents/Inventory";
    internal const string DefaultUIFontPath = "Assets/Dist/Scripts/UI/Font/Katuri SDF.asset";

    static readonly Color PanelColor = new(0.12f, 0.12f, 0.12f, 0.95f);
    static readonly Color RowColor = new(0.18f, 0.18f, 0.18f, 1f);
    static readonly Color SlotColor = new(0.22f, 0.22f, 0.22f, 1f);
    static readonly Color HighlightColor = new(0.35f, 0.45f, 0.35f, 0.9f);

    public static UIItemListRow BuildRowPrefabRoot()
    {
        InventoryUIPrefabStyleSpec spec = InventoryUIPrefabStyleSpec.Default;
        var row = CreateRect("Grp_ItemListRow", null, RowColor);
        var rowRect = row.GetComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0f, 0.5f);
        rowRect.anchorMax = new Vector2(1f, 0.5f);
        rowRect.pivot = new Vector2(0.5f, 0.5f);
        rowRect.sizeDelta = new Vector2(0f, spec.RowHeight);
        var layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(spec.RowPaddingH, spec.RowPaddingH, spec.RowPaddingV, spec.RowPaddingV);
        layout.spacing = spec.RowSpacing;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        row.AddComponent<LayoutElement>().preferredHeight = spec.RowHeight;

        Image icon = CreateIcon("Icon", row.transform, spec.RowIconSize);
        var category = CreateTmp("Category", row.transform, spec.RowCategoryWidth, spec.RowFontCategory);
        var name = CreateTmp("Name", row.transform, 0f, spec.RowFontName, flexibleWidth: true);
        var detail = CreateTmp("Detail", row.transform, spec.RowDetailWidth, spec.RowFontDetail);

        var rowView = row.AddComponent<UIItemListRow>();
        SetReference(rowView, "_backgroundImage", row.GetComponent<Image>());
        SetReference(rowView, "_iconImage", icon);
        SetReference(rowView, "_categoryText", category);
        SetReference(rowView, "_nameText", name);
        SetReference(rowView, "_detailText", detail);
        return rowView;
    }

    public static UIContainerSlot BuildSlotPrefabRoot()
    {
        InventoryUIPrefabStyleSpec spec = InventoryUIPrefabStyleSpec.Default;
        var slot = CreateRect("Grp_ContainerSlot", null, SlotColor);
        slot.AddComponent<LayoutElement>().preferredHeight = spec.SlotHeight;
        var button = slot.AddComponent<Button>();
        var highlight = CreateRect("Highlight", slot.transform, HighlightColor);
        Stretch(highlight.GetComponent<RectTransform>(), 0f, 0f, 0f, 0f);
        highlight.SetActive(false);
        float inset = spec.SlotLabelInset;
        // Icon fills the slot (no layout group on slot) — sprite bound at runtime by UIContainerSlot.
        var iconGo = CreateRect("Icon", slot.transform, Color.white);
        Stretch(iconGo.GetComponent<RectTransform>(), inset, inset, inset, inset);
        var icon = iconGo.GetComponent<Image>();
        icon.raycastTarget = false;
        icon.preserveAspect = true;
        icon.enabled = false;
        var label = CreateTmp("Label", slot.transform, 0f, spec.SlotFontSize);
        Stretch(label.rectTransform, inset, inset, inset, inset);

        var slotView = slot.AddComponent<UIContainerSlot>();
        SetReference(slotView, "_button", button);
        SetReference(slotView, "_label", label);
        SetReference(slotView, "_iconImage", icon);
        SetReference(slotView, "_highlight", highlight.GetComponent<Image>());
        return slotView;
    }

    public static UIInventoryDragGhost BuildDragGhostRoot(Canvas rootCanvas)
    {
        // Icon Image must be opaque white — transparent CreateRect color hides sprites.
        var go = CreateRect("InventoryDragGhost", null, Color.white);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(40f, 40f);

        Image icon = go.GetComponent<Image>();
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        icon.sprite = ItemVisualPresenter.GetDefaultIcon();
        icon.enabled = icon.sprite != null;

        var labelGo = new GameObject("Count", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        labelGo.transform.SetParent(go.transform, false);
        labelGo.layer = LayerMask.NameToLayer("UI");
        var labelRect = labelGo.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(1f, 1f);
        labelRect.anchorMax = new Vector2(1f, 1f);
        labelRect.pivot = new Vector2(0f, 0f);
        labelRect.anchoredPosition = new Vector2(4f, -4f);
        labelRect.sizeDelta = new Vector2(48f, 20f);

        TMP_Text label = labelGo.GetComponent<TextMeshProUGUI>();
        label.font = LoadDefaultFont();
        label.fontSize = 12f;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.BottomRight;
        label.raycastTarget = false;

        var ghost = go.AddComponent<UIInventoryDragGhost>();
        ghost.Initialize(icon, label, rootCanvas);
        go.SetActive(false);
        return ghost;
    }

    public static GameObject BuildScrollDragOverlayRoot()
    {
        var overlay = CreateRect("InventoryScrollDragOverlay", null, new Color(0f, 0f, 0f, 0f));
        Stretch(overlay.GetComponent<RectTransform>(), 0f, 0f, 0f, 0f);
        Image image = overlay.GetComponent<Image>();
        image.raycastTarget = true;
        overlay.SetActive(false);
        return overlay;
    }

    public static UIItemContextMenu BuildContextMenuRoot(Button buttonPrefab)
    {
        // buttonPrefab는 레거시 시그니처 유지용. 캐스케이드는 내부 Row 템플릿을 쓴다.
        _ = buttonPrefab;

        var root = CreateRect("ItemContextMenu", null, Color.clear);
        var rootRect = root.GetComponent<RectTransform>();
        Stretch(rootRect, 0f, 0f, 0f, 0f);
        root.GetComponent<Image>().raycastTarget = false;

        var panelRootGo = CreateRect("PanelRoot", root.transform, Color.clear);
        var panelRoot = panelRootGo.GetComponent<RectTransform>();
        Stretch(panelRoot, 0f, 0f, 0f, 0f);
        panelRootGo.GetComponent<Image>().raycastTarget = false;

        UIContextMenuItemRow rowTemplate = BuildContextMenuRowTemplate(root.transform);
        UIContextMenuCascadePanel panelTemplate = BuildContextMenuPanelTemplate(root.transform, rowTemplate);

        var menu = root.AddComponent<UIItemContextMenu>();
        SetReference(menu, "_panelRoot", panelRoot);
        SetReference(menu, "_panelPrefab", panelTemplate);
        SetReference(menu, "_rowPrefab", rowTemplate);
        return menu;
    }

    static UIContextMenuItemRow BuildContextMenuRowTemplate(Transform parent)
    {
        var rowGo = CreateRect("RowTemplate", parent, ContextMenuStyle.RowColor);
        var rowRect = rowGo.GetComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0f, 1f);
        rowRect.anchorMax = new Vector2(1f, 1f);
        rowRect.pivot = new Vector2(0.5f, 1f);
        rowRect.sizeDelta = new Vector2(0f, ContextMenuStyle.RowHeight);

        var h = rowGo.AddComponent<HorizontalLayoutGroup>();
        h.padding = new RectOffset(6, 4, 2, 2);
        h.spacing = 4f;
        h.childAlignment = TextAnchor.MiddleLeft;
        h.childControlWidth = true;
        h.childControlHeight = true;
        h.childForceExpandWidth = false;
        h.childForceExpandHeight = false;

        var le = rowGo.AddComponent<LayoutElement>();
        le.preferredHeight = ContextMenuStyle.RowHeight;
        le.flexibleWidth = 1f;
        le.minHeight = ContextMenuStyle.RowHeight;

        var label = CreateTmp("Label", rowGo.transform, 0f, ContextMenuStyle.FontSize, flexibleWidth: true);
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.raycastTarget = false;

        var chevron = CreateTmp("Chevron", rowGo.transform, ContextMenuStyle.ChevronWidth, ContextMenuStyle.FontSize);
        chevron.text = ItemContextMenuLabels.SubmenuChevron;
        chevron.alignment = TextAlignmentOptions.MidlineRight;
        chevron.raycastTarget = false;

        var row = rowGo.AddComponent<UIContextMenuItemRow>();
        SetReference(row, "_background", rowGo.GetComponent<Image>());
        SetReference(row, "_label", label);
        SetReference(row, "_chevron", chevron);
        rowGo.SetActive(false);
        return row;
    }

    static UIContextMenuCascadePanel BuildContextMenuPanelTemplate(Transform parent, UIContextMenuItemRow rowTemplate)
    {
        var panelGo = CreateRect("PanelTemplate", parent, ContextMenuStyle.PanelColor);
        var panelRect = panelGo.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.sizeDelta = new Vector2(ContextMenuStyle.PanelWidth, ContextMenuStyle.RowHeight);

        var layout = panelGo.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(
            (int)ContextMenuStyle.PanelPadding,
            (int)ContextMenuStyle.PanelPadding,
            (int)ContextMenuStyle.PanelPadding,
            (int)ContextMenuStyle.PanelPadding);
        layout.spacing = ContextMenuStyle.RowSpacing;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        var fitter = panelGo.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var le = panelGo.AddComponent<LayoutElement>();
        le.preferredWidth = ContextMenuStyle.PanelWidth;
        le.minWidth = ContextMenuStyle.PanelWidth;

        // Scroll: 내용이 PanelMaxHeight를 넘기면 스크롤
        var scrollGo = CreateRect("Scroll", panelGo.transform, Color.clear);
        scrollGo.GetComponent<Image>().raycastTarget = false;
        var scrollRectTransform = scrollGo.GetComponent<RectTransform>();
        var scrollLe = scrollGo.AddComponent<LayoutElement>();
        scrollLe.preferredWidth = ContextMenuStyle.PanelWidth - ContextMenuStyle.PanelPadding * 2f;
        scrollLe.flexibleWidth = 1f;
        scrollLe.minHeight = ContextMenuStyle.RowHeight;
        scrollLe.preferredHeight = ContextMenuStyle.RowHeight;

        var viewport = CreateRect("Viewport", scrollGo.transform, Color.clear);
        viewport.GetComponent<Image>().raycastTarget = true;
        Stretch(viewport.GetComponent<RectTransform>(), 0f, 0f, 0f, 0f);
        viewport.AddComponent<RectMask2D>();

        var content = CreateRect("Content", viewport.transform, Color.clear);
        content.GetComponent<Image>().raycastTarget = false;
        var contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.sizeDelta = new Vector2(0f, 0f);

        var contentLayout = content.AddComponent<VerticalLayoutGroup>();
        contentLayout.spacing = ContextMenuStyle.RowSpacing;
        contentLayout.childControlHeight = true;
        contentLayout.childControlWidth = true;
        contentLayout.childForceExpandHeight = false;
        contentLayout.childForceExpandWidth = true;
        var contentFitter = content.AddComponent<ContentSizeFitter>();
        contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var scroll = scrollGo.AddComponent<ScrollRect>();
        scroll.viewport = viewport.GetComponent<RectTransform>();
        scroll.content = contentRect;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;

        // preferredHeight는 런타임 Bind 후 ContentSize로 자연 확장; 상한은 ContentSizeFitter 대신
        // LayoutElement max를 쓰지 않고 호스트 Clamp만 사용. 스크롤 영역 높이는 Content 높이와 맞춤.
        var panel = panelGo.AddComponent<UIContextMenuCascadePanel>();
        SetReference(panel, "_root", panelRect);
        SetReference(panel, "_rowContainer", content.transform);
        SetReference(panel, "_rowPrefab", rowTemplate);
        panelGo.SetActive(false);
        return panel;
    }

    public static UIInventoryListWindow BuildWindowRoot(
        UIItemListRow rowPrefab,
        UIContainerSlot slotPrefab)
    {
        InventoryUIPrefabStyleSpec spec = InventoryUIPrefabStyleSpec.Default;
        var root = CreateRect("Grp_InventoryListWindow", null, PanelColor);
        var rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 0.5f);
        rootRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.sizeDelta = spec.WindowSize;
        rootRect.anchoredPosition = Vector2.zero;

        var headerArea = CreateRect("Area_Header", root.transform, new Color(0.16f, 0.16f, 0.16f, 1f));
        var headerRect = headerArea.GetComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0f, 1f);
        headerRect.anchorMax = new Vector2(1f, 1f);
        headerRect.pivot = new Vector2(0.5f, 1f);
        headerRect.anchoredPosition = Vector2.zero;
        headerRect.sizeDelta = new Vector2(0f, spec.HeaderHeight);
        headerArea.AddComponent<InventoryWindowDragHandler>();

        var headerTitle = CreateTmp("Txt_Title", headerArea.transform, 0f, spec.HeaderFontSize);
        headerTitle.text = "Inventory";
        headerTitle.alignment = TextAlignmentOptions.MidlineLeft;
        headerTitle.raycastTarget = false;
        Stretch(headerTitle.rectTransform, spec.ChromeMargin, spec.ChromeMargin, 0f, 0f);

        float listTop = spec.HeaderHeight + spec.ChromeMargin;
        // List right inset + sidebar Image raycast are owned by shared UIInventoryListWindow
        // (ApplyModeLayout / EnsureSidebarRaycastTarget) for Primary and Loot windows alike.
        var listArea = CreateRect("Area_List", root.transform, new Color(0.1f, 0.1f, 0.1f, 1f));
        var listRect = listArea.GetComponent<RectTransform>();
        Stretch(listRect, spec.ChromeMargin, spec.ChromeMargin, listTop, spec.ChromeMargin);

        var sidebarArea = CreateRect("Area_Sidebar", root.transform, new Color(0.08f, 0.08f, 0.08f, 1f));
        var sidebarRect = sidebarArea.GetComponent<RectTransform>();
        sidebarRect.anchorMin = new Vector2(1f, 0f);
        sidebarRect.anchorMax = new Vector2(1f, 1f);
        sidebarRect.pivot = new Vector2(1f, 0.5f);
        sidebarRect.offsetMin = new Vector2(-spec.SidebarWidth, spec.ChromeMargin);
        sidebarRect.offsetMax = new Vector2(-spec.ChromeMargin, -listTop);

        var handleColor = new Color(1f, 1f, 1f, 0.02f);

        CreateResizeHandle(root.transform, "Area_ResizeHandle_Left", handleColor, WindowResizeEdge.Left,
            new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), Vector2.zero,
            new Vector2(spec.EdgeThickness, 0f));
        CreateResizeHandle(root.transform, "Area_ResizeHandle_Right", handleColor, WindowResizeEdge.Right,
            new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), Vector2.zero,
            new Vector2(spec.EdgeThickness, 0f));
        CreateResizeHandle(root.transform, "Area_ResizeHandle_Top", handleColor, WindowResizeEdge.Top,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero,
            new Vector2(0f, spec.EdgeThickness));
        CreateResizeHandle(root.transform, "Area_ResizeHandle_Bottom", handleColor, WindowResizeEdge.Bottom,
            new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), Vector2.zero,
            new Vector2(0f, spec.EdgeThickness));
        CreateResizeHandle(root.transform, "Area_ResizeHandle_TopLeft", handleColor, WindowResizeEdge.TopLeft,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero,
            new Vector2(spec.CornerSize, spec.CornerSize));
        CreateResizeHandle(root.transform, "Area_ResizeHandle_TopRight", handleColor, WindowResizeEdge.TopRight,
            new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), Vector2.zero,
            new Vector2(spec.CornerSize, spec.CornerSize));
        CreateResizeHandle(root.transform, "Area_ResizeHandle_BottomLeft", handleColor, WindowResizeEdge.BottomLeft,
            new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), Vector2.zero,
            new Vector2(spec.CornerSize, spec.CornerSize));
        CreateResizeHandle(root.transform, "Area_ResizeHandle_BottomRight", handleColor, WindowResizeEdge.BottomRight,
            new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), Vector2.zero,
            new Vector2(spec.CornerSize, spec.CornerSize));

        UIItemListView listView = listArea.AddComponent<UIItemListView>();
        ScrollRect scroll = listArea.AddComponent<ScrollRect>();
        var viewport = CreateRect("Viewport", listArea.transform, new Color(1f, 1f, 1f, 0f));
        Stretch(viewport.GetComponent<RectTransform>(), 0f, 0f, 0f, 0f);
        Image viewportImage = viewport.GetComponent<Image>();
        viewportImage.raycastTarget = true;
        viewport.gameObject.AddComponent<RectMask2D>();
        var content = CreateRect("Content", viewport.transform, Color.clear);
        var contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0f, 0f);
        var contentLayout = content.AddComponent<VerticalLayoutGroup>();
        contentLayout.childControlHeight = true;
        contentLayout.childControlWidth = true;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;
        contentLayout.spacing = spec.ContentSpacing;
        int pad = spec.ContentPadding;
        contentLayout.padding = new RectOffset(pad, pad, pad, pad);
        content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        UIInventoryListDropZone dropZone = viewport.AddComponent<UIInventoryListDropZone>();
        InventoryListMarqueeSelector marquee = viewport.AddComponent<InventoryListMarqueeSelector>();
        RectTransform marqueeRect = CreateMarqueeSelectionRect(viewport.transform);
        SetReference(marquee, "_selectionRect", marqueeRect);
        scroll.viewport = viewport.GetComponent<RectTransform>();
        scroll.content = contentRect;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        viewport.AddComponent<InventoryScrollDragHandler>();
        SetReference(listView, "_contentRoot", contentRect);
        SetReference(listView, "_rowPrefab", rowPrefab);

        UIContainerSidebar sidebar = sidebarArea.AddComponent<UIContainerSidebar>();
        var slotRoot = CreateRect("SlotRoot", sidebarArea.transform, Color.clear);
        Stretch(slotRoot.GetComponent<RectTransform>(), 0f, 0f, 0f, 0f);
        var slotLayout = slotRoot.AddComponent<VerticalLayoutGroup>();
        slotLayout.spacing = spec.SidebarSlotSpacing;
        int slotPad = spec.SidebarSlotPadding;
        slotLayout.padding = new RectOffset(slotPad, slotPad, slotPad, slotPad);
        slotLayout.childControlHeight = true;
        slotLayout.childForceExpandHeight = false;
        SetReference(sidebar, "_slotRoot", slotRoot.GetComponent<RectTransform>());
        SetReference(sidebar, "_slotPrefab", slotPrefab);

        var window = root.AddComponent<UIInventoryListWindow>();
        SetReference(window, "_listView", listView);
        SetReference(window, "_sidebar", sidebar);
        SetReference(window, "_listArea", listRect);
        SetReference(window, "_sidebarArea", sidebarRect);
        SetReference(window, "_windowDragHandler", headerArea.GetComponent<InventoryWindowDragHandler>());
        SetReference(window, "_headerTitle", headerTitle);
        return window;
    }

    static RectTransform CreateMarqueeSelectionRect(Transform parent)
    {
        var go = CreateRect("MarqueeSelection", parent, new Color(0.35f, 0.55f, 0.85f, 0.25f));
        var rect = go.GetComponent<RectTransform>();
        rect.pivot = Vector2.zero;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        go.GetComponent<Image>().raycastTarget = false;
        go.SetActive(false);
        return rect;
    }

    static void CreateResizeHandle(
        Transform parent,
        string name,
        Color color,
        WindowResizeEdge edge,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 anchoredPosition,
        Vector2 sizeDelta)
    {
        var handle = CreateRect(name, parent, color);
        var rect = handle.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;

        InventoryWindowResizeHandler resizeHandler = handle.AddComponent<InventoryWindowResizeHandler>();
        SetEnumReference(resizeHandler, "_edge", edge);
    }

    static GameObject CreateRect(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        if (parent != null)
            go.transform.SetParent(parent, false);
        go.layer = LayerMask.NameToLayer("UI");
        go.GetComponent<Image>().color = color;
        return go;
    }

    static TMP_Text CreateTmp(string name, Transform parent, float width, float fontSize, bool flexibleWidth = false)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        go.layer = LayerMask.NameToLayer("UI");
        var text = go.GetComponent<TextMeshProUGUI>();
        text.font = LoadDefaultFont();
        text.fontSize = fontSize;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.enableWordWrapping = false;

        var layout = go.AddComponent<LayoutElement>();
        if (flexibleWidth)
        {
            layout.flexibleWidth = 1f;
            layout.minWidth = 48f;
        }
        else if (width > 0f)
        {
            layout.preferredWidth = width;
        }

        return text;
    }

    static Image CreateIcon(string name, Transform parent, float size)
    {
        var go = CreateRect(name, parent, new Color(0.25f, 0.25f, 0.25f, 1f));
        var layout = go.AddComponent<LayoutElement>();
        layout.preferredWidth = size;
        layout.preferredHeight = size;
        layout.minWidth = size;
        layout.minHeight = size;

        var image = go.GetComponent<Image>();
        image.raycastTarget = false;
        image.preserveAspect = true;
        image.color = Color.white;
        return image;
    }

    static void Stretch(RectTransform rect, float left, float right, float top, float bottom)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
    }

    static void SetEnumReference(Object target, string propertyName, System.Enum value)
    {
        var serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null)
        {
            Debug.LogError($"[InventoryUIHierarchyBuilder] Missing property '{propertyName}' on {target.GetType().Name}.");
            return;
        }

        property.enumValueIndex = System.Convert.ToInt32(value);
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    static void SetReference(Object target, string propertyName, Object value)
    {
        var serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null)
        {
            Debug.LogError($"[InventoryUIHierarchyBuilder] Missing property '{propertyName}' on {target.GetType().Name}.");
            return;
        }

        property.objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    static TMP_FontAsset LoadDefaultFont()
    {
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(DefaultUIFontPath);
        if (font == null)
            Debug.LogError($"[InventoryUIHierarchyBuilder] Default UI font not found: {DefaultUIFontPath}");

        return font;
    }
}
#endif
