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
    internal const string DefaultUIFontPath = "Assets/Dist/Scripts/UI/Font/Galmuri-v2.40.3/Galmuri7 SDF.asset";
    const string ContextMenuRowIconChildName = "Img_Icon";

    static readonly Color PanelColor = new(0.12f, 0.12f, 0.12f, 0.95f);
    static readonly Color RowColor = new(0.18f, 0.18f, 0.18f, 1f);
    static readonly Color SlotColor = new(0.22f, 0.22f, 0.22f, 1f);
    static readonly Color HighlightColor = new(0.35f, 0.45f, 0.35f, 0.9f);

    public static UIItemListRow BuildRowPrefabRoot()
    {
        // Layout SSOT: Grp_ItemListRow hand layout (HLG pad 8/8/2/2, spacing 4, category 100, icon wrapper 32)
        // + count / weight value+unit / volume value+unit columns.
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

        Image icon = CreateRowIcon(row.transform, spec.RowIconSize);
        var category = CreateTmp("Category", row.transform, spec.RowCategoryWidth, spec.RowFontCategory);
        var name = CreateTmp("Name", row.transform, 0f, spec.RowFontName, flexibleWidth: true);
        var count = CreateTmp(
            "Count", row.transform, spec.RowCountWidth, spec.RowFontDetail,
            alignment: TextAlignmentOptions.MidlineRight);
        var weightValue = CreateTmp(
            "WeightValue", row.transform, spec.RowWeightValueWidth, spec.RowFontDetail,
            alignment: TextAlignmentOptions.MidlineRight);
        var weightUnit = CreateTmp(
            "WeightUnit", row.transform, spec.RowWeightUnitWidth, spec.RowFontDetail,
            alignment: TextAlignmentOptions.MidlineLeft);
        var volumeValue = CreateTmp(
            "VolumeValue", row.transform, spec.RowVolumeValueWidth, spec.RowFontDetail,
            alignment: TextAlignmentOptions.MidlineRight);
        var volumeUnit = CreateTmp(
            "VolumeUnit", row.transform, spec.RowVolumeUnitWidth, spec.RowFontDetail,
            alignment: TextAlignmentOptions.MidlineLeft);

        var rowView = row.AddComponent<UIItemListRow>();
        SetReference(rowView, "_backgroundImage", row.GetComponent<Image>());
        SetReference(rowView, "_iconImage", icon);
        SetReference(rowView, "_categoryText", category);
        SetReference(rowView, "_nameText", name);
        SetReference(rowView, "_countText", count);
        SetReference(rowView, "_weightValueText", weightValue);
        SetReference(rowView, "_weightUnitText", weightUnit);
        SetReference(rowView, "_volumeValueText", volumeValue);
        SetReference(rowView, "_volumeUnitText", volumeUnit);

        InventoryListColumnLayoutSettings settings = InventoryListColumnLayoutSettingsUtility.LoadOrCreateSettings();
        InventoryListColumnLayoutSettingsUtility.EnsureLineLayout(row.transform, settings, dataRow: true);
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

    public static UIInventoryItemDetailPanel BuildItemDetailPanelRoot(Canvas rootCanvas)
    {
        const float panelWidth = 240f;
        const int padding = 8;
        const float lineSpacing = 2f;
        const float nameFontSize = 14f;
        const float bodyFontSize = 12f;
        var detailColor = new Color(0.1f, 0.12f, 0.16f, 0.98f);

        var root = CreateRect("InventoryItemDetailPanel", null, detailColor);
        var rect = root.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0f, 1f);
        rect.sizeDelta = new Vector2(panelWidth, 120f);

        Image background = root.GetComponent<Image>();
        background.raycastTarget = false;

        var fitter = root.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var layout = root.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(padding, padding, padding, padding);
        layout.spacing = lineSpacing;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        TMP_Text nameLine = CreateDetailLine("Line_Name", root.transform, nameFontSize, bold: true);
        TMP_Text descriptionLine = CreateDetailLine("Line_Description", root.transform, bodyFontSize);
        TMP_Text categoryLine = CreateDetailLine("Line_Category", root.transform, bodyFontSize);
        TMP_Text typeLine = CreateDetailLine("Line_Type", root.transform, bodyFontSize);
        TMP_Text countLine = CreateDetailLine("Line_Count", root.transform, bodyFontSize);
        TMP_Text weightLine = CreateDetailLine("Line_Weight", root.transform, bodyFontSize);
        TMP_Text volumeLine = CreateDetailLine("Line_Volume", root.transform, bodyFontSize);
        TMP_Text durabilityLine = CreateDetailLine("Line_Durability", root.transform, bodyFontSize);
        TMP_Text containerCapacityLine = CreateDetailLine("Line_ContainerCapacity", root.transform, bodyFontSize);
        TMP_Text materialsLine = CreateDetailLine("Line_Materials", root.transform, bodyFontSize);

        var panel = root.AddComponent<UIInventoryItemDetailPanel>();
        var shell = root.AddComponent<UIHoverPanelShell>();
        SetReference(panel, "_rect", rect);
        SetReference(panel, "_shell", shell);
        SetReference(panel, "_nameLine", nameLine);
        SetReference(panel, "_descriptionLine", descriptionLine);
        SetReference(panel, "_categoryLine", categoryLine);
        SetReference(panel, "_typeLine", typeLine);
        SetReference(panel, "_countLine", countLine);
        SetReference(panel, "_weightLine", weightLine);
        SetReference(panel, "_volumeLine", volumeLine);
        SetReference(panel, "_durabilityLine", durabilityLine);
        SetReference(panel, "_containerCapacityLine", containerCapacityLine);
        SetReference(panel, "_materialsLine", materialsLine);

        panel.Initialize(rootCanvas);
        root.SetActive(false);
        return panel;
    }

    static TMP_Text CreateDetailLine(string name, Transform parent, float fontSize, bool bold = false)
    {
        var go = CreateRect(name, parent, Color.clear);
        go.GetComponent<Image>().raycastTarget = false;
        var layout = go.AddComponent<LayoutElement>();
        layout.minHeight = fontSize + 4f;
        layout.preferredHeight = fontSize + 4f;

        TMP_Text text = CreateTmp(name + "_Text", go.transform, 0f, fontSize, flexibleWidth: true);
        Stretch(text.rectTransform, 0f, 0f, 0f, 0f);
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = false;
        if (bold)
            text.fontStyle = FontStyles.Bold;
        return text;
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

    public static UIContextMenuHost BuildTileObjectContextMenuRoot()
    {
        var root = CreateRect("TileObjectContextMenu", null, Color.clear);
        var rootRect = root.GetComponent<RectTransform>();
        Stretch(rootRect, 0f, 0f, 0f, 0f);
        root.GetComponent<Image>().raycastTarget = false;

        var panelRootGo = CreateRect("PanelRoot", root.transform, Color.clear);
        var panelRoot = panelRootGo.GetComponent<RectTransform>();
        Stretch(panelRoot, 0f, 0f, 0f, 0f);
        panelRootGo.GetComponent<Image>().raycastTarget = false;

        UIContextMenuItemRow rowTemplate = BuildContextMenuRowTemplate(root.transform);
        UIContextMenuCascadePanel panelTemplate = BuildContextMenuPanelTemplate(root.transform, rowTemplate);

        var menu = root.AddComponent<UIContextMenuHost>();
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
        h.padding = new RectOffset(
            (int)ContextMenuStyle.RowPaddingLeft,
            (int)ContextMenuStyle.RowPaddingRight,
            2,
            2);
        h.spacing = ContextMenuStyle.RowLabelChevronGap;
        h.childAlignment = TextAnchor.MiddleLeft;
        h.childControlWidth = true;
        h.childControlHeight = true;
        h.childForceExpandWidth = false;
        h.childForceExpandHeight = false;

        var le = rowGo.AddComponent<LayoutElement>();
        le.preferredHeight = ContextMenuStyle.RowHeight;
        le.flexibleWidth = 1f;
        le.minHeight = ContextMenuStyle.RowHeight;

        Image icon = EnsureContextMenuRowIcon(rowGo.transform);

        var label = CreateTmp("Label", rowGo.transform, 0f, ContextMenuStyle.FontSize, flexibleWidth: true);
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.raycastTarget = false;

        var chevron = CreateTmp("Chevron", rowGo.transform, ContextMenuStyle.ChevronWidth, ContextMenuStyle.FontSize);
        chevron.text = ItemContextMenuLabels.SubmenuChevron;
        chevron.alignment = TextAlignmentOptions.MidlineRight;
        chevron.raycastTarget = false;

        var row = rowGo.AddComponent<UIContextMenuItemRow>();
        SetReference(row, "_background", rowGo.GetComponent<Image>());
        SetReference(row, "_icon", icon);
        SetReference(row, "_label", label);
        SetReference(row, "_chevron", chevron);
        rowGo.SetActive(false);
        return row;
    }

    internal static int PatchContextMenuRowIcons(GameObject root)
    {
        UIContextMenuItemRow[] rows = root.GetComponentsInChildren<UIContextMenuItemRow>(true);
        for (int i = 0; i < rows.Length; i++)
        {
            Image icon = EnsureContextMenuRowIcon(rows[i].transform);
            SetReference(rows[i], "_icon", icon);
        }

        return rows.Length;
    }

    static Image EnsureContextMenuRowIcon(Transform rowTransform)
    {
        Transform existing = rowTransform.Find(ContextMenuRowIconChildName);
        Image icon;
        if (existing != null)
        {
            icon = existing.GetComponent<Image>();
            if (icon == null)
                icon = existing.gameObject.AddComponent<Image>();
        }
        else
        {
            icon = CreateIcon(ContextMenuRowIconChildName, rowTransform, ContextMenuStyle.RowIconSize);
        }

        ApplyContextMenuRowIconLayout(icon);
        icon.transform.SetAsFirstSibling();
        icon.gameObject.SetActive(false);
        return icon;
    }

    static void ApplyContextMenuRowIconLayout(Image icon)
    {
        icon.raycastTarget = false;
        icon.preserveAspect = true;
        icon.color = Color.white;

        var layout = icon.GetComponent<LayoutElement>();
        if (layout == null)
            layout = icon.gameObject.AddComponent<LayoutElement>();

        float size = ContextMenuStyle.RowIconSize;
        layout.minWidth = size;
        layout.minHeight = size;
        layout.preferredWidth = size;
        layout.preferredHeight = size;
        layout.flexibleWidth = 0f;
        layout.flexibleHeight = 0f;
    }

    static UIContextMenuCascadePanel BuildContextMenuPanelTemplate(Transform parent, UIContextMenuItemRow rowTemplate)
    {
        var panelGo = CreateRect("PanelTemplate", parent, ContextMenuStyle.PanelColor);
        var panelRect = panelGo.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.sizeDelta = new Vector2(ContextMenuStyle.MinPanelWidth, ContextMenuStyle.RowHeight);

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
        le.preferredWidth = ContextMenuStyle.MinPanelWidth;
        le.minWidth = ContextMenuStyle.MinPanelWidth;

        // Scroll: 내용이 PanelMaxHeight를 넘기면 스크롤
        var scrollGo = CreateRect("Scroll", panelGo.transform, Color.clear);
        scrollGo.GetComponent<Image>().raycastTarget = false;
        var scrollRectTransform = scrollGo.GetComponent<RectTransform>();
        var scrollLe = scrollGo.AddComponent<LayoutElement>();
        scrollLe.preferredWidth = ContextMenuStyle.MinPanelWidth - ContextMenuStyle.PanelPadding * 2f;
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
        UIWindowDragHandler dragHandler = headerArea.AddComponent<UIWindowDragHandler>();
        dragHandler.Initialize(rootRect, null);

        var headerTitle = CreateTmp("Txt_Title", headerArea.transform, 0f, spec.HeaderFontSize);
        headerTitle.text = InventoryWindowLabels.PrimaryTitle;
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

        UIWindowResizeHandles resizeHandles = root.AddComponent<UIWindowResizeHandles>();
        SetFloat(resizeHandles, "_handleWidth", spec.EdgeThickness);
        SetReference(resizeHandles, "_window", rootRect);
        SetVector2(resizeHandles, "_minSize",
            new Vector2(
                InventoryWindowLayout.ComputeMinWidth(
                    spec.ChromeMargin,
                    spec.SidebarWidth,
                    spec.ScrollbarWidth,
                    ResolveRowMinWidth()),
                InventoryWindowLayout.MinHeight));
        SetVector2(resizeHandles, "_maxSize",
            new Vector2(UIWindowResizeHandles.DefaultMaxSize, UIWindowResizeHandles.DefaultMaxSize));
        UIItemListView listView = listArea.AddComponent<UIItemListView>();
        ScrollRect scroll = listArea.AddComponent<ScrollRect>();
        var viewport = CreateRect("Viewport", listArea.transform, new Color(1f, 1f, 1f, 0f));
        Stretch(viewport.GetComponent<RectTransform>(), 0f, spec.ScrollbarWidth, 0f, 0f);
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
        Scrollbar listScrollbar = CreateVerticalScrollbar(listArea.transform, spec);
        WireVerticalScrollRect(scroll, viewport.GetComponent<RectTransform>(), contentRect, listScrollbar);
        viewport.AddComponent<InventoryScrollDragHandler>();
        SetReference(listView, "_contentRoot", contentRect);
        SetReference(listView, "_rowPrefab", rowPrefab);

        UIContainerSidebar sidebar = sidebarArea.AddComponent<UIContainerSidebar>();
        InventorySidebarScrollRect sidebarScroll = sidebarArea.AddComponent<InventorySidebarScrollRect>();
        var sidebarViewport = CreateRect("Viewport", sidebarArea.transform, new Color(1f, 1f, 1f, 0f));
        Stretch(sidebarViewport.GetComponent<RectTransform>(), 0f, spec.ScrollbarWidth, 0f, 0f);
        Image sidebarViewportImage = sidebarViewport.GetComponent<Image>();
        sidebarViewportImage.raycastTarget = true;
        sidebarViewport.AddComponent<RectMask2D>();

        var slotRoot = CreateRect("SlotRoot", sidebarViewport.transform, Color.clear);
        var slotRootRect = slotRoot.GetComponent<RectTransform>();
        slotRootRect.anchorMin = new Vector2(0f, 1f);
        slotRootRect.anchorMax = new Vector2(1f, 1f);
        slotRootRect.pivot = new Vector2(0.5f, 1f);
        slotRootRect.anchoredPosition = Vector2.zero;
        slotRootRect.sizeDelta = new Vector2(0f, 0f);
        var slotLayout = slotRoot.AddComponent<VerticalLayoutGroup>();
        slotLayout.spacing = spec.SidebarSlotSpacing;
        int slotPad = spec.SidebarSlotPadding;
        slotLayout.padding = new RectOffset(slotPad, slotPad, slotPad, slotPad);
        slotLayout.childControlHeight = true;
        slotLayout.childControlWidth = true;
        slotLayout.childForceExpandWidth = true;
        slotLayout.childForceExpandHeight = false;
        slotRoot.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        Scrollbar sidebarScrollbar = CreateVerticalScrollbar(sidebarArea.transform, spec);
        WireVerticalScrollRect(sidebarScroll, sidebarViewport.GetComponent<RectTransform>(), slotRootRect, sidebarScrollbar);
        SetReference(sidebar, "_slotRoot", slotRootRect);
        SetReference(sidebar, "_slotPrefab", slotPrefab);

        var window = root.AddComponent<UIInventoryListWindow>();
        SetReference(window, "_listView", listView);
        SetReference(window, "_sidebar", sidebar);
        SetReference(window, "_listArea", listRect);
        SetReference(window, "_sidebarArea", sidebarRect);
        SetReference(window, "_windowDragHandler", dragHandler);
        SetReference(window, "_headerTitle", headerTitle);
        return window;
    }

    /// <summary>
    /// 구 Area_ResizeHandle_* 제거 후 UIWindowResizeHandles 부착 (계층 본문 유지).
    /// </summary>
    public static void PatchExistingWindowResizeHandlers(GameObject windowRoot)
    {
        if (windowRoot == null)
            return;

        UIInventoryListWindow window = windowRoot.GetComponent<UIInventoryListWindow>();
        if (window == null)
        {
            Debug.LogError(
                "[InventoryUIHierarchyBuilder] UIInventoryListWindow missing; cannot patch resize handlers.",
                windowRoot);
            return;
        }

        InventoryUIPrefabStyleSpec spec = InventoryUIPrefabStyleSpec.Default;
        UIWindowResizeHandlesPrefabPatch.Apply(
            windowRoot,
            spec.EdgeThickness,
            proximityReveal: false,
            new Vector2(
                InventoryWindowLayout.ComputeMinWidth(
                    spec.ChromeMargin,
                    spec.SidebarWidth,
                    spec.ScrollbarWidth,
                    ResolveRowMinWidth()),
                InventoryWindowLayout.MinHeight),
            new Vector2(UIWindowResizeHandles.DefaultMaxSize, UIWindowResizeHandles.DefaultMaxSize));

        UIWindowDragHandler drag = windowRoot.GetComponentInChildren<UIWindowDragHandler>(true);
        if (drag != null)
        {
            SetReference(window, "_windowDragHandler", drag);
            SetReference(drag, "_window", windowRoot.transform as RectTransform);
        }
    }

    /// <summary>
    /// Patches scrollbars onto an existing window prefab root without rebuilding chrome
    /// (preserves Area_InvInfo and other hand-authored children).
    /// </summary>
    public static void PatchExistingWindowScrollbars(GameObject windowRoot)
    {
        if (windowRoot == null)
            throw new System.ArgumentNullException(nameof(windowRoot));

        InventoryUIPrefabStyleSpec spec = InventoryUIPrefabStyleSpec.Default;
        Transform listArea = windowRoot.transform.Find("Area_List");
        Transform sidebarArea = windowRoot.transform.Find("Area_Sidebar");
        if (listArea == null || sidebarArea == null)
        {
            Debug.LogError("[InventoryUIHierarchyBuilder] Area_List/Area_Sidebar missing; cannot patch scrollbars.", windowRoot);
            return;
        }

        PatchListAreaScrollbars(listArea, spec);
        PatchSidebarScrollbars(sidebarArea, spec);
    }

    /// <summary>
    /// Adds Area_ColumnHeader under Area_List and insets Viewport top. Does not rebuild window chrome.
    /// Run after Patch Window Scrollbars if both are needed (scrollbar patch resets viewport top).
    /// </summary>
    public static void PatchExistingWindowColumnHeader(GameObject windowRoot)
    {
        if (windowRoot == null)
            throw new System.ArgumentNullException(nameof(windowRoot));

        InventoryUIPrefabStyleSpec spec = InventoryUIPrefabStyleSpec.Default;
        Transform listArea = windowRoot.transform.Find("Area_List");
        if (listArea == null)
        {
            Debug.LogError("[InventoryUIHierarchyBuilder] Area_List missing; cannot patch column header.", windowRoot);
            return;
        }

        if (!listArea.TryGetComponent(out UIItemListView listView))
        {
            Debug.LogError("[InventoryUIHierarchyBuilder] UIItemListView missing on Area_List.", listArea);
            return;
        }

        if (!listArea.TryGetComponent(out ScrollRect scroll) || scroll.viewport == null)
        {
            Debug.LogError("[InventoryUIHierarchyBuilder] ScrollRect viewport missing on Area_List.", listArea);
            return;
        }

        Transform existing = listArea.Find("Area_ColumnHeader");
        if (existing == null && scroll.viewport != null)
            existing = scroll.viewport.Find("Area_ColumnHeader");
        if (existing != null)
            Object.DestroyImmediate(existing.gameObject);

        // Sticky under Viewport so header + rows share the same layout width.
        UIItemListColumnHeader header = BuildColumnHeaderRoot(scroll.viewport, listView, spec);
        ApplyStickyColumnHeaderLayout(scroll, spec);

        SetReference(listView, "_columnHeader", header);
    }

    /// <summary>
    /// Syncs sticky header + Content pad + header LineLayout from Settings (no full rebake).
    /// </summary>
    public static void PatchExistingWindowListColumnLayout(
        GameObject windowRoot,
        InventoryListColumnLayoutSettings settings)
    {
        if (windowRoot == null)
            throw new System.ArgumentNullException(nameof(windowRoot));
        if (settings == null)
            throw new System.ArgumentNullException(nameof(settings));

        InventoryListColumnLayoutSettings.SetCachedDefault(settings);
        InventoryUIPrefabStyleSpec spec = InventoryUIPrefabStyleSpec.Default;

        Transform listArea = windowRoot.transform.Find("Area_List");
        if (listArea == null)
        {
            Debug.LogError("[InventoryUIHierarchyBuilder] Area_List missing; cannot sync column layout.", windowRoot);
            return;
        }

        if (!listArea.TryGetComponent(out ScrollRect scroll) || scroll.viewport == null || scroll.content == null)
        {
            Debug.LogError("[InventoryUIHierarchyBuilder] ScrollRect viewport/content missing on Area_List.", listArea);
            return;
        }

        Transform headerTf = scroll.viewport.Find("Area_ColumnHeader");
        if (headerTf == null)
            headerTf = listArea.Find("Area_ColumnHeader");
        if (headerTf == null)
        {
            Debug.LogError(
                "[InventoryUIHierarchyBuilder] Area_ColumnHeader missing; run Patch Window Column Header first.",
                listArea);
            return;
        }

        if (headerTf.parent != scroll.viewport)
            headerTf.SetParent(scroll.viewport, false);

        ApplyStickyColumnHeaderLayout(scroll, spec);
        InventoryListColumnLayoutSettingsUtility.EnsureLineLayout(headerTf, settings, dataRow: false);
    }

    /// <summary>
    /// Sticky Area_ColumnHeader inside Viewport; Content top pad reserves header height.
    /// </summary>
    static void ApplyStickyColumnHeaderLayout(ScrollRect scroll, InventoryUIPrefabStyleSpec spec)
    {
        RectTransform viewport = scroll.viewport;
        RectTransform content = scroll.content;
        Stretch(viewport, 0f, spec.ScrollbarWidth, 0f, 0f);
        viewport.pivot = new Vector2(0.5f, 0.5f);
        scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

        Transform headerTf = viewport.Find("Area_ColumnHeader");
        if (headerTf != null)
        {
            var headerRect = (RectTransform)headerTf;
            headerRect.anchorMin = new Vector2(0f, 1f);
            headerRect.anchorMax = new Vector2(1f, 1f);
            headerRect.pivot = new Vector2(0.5f, 1f);
            headerRect.offsetMin = new Vector2(0f, -InventoryListColumnLayout.ColumnHeaderHeight);
            headerRect.offsetMax = Vector2.zero;
            // Draw above Content so clicks hit header buttons.
            headerTf.SetSiblingIndex(content.GetSiblingIndex() + 1);
        }

        if (content.TryGetComponent(out VerticalLayoutGroup contentLayout))
        {
            int pad = InventoryListColumnLayout.ContentPadding;
            contentLayout.padding = new RectOffset(
                pad,
                pad,
                InventoryListColumnLayout.ContentPaddingTopWithStickyHeader,
                pad);
        }
    }

    public static UIItemListColumnHeader BuildColumnHeaderRoot(
        Transform listArea,
        UIItemListView listView,
        InventoryUIPrefabStyleSpec spec)
    {
        var headerGo = CreateRect("Area_ColumnHeader", listArea, new Color(0.14f, 0.14f, 0.14f, 1f));
        var headerRect = headerGo.GetComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0f, 1f);
        headerRect.anchorMax = new Vector2(1f, 1f);
        headerRect.pivot = new Vector2(0.5f, 1f);
        headerRect.offsetMin = new Vector2(0f, -InventoryListColumnLayout.ColumnHeaderHeight);
        headerRect.offsetMax = Vector2.zero;

        var layout = headerGo.AddComponent<HorizontalLayoutGroup>();
        // Same horizontal inset as Content pad + row pad.
        int padH = InventoryListColumnLayout.ListInsetHorizontal;
        layout.padding = new RectOffset(padH, padH, spec.RowPaddingV, spec.RowPaddingV);
        layout.spacing = spec.RowSpacing;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        headerGo.AddComponent<LayoutElement>().preferredHeight = InventoryListColumnLayout.ColumnHeaderHeight;

        CreateHeaderSpacer(headerGo.transform, spec.RowIconSize);

        TMP_Text category = CreateHeaderCell(
            "Category", headerGo.transform, spec.RowCategoryWidth, InventoryListColumnLayout.FontHeader,
            TextAlignmentOptions.MidlineLeft, out Button categoryButton);
        TMP_Text name = CreateHeaderCell(
            "Name", headerGo.transform, 0f, InventoryListColumnLayout.FontHeader,
            TextAlignmentOptions.MidlineLeft, out Button nameButton, flexibleWidth: true);
        TMP_Text count = CreateHeaderCell(
            "Count", headerGo.transform, spec.RowCountWidth, InventoryListColumnLayout.FontHeader,
            TextAlignmentOptions.MidlineRight, out Button countButton);
        TMP_Text weightValue = CreateHeaderCell(
            "WeightValue", headerGo.transform, spec.RowWeightValueWidth, InventoryListColumnLayout.FontHeader,
            TextAlignmentOptions.MidlineRight, out Button weightButton);
        TMP_Text weightUnit = CreateTmp(
            "WeightUnit", headerGo.transform, spec.RowWeightUnitWidth, InventoryListColumnLayout.FontHeader,
            alignment: TextAlignmentOptions.MidlineLeft);
        TMP_Text volumeValue = CreateHeaderCell(
            "VolumeValue", headerGo.transform, spec.RowVolumeValueWidth, InventoryListColumnLayout.FontHeader,
            TextAlignmentOptions.MidlineRight, out Button volumeButton);
        TMP_Text volumeUnit = CreateTmp(
            "VolumeUnit", headerGo.transform, spec.RowVolumeUnitWidth, InventoryListColumnLayout.FontHeader,
            alignment: TextAlignmentOptions.MidlineLeft);

        var header = headerGo.AddComponent<UIItemListColumnHeader>();
        SetReference(header, "_listView", listView);
        SetReference(header, "_categoryLabel", category);
        SetReference(header, "_nameLabel", name);
        SetReference(header, "_countLabel", count);
        SetReference(header, "_weightValueLabel", weightValue);
        SetReference(header, "_weightUnitLabel", weightUnit);
        SetReference(header, "_volumeValueLabel", volumeValue);
        SetReference(header, "_volumeUnitLabel", volumeUnit);
        SetReference(header, "_categoryButton", categoryButton);
        SetReference(header, "_nameButton", nameButton);
        SetReference(header, "_countButton", countButton);
        SetReference(header, "_weightButton", weightButton);
        SetReference(header, "_volumeButton", volumeButton);
        return header;
    }

    static void CreateHeaderSpacer(Transform parent, float size)
    {
        var go = CreateRect("IconSpacer", parent, Color.clear);
        go.GetComponent<Image>().raycastTarget = false;
        var layout = go.AddComponent<LayoutElement>();
        layout.preferredWidth = size;
        layout.preferredHeight = size;
        layout.minWidth = size;
        layout.minHeight = size;
    }

    static TMP_Text CreateHeaderCell(
        string name,
        Transform parent,
        float width,
        float fontSize,
        TextAlignmentOptions alignment,
        out Button button,
        bool flexibleWidth = false)
    {
        TMP_Text text = CreateTmp(name, parent, width, fontSize, flexibleWidth, alignment);
        text.raycastTarget = true;
        button = text.gameObject.AddComponent<Button>();
        button.targetGraphic = text;
        button.transition = Selectable.Transition.None;
        return text;
    }

    static void PatchListAreaScrollbars(Transform listArea, InventoryUIPrefabStyleSpec spec)
    {
        if (!listArea.TryGetComponent(out ScrollRect scroll))
        {
            Debug.LogError("[InventoryUIHierarchyBuilder] ScrollRect missing on Area_List.", listArea);
            return;
        }

        RectTransform viewport = scroll.viewport;
        RectTransform content = scroll.content;
        if (viewport == null || content == null)
        {
            Debug.LogError("[InventoryUIHierarchyBuilder] Area_List ScrollRect viewport/content missing.", listArea);
            return;
        }

        Scrollbar scrollbar = FindChildScrollbar(listArea);
        if (scrollbar == null)
            scrollbar = CreateVerticalScrollbar(listArea, spec);

        Stretch(viewport, 0f, spec.ScrollbarWidth, 0f, 0f);
        WireVerticalScrollRect(scroll, viewport, content, scrollbar);

        if (viewport.GetComponent<InventoryScrollDragHandler>() == null)
            viewport.gameObject.AddComponent<InventoryScrollDragHandler>();
    }

    static void PatchSidebarScrollbars(Transform sidebarArea, InventoryUIPrefabStyleSpec spec)
    {
        Transform slotRootTf = sidebarArea.Find("SlotRoot");
        RectTransform slotRootRect = null;
        if (slotRootTf != null)
            slotRootRect = slotRootTf as RectTransform;
        else
        {
            Transform nested = sidebarArea.Find("Viewport/SlotRoot");
            if (nested != null)
                slotRootRect = nested as RectTransform;
        }

        if (slotRootRect == null)
        {
            Debug.LogError("[InventoryUIHierarchyBuilder] SlotRoot missing under Area_Sidebar.", sidebarArea);
            return;
        }

        InventorySidebarScrollRect sidebarScroll = EnsureSidebarScrollRect(sidebarArea.gameObject);

        Transform viewportTf = sidebarArea.Find("Viewport");
        RectTransform viewportRect;
        if (viewportTf == null)
        {
            var viewportGo = CreateRect("Viewport", sidebarArea, new Color(1f, 1f, 1f, 0f));
            viewportRect = viewportGo.GetComponent<RectTransform>();
            Stretch(viewportRect, 0f, spec.ScrollbarWidth, 0f, 0f);
            viewportGo.GetComponent<Image>().raycastTarget = true;
            viewportGo.AddComponent<RectMask2D>();
        }
        else
        {
            viewportRect = viewportTf as RectTransform;
            Stretch(viewportRect, 0f, spec.ScrollbarWidth, 0f, 0f);
            if (viewportRect.GetComponent<RectMask2D>() == null)
                viewportRect.gameObject.AddComponent<RectMask2D>();
            InventoryScrollDragHandler[] handlers =
                viewportRect.GetComponents<InventoryScrollDragHandler>();
            for (int i = 0; i < handlers.Length; i++)
                Object.DestroyImmediate(handlers[i]);
            if (viewportRect.TryGetComponent(out Image vpImage))
            {
                vpImage.color = new Color(1f, 1f, 1f, 0f);
                vpImage.raycastTarget = true;
            }
        }

        if (slotRootRect.parent != viewportRect)
            slotRootRect.SetParent(viewportRect, false);

        slotRootRect.anchorMin = new Vector2(0f, 1f);
        slotRootRect.anchorMax = new Vector2(1f, 1f);
        slotRootRect.pivot = new Vector2(0.5f, 1f);
        slotRootRect.anchoredPosition = Vector2.zero;
        slotRootRect.sizeDelta = new Vector2(0f, 0f);

        if (slotRootRect.TryGetComponent(out VerticalLayoutGroup slotLayout))
        {
            slotLayout.childControlWidth = true;
            slotLayout.childForceExpandWidth = true;
            slotLayout.childForceExpandHeight = false;
        }

        if (!slotRootRect.TryGetComponent(out ContentSizeFitter fitter))
            fitter = slotRootRect.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        Scrollbar scrollbar = FindChildScrollbar(sidebarArea);
        if (scrollbar == null)
            scrollbar = CreateVerticalScrollbar(sidebarArea, spec);

        WireVerticalScrollRect(sidebarScroll, viewportRect, slotRootRect, scrollbar);

        if (sidebarArea.TryGetComponent(out UIContainerSidebar sidebar))
            SetReference(sidebar, "_slotRoot", slotRootRect);
    }

    static InventorySidebarScrollRect EnsureSidebarScrollRect(GameObject sidebarGo)
    {
        if (sidebarGo.TryGetComponent(out InventorySidebarScrollRect existing))
            return existing;

        // Plain ScrollRect cannot become a subclass in-place — replace and rewire later.
        if (sidebarGo.TryGetComponent(out ScrollRect plain) && plain is not InventorySidebarScrollRect)
            Object.DestroyImmediate(plain);

        return sidebarGo.AddComponent<InventorySidebarScrollRect>();
    }

    static Scrollbar FindChildScrollbar(Transform parent)
    {
        Transform existing = parent.Find("Scrollbar_Vertical");
        return existing != null ? existing.GetComponent<Scrollbar>() : null;
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

    static void WireVerticalScrollRect(
        ScrollRect scroll,
        RectTransform viewport,
        RectTransform content,
        Scrollbar verticalScrollbar)
    {
        scroll.viewport = viewport;
        scroll.content = content;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.verticalScrollbar = verticalScrollbar;
        // AutoHideAndExpandViewport resizes Viewport at runtime and covers Area_ColumnHeader.
        scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
        scroll.verticalScrollbarSpacing = 0f;
        scroll.horizontalScrollbar = null;
    }

    static Scrollbar CreateVerticalScrollbar(Transform parent, InventoryUIPrefabStyleSpec spec)
    {
        var root = CreateRect("Scrollbar_Vertical", parent, spec.ScrollbarTrackColor);
        var rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(1f, 0f);
        rootRect.anchorMax = new Vector2(1f, 1f);
        rootRect.pivot = new Vector2(1f, 0.5f);
        rootRect.anchoredPosition = Vector2.zero;
        rootRect.sizeDelta = new Vector2(spec.ScrollbarWidth, 0f);

        var slidingArea = CreateRect("Sliding Area", root.transform, Color.clear);
        slidingArea.GetComponent<Image>().raycastTarget = false;
        Stretch(slidingArea.GetComponent<RectTransform>(), 0f, 0f, 0f, 0f);

        var handle = CreateRect("Handle", slidingArea.transform, spec.ScrollbarHandleColor);
        Stretch(handle.GetComponent<RectTransform>(), 0f, 0f, 0f, 0f);
        Image handleImage = handle.GetComponent<Image>();
        handleImage.raycastTarget = true;

        Scrollbar scrollbar = root.AddComponent<Scrollbar>();
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        scrollbar.handleRect = handle.GetComponent<RectTransform>();
        scrollbar.targetGraphic = handleImage;
        scrollbar.transition = Selectable.Transition.None;
        return scrollbar;
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

    static TMP_Text CreateTmp(
        string name,
        Transform parent,
        float width,
        float fontSize,
        bool flexibleWidth = false,
        TextAlignmentOptions alignment = TextAlignmentOptions.MidlineLeft)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        go.layer = LayerMask.NameToLayer("UI");
        var text = go.GetComponent<TextMeshProUGUI>();
        text.font = LoadDefaultFont();
        text.fontSize = fontSize;
        text.color = Color.white;
        text.alignment = alignment;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.enableWordWrapping = false;
        text.raycastTarget = false;

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

    static Image CreateRowIcon(Transform parent, float size)
    {
        var wrapper = CreateRect("IconWarpper", parent, Color.clear);
        wrapper.GetComponent<Image>().raycastTarget = false;
        var wrapperLayout = wrapper.AddComponent<LayoutElement>();
        wrapperLayout.preferredWidth = size;
        wrapperLayout.preferredHeight = size;
        wrapperLayout.minWidth = size;
        wrapperLayout.minHeight = size;

        var iconGo = CreateRect("Icon", wrapper.transform, new Color(0.25f, 0.25f, 0.25f, 1f));
        Stretch(iconGo.GetComponent<RectTransform>(), 0f, 0f, 0f, 0f);
        Image icon = iconGo.GetComponent<Image>();
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        return icon;
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


    static float ResolveRowMinWidth()
    {
        UIItemListRow row = AssetDatabase.LoadAssetAtPath<UIItemListRow>(
            PrefabFolder + "/Grp_ItemListRow.prefab");
        return InventoryListColumnLayout.MeasureMinRowWidth(row);
    }

    static void SetFloat(Object target, string propertyName, float value)
    {
        var serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null)
        {
            Debug.LogError($"[InventoryUIHierarchyBuilder] Missing property '{propertyName}' on {target.GetType().Name}.");
            return;
        }

        property.floatValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    static void SetVector2(Object target, string propertyName, Vector2 value)
    {
        var serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null)
        {
            Debug.LogError($"[InventoryUIHierarchyBuilder] Missing property '{propertyName}' on {target.GetType().Name}.");
            return;
        }

        property.vector2Value = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
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

    static void SetObjectReferenceArray(Object target, string propertyName, Object[] values)
    {
        var serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null || !property.isArray)
        {
            Debug.LogError(
                $"[InventoryUIHierarchyBuilder] Missing array property '{propertyName}' on {target.GetType().Name}.");
            return;
        }

        property.arraySize = values != null ? values.Length : 0;
        if (values != null)
        {
            for (int i = 0; i < values.Length; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

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
