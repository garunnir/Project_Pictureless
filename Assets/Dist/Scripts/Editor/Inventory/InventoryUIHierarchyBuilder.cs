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
    internal const string EmptyItemIconPath = "Assets/Dist/Visual/Sprites/Textures/UI/Inventory/ui_icon_empty.png";

    static readonly Color PanelColor = new(0.12f, 0.12f, 0.12f, 0.95f);
    static readonly Color RowColor = new(0.18f, 0.18f, 0.18f, 1f);
    static readonly Color SlotColor = new(0.22f, 0.22f, 0.22f, 1f);
    static readonly Color HighlightColor = new(0.35f, 0.45f, 0.35f, 0.9f);

    public static UIItemListRow BuildRowPrefabRoot()
    {
        var row = CreateRect("Grp_ItemListRow", null, RowColor);
        var rowRect = row.GetComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0f, 0.5f);
        rowRect.anchorMax = new Vector2(1f, 0.5f);
        rowRect.pivot = new Vector2(0.5f, 0.5f);
        rowRect.sizeDelta = new Vector2(0f, 36f);
        var layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(8, 8, 4, 4);
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        row.AddComponent<LayoutElement>().preferredHeight = 36f;

        Image icon = CreateIcon("Icon", row.transform, 32f);
        var category = CreateTmp("Category", row.transform, 64f, 14);
        var name = CreateTmp("Name", row.transform, 0f, 16, flexibleWidth: true);
        var detail = CreateTmp("Detail", row.transform, 88f, 14);

        var rowView = row.AddComponent<UIItemListRow>();
        SetReference(rowView, "_backgroundImage", row.GetComponent<Image>());
        SetReference(rowView, "_iconImage", icon);
        SetReference(rowView, "_emptyIconSprite", LoadEmptyItemIcon());
        SetReference(rowView, "_categoryText", category);
        SetReference(rowView, "_nameText", name);
        SetReference(rowView, "_detailText", detail);
        return rowView;
    }

    public static UIContainerSlot BuildSlotPrefabRoot()
    {
        var slot = CreateRect("Grp_ContainerSlot", null, SlotColor);
        slot.AddComponent<LayoutElement>().preferredHeight = 48f;
        var button = slot.AddComponent<Button>();
        var highlight = CreateRect("Highlight", slot.transform, HighlightColor);
        Stretch(highlight.GetComponent<RectTransform>(), 0f, 0f, 0f, 0f);
        highlight.SetActive(false);
        var label = CreateTmp("Label", slot.transform, 0f, 14);
        Stretch(label.rectTransform, 6f, 6f, 6f, 6f);

        var slotView = slot.AddComponent<UIContainerSlot>();
        SetReference(slotView, "_button", button);
        SetReference(slotView, "_label", label);
        SetReference(slotView, "_highlight", highlight.GetComponent<Image>());
        return slotView;
    }

    public static UIInventoryListWindow BuildWindowRoot(
        UIItemListRow rowPrefab,
        UIContainerSlot slotPrefab)
    {
        var root = CreateRect("Grp_InventoryListWindow", null, PanelColor);
        var rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 0.5f);
        rootRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.sizeDelta = new Vector2(480f, 360f);
        rootRect.anchoredPosition = Vector2.zero;

        var headerArea = CreateRect("Area_Header", root.transform, new Color(0.16f, 0.16f, 0.16f, 1f));
        var headerRect = headerArea.GetComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0f, 1f);
        headerRect.anchorMax = new Vector2(1f, 1f);
        headerRect.pivot = new Vector2(0.5f, 1f);
        headerRect.anchoredPosition = Vector2.zero;
        headerRect.sizeDelta = new Vector2(0f, InventoryWindowLayout.HeaderHeight);
        headerArea.AddComponent<InventoryWindowDragHandler>();

        var headerTitle = CreateTmp("Txt_Title", headerArea.transform, 0f, 14);
        headerTitle.text = "Inventory";
        headerTitle.alignment = TextAlignmentOptions.MidlineLeft;
        headerTitle.raycastTarget = false;
        Stretch(headerTitle.rectTransform, 10f, 10f, 0f, 0f);

        var listArea = CreateRect("Area_List", root.transform, new Color(0.1f, 0.1f, 0.1f, 1f));
        var listRect = listArea.GetComponent<RectTransform>();
        Stretch(listRect, 10f, 10f, InventoryWindowLayout.HeaderHeight + 10f, 10f);

        var sidebarArea = CreateRect("Area_Sidebar", root.transform, new Color(0.08f, 0.08f, 0.08f, 1f));
        var sidebarRect = sidebarArea.GetComponent<RectTransform>();
        sidebarRect.anchorMin = new Vector2(1f, 0f);
        sidebarRect.anchorMax = new Vector2(1f, 1f);
        sidebarRect.pivot = new Vector2(1f, 0.5f);
        sidebarRect.offsetMin = new Vector2(-120f, 10f);
        sidebarRect.offsetMax = new Vector2(-10f, -(InventoryWindowLayout.HeaderHeight + 10f));

        const float edgeThickness = 6f;
        const float cornerSize = 10f;
        var handleColor = new Color(1f, 1f, 1f, 0.02f);

        CreateResizeHandle(root.transform, "Area_ResizeHandle_Left", handleColor, WindowResizeEdge.Left,
            new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), Vector2.zero,
            new Vector2(edgeThickness, 0f));
        CreateResizeHandle(root.transform, "Area_ResizeHandle_Right", handleColor, WindowResizeEdge.Right,
            new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), Vector2.zero,
            new Vector2(edgeThickness, 0f));
        CreateResizeHandle(root.transform, "Area_ResizeHandle_Top", handleColor, WindowResizeEdge.Top,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero,
            new Vector2(0f, edgeThickness));
        CreateResizeHandle(root.transform, "Area_ResizeHandle_Bottom", handleColor, WindowResizeEdge.Bottom,
            new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), Vector2.zero,
            new Vector2(0f, edgeThickness));
        CreateResizeHandle(root.transform, "Area_ResizeHandle_TopLeft", handleColor, WindowResizeEdge.TopLeft,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero,
            new Vector2(cornerSize, cornerSize));
        CreateResizeHandle(root.transform, "Area_ResizeHandle_TopRight", handleColor, WindowResizeEdge.TopRight,
            new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), Vector2.zero,
            new Vector2(cornerSize, cornerSize));
        CreateResizeHandle(root.transform, "Area_ResizeHandle_BottomLeft", handleColor, WindowResizeEdge.BottomLeft,
            new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), Vector2.zero,
            new Vector2(cornerSize, cornerSize));
        CreateResizeHandle(root.transform, "Area_ResizeHandle_BottomRight", handleColor, WindowResizeEdge.BottomRight,
            new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), Vector2.zero,
            new Vector2(cornerSize, cornerSize));

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
        contentLayout.spacing = 4f;
        contentLayout.padding = new RectOffset(4, 4, 4, 4);
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
        slotLayout.spacing = 6f;
        slotLayout.padding = new RectOffset(4, 4, 4, 4);
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

    static Sprite LoadEmptyItemIcon()
    {
        var sprites = AssetDatabase.LoadAllAssetsAtPath(EmptyItemIconPath);
        for (int i = 0; i < sprites.Length; i++)
        {
            if (sprites[i] is Sprite sprite)
                return sprite;
        }

        Debug.LogError($"[InventoryUIHierarchyBuilder] Empty item icon sprite not found: {EmptyItemIconPath}");
        return null;
    }
}
#endif
