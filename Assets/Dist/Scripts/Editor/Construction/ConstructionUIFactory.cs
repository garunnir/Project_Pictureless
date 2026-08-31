// ============================================================
// ConstructionUIFactory — 본편 건설 창 프리팹 일회 생성용
// ============================================================

#if UNITY_EDITOR
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class ConstructionUIFactory
{
    public static UIConstructionWindow CreateWindowRoot()
    {
        var root = new GameObject(
            "Wnd_Construction",
            typeof(RectTransform),
            typeof(Image),
            typeof(UIConstructionWindow));
        var rootRt = root.GetComponent<RectTransform>();
        rootRt.sizeDelta = new Vector2(360f, 420f);
        var rootImg = root.GetComponent<Image>();
        rootImg.color = new Color(0.12f, 0.12f, 0.14f, 0.94f);

        UIConstructionWindow window = root.GetComponent<UIConstructionWindow>();

        TMP_Text title = CreateLabel(root.transform, "Lbl_Title", "건설", 20f, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(12f, -36f), new Vector2(-12f, -8f));
        TMP_Text detail = CreateLabel(root.transform, "Lbl_Detail", string.Empty, 14f, new Vector2(0f, 0f), new Vector2(1f, 0.42f), new Vector2(12f, 52f), new Vector2(-12f, 8f));

        GameObject scrollGo = new GameObject("Scroll_List", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        scrollGo.transform.SetParent(root.transform, false);
        var scrollRt = scrollGo.GetComponent<RectTransform>();
        scrollRt.anchorMin = new Vector2(0f, 0.42f);
        scrollRt.anchorMax = new Vector2(1f, 1f);
        scrollRt.offsetMin = new Vector2(12f, 8f);
        scrollRt.offsetMax = new Vector2(-12f, -44f);
        scrollGo.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.1f, 1f);

        GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
        viewport.transform.SetParent(scrollGo.transform, false);
        Stretch(viewport.GetComponent<RectTransform>());

        GameObject content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        content.transform.SetParent(viewport.transform, false);
        var contentRt = content.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0f, 1f);
        contentRt.anchorMax = new Vector2(1f, 1f);
        contentRt.pivot = new Vector2(0.5f, 1f);
        contentRt.sizeDelta = new Vector2(0f, 0f);
        var vlg = content.GetComponent<VerticalLayoutGroup>();
        vlg.childControlHeight = true;
        vlg.childControlWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.spacing = 2f;
        content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scroll = scrollGo.GetComponent<ScrollRect>();
        scroll.viewport = viewport.GetComponent<RectTransform>();
        scroll.content = contentRt;
        scroll.horizontal = false;

        Button build = CreateButton(root.transform, "Btn_Build", "건설", new Vector2(0f, 0f), new Vector2(0.5f, 0f), new Vector2(12f, 12f), new Vector2(-4f, 44f));
        Button close = CreateButton(root.transform, "Btn_Close", "닫기", new Vector2(0.5f, 0f), new Vector2(1f, 0f), new Vector2(4f, 12f), new Vector2(-12f, 44f));

        UIConstructionRecipeRow rowPrefab = CreateRowPrefab();
        rowPrefab.transform.SetParent(root.transform, false);

        SetPrivate(window, "_title", title);
        SetPrivate(window, "_detail", detail);
        SetPrivate(window, "_buildButton", build);
        SetPrivate(window, "_closeButton", close);
        SetPrivate(window, "_listContent", content.transform);
        SetPrivate(window, "_rowPrefab", rowPrefab);

        return window;
    }

    static UIConstructionRecipeRow CreateRowPrefab()
    {
        var go = new GameObject("Row_ConstructionRecipe", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement), typeof(UIConstructionRecipeRow));
        go.GetComponent<LayoutElement>().preferredHeight = 28f;
        go.GetComponent<Image>().color = Color.white;
        var row = go.GetComponent<UIConstructionRecipeRow>();
        row.EnsureRuntimeChrome();
        SetPrivate(row, "_button", go.GetComponent<Button>());
        SetPrivate(row, "_background", go.GetComponent<Image>());
        SetPrivate(row, "_label", go.GetComponentInChildren<TMP_Text>());
        go.SetActive(false);
        return row;
    }

    static TMP_Text CreateLabel(
        Transform parent,
        string name,
        string text,
        float size,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 offsetMin,
        Vector2 offsetMax)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        DistUiFont.Apply(tmp);
        return tmp;
    }

    static Button CreateButton(
        Transform parent,
        string name,
        string label,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 offsetMin,
        Vector2 offsetMax)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
        go.GetComponent<Image>().color = new Color(0.25f, 0.45f, 0.3f, 1f);
        var btn = go.GetComponent<Button>();
        TMP_Text tmp = CreateLabel(
            go.transform,
            "Label",
            label,
            16f,
            Vector2.zero,
            Vector2.one,
            new Vector2(4f, 2f),
            new Vector2(-4f, -2f));
        tmp.alignment = TextAlignmentOptions.Center;
        return btn;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static void SetPrivate(object target, string field, object value)
    {
        var f = target.GetType().GetField(
            field,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        f?.SetValue(target, value);
    }
}
#endif
