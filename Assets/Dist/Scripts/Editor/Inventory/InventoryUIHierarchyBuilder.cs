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
        rowRect.sizeDelta = new Vector2(0f, 36f);
        var layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(8, 8, 4, 4);
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        row.AddComponent<LayoutElement>().preferredHeight = 36f;

        Image icon = CreateIcon("Icon", row.transform, 32f);
        var category = CreateTmp("Category", row.transform, 80f, 14);
        var name = CreateTmp("Name", row.transform, 200f, 16);
        var detail = CreateTmp("Detail", row.transform, 160f, 14);

        var rowView = row.AddComponent<UIItemListRow>();
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
        rootRect.sizeDelta = new Vector2(720f, 420f);
        rootRect.anchoredPosition = Vector2.zero;

        var listArea = CreateRect("Area_List", root.transform, new Color(0.1f, 0.1f, 0.1f, 1f));
        Stretch(listArea.GetComponent<RectTransform>(), 10f, 10f, 130f, 10f);

        var sidebarArea = CreateRect("Area_Sidebar", root.transform, new Color(0.08f, 0.08f, 0.08f, 1f));
        var sidebarRect = sidebarArea.GetComponent<RectTransform>();
        sidebarRect.anchorMin = new Vector2(1f, 0f);
        sidebarRect.anchorMax = new Vector2(1f, 1f);
        sidebarRect.pivot = new Vector2(1f, 0.5f);
        sidebarRect.anchoredPosition = new Vector2(-10f, 0f);
        sidebarRect.sizeDelta = new Vector2(110f, -20f);

        UIItemListView listView = listArea.AddComponent<UIItemListView>();
        ScrollRect scroll = listArea.AddComponent<ScrollRect>();
        var viewport = CreateRect("Viewport", listArea.transform, Color.white);
        Stretch(viewport.GetComponent<RectTransform>(), 0f, 0f, 0f, 0f);
        var mask = viewport.AddComponent<Mask>();
        mask.showMaskGraphic = false;
        var content = CreateRect("Content", viewport.transform, Color.clear);
        var contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0f, 0f);
        var contentLayout = content.AddComponent<VerticalLayoutGroup>();
        contentLayout.childControlHeight = true;
        contentLayout.childForceExpandHeight = false;
        contentLayout.spacing = 4f;
        contentLayout.padding = new RectOffset(4, 4, 4, 4);
        content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.viewport = viewport.GetComponent<RectTransform>();
        scroll.content = contentRect;
        scroll.horizontal = false;
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
        return window;
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

    static TMP_Text CreateTmp(string name, Transform parent, float width, float fontSize)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        go.layer = LayerMask.NameToLayer("UI");
        var text = go.GetComponent<TextMeshProUGUI>();
        text.font = LoadDefaultFont();
        text.fontSize = fontSize;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        if (width > 0f)
        {
            var layout = go.AddComponent<LayoutElement>();
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
