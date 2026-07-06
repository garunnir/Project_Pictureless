// ============================================================
// InventoryUIFactory — 씬 테스트용 인벤 UI 골격 런타임 생성
// ============================================================

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class InventoryUIFactory
{
    static readonly Color PanelColor = new(0.12f, 0.12f, 0.12f, 0.95f);
    static readonly Color RowColor = new(0.18f, 0.18f, 0.18f, 1f);
    static readonly Color SlotColor = new(0.22f, 0.22f, 0.22f, 1f);
    static readonly Color HighlightColor = new(0.35f, 0.45f, 0.35f, 0.9f);

    public static UIInventoryListWindow CreateWindow(Transform parent, string windowName)
    {
        var root = CreateRect(windowName, parent, PanelColor);
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

        UIItemListRow rowPrefab = CreateRowPrefab(root.transform);
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
        var layout = content.AddComponent<VerticalLayoutGroup>();
        layout.childControlHeight = true;
        layout.childForceExpandHeight = false;
        layout.spacing = 4f;
        layout.padding = new RectOffset(4, 4, 4, 4);
        content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.viewport = viewport.GetComponent<RectTransform>();
        scroll.content = contentRect;
        scroll.horizontal = false;
        SetPrivateField(listView, "_contentRoot", contentRect);
        SetPrivateField(listView, "_rowPrefab", rowPrefab);

        UIContainerSlot slotPrefab = CreateSlotPrefab(root.transform);
        UIContainerSidebar sidebar = sidebarArea.AddComponent<UIContainerSidebar>();
        var slotRoot = CreateRect("SlotRoot", sidebarArea.transform, Color.clear);
        Stretch(slotRoot.GetComponent<RectTransform>(), 0f, 0f, 0f, 0f);
        var slotLayout = slotRoot.AddComponent<VerticalLayoutGroup>();
        slotLayout.spacing = 6f;
        slotLayout.padding = new RectOffset(4, 4, 4, 4);
        slotLayout.childControlHeight = true;
        slotLayout.childForceExpandHeight = false;
        SetPrivateField(sidebar, "_slotRoot", slotRoot.GetComponent<RectTransform>());
        SetPrivateField(sidebar, "_slotPrefab", slotPrefab);

        var window = root.AddComponent<UIInventoryListWindow>();
        SetPrivateField(window, "_listView", listView);
        SetPrivateField(window, "_sidebar", sidebar);
        root.SetActive(false);
        return window;
    }

    static UIItemListRow CreateRowPrefab(Transform parent)
    {
        var row = CreateRect("Grp_ItemListRow_Template", parent, RowColor);
        var rowRect = row.GetComponent<RectTransform>();
        rowRect.sizeDelta = new Vector2(0f, 36f);
        var layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(8, 8, 4, 4);
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        row.AddComponent<LayoutElement>().preferredHeight = 36f;

        var category = CreateTmp("Category", row.transform, 80f, 14);
        var name = CreateTmp("Name", row.transform, 220f, 16);
        var detail = CreateTmp("Detail", row.transform, 180f, 14);

        var rowView = row.AddComponent<UIItemListRow>();
        SetPrivateField(rowView, "_categoryText", category);
        SetPrivateField(rowView, "_nameText", name);
        SetPrivateField(rowView, "_detailText", detail);
        row.SetActive(false);
        return rowView;
    }

    static UIContainerSlot CreateSlotPrefab(Transform parent)
    {
        var slot = CreateRect("Grp_ContainerSlot_Template", parent, SlotColor);
        slot.AddComponent<LayoutElement>().preferredHeight = 48f;
        var button = slot.AddComponent<Button>();
        var highlight = CreateRect("Highlight", slot.transform, HighlightColor);
        Stretch(highlight.GetComponent<RectTransform>(), 0f, 0f, 0f, 0f);
        highlight.SetActive(false);
        var label = CreateTmp("Label", slot.transform, 0f, 14);
        Stretch(label.rectTransform, 6f, 6f, 6f, 6f);

        var slotView = slot.AddComponent<UIContainerSlot>();
        SetPrivateField(slotView, "_button", button);
        SetPrivateField(slotView, "_label", label);
        SetPrivateField(slotView, "_highlight", highlight.GetComponent<Image>());
        slot.SetActive(false);
        return slotView;
    }

    static GameObject CreateRect(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = color;
        return go;
    }

    static TMP_Text CreateTmp(string name, Transform parent, float width, float fontSize)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var text = go.GetComponent<TextMeshProUGUI>();
        text.font = TMP_Settings.defaultFontAsset;
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

    static void Stretch(RectTransform rect, float left, float right, float top, float bottom)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
    }

    static void SetPrivateField(Object target, string fieldName, Object value)
    {
        var field = target.GetType().GetField(
            fieldName,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        field?.SetValue(target, value);
    }
}
