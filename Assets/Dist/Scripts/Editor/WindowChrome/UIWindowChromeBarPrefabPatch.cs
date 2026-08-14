// ============================================================
// UIWindowChromeBarPrefabPatch — 헤더에 접기/끄기 버튼 부착 (MCP)
// ============================================================

#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class UIWindowChromeBarPrefabPatch
{
    const string FoldButtonName = "Btn_Fold";
    const string CloseButtonName = "Btn_Close";
    const string FoldedTitleName = "Txt_FoldedTitle";

    public static UIWindowChromeBar Apply(
        GameObject windowRoot,
        bool createHeaderIfMissing,
        bool addFoldedTitle,
        string foldedTitleText)
    {
        if (windowRoot == null)
            return null;

        RectTransform windowRect = windowRoot.transform as RectTransform;
        Transform header = FindHeader(windowRoot.transform);
        if (header == null && createHeaderIfMissing)
            header = CreateHeader(windowRoot.transform);

        if (header == null)
        {
            Debug.LogError(
                $"[UIWindowChromeBarPrefabPatch] Header missing on {windowRoot.name}.",
                windowRoot);
            return null;
        }

        EnsureDragHandler(header, windowRect);
        EnsureOverlayWindow(windowRoot);

        RectTransform headerRect = header as RectTransform;
        float headerHeight = ResolveHeaderHeight(headerRect);
        float buttonSize = Mathf.Max(10f, Mathf.Min(UIWindowChromeLayout.ButtonSize, headerHeight - 2f));

        Button closeButton = FindButton(header, CloseButtonName);
        if (closeButton == null)
            closeButton = CreateButton(header, CloseButtonName, UIWindowChromeLayout.CloseLabel, buttonSize);

        Button foldButton = FindButton(header, FoldButtonName);
        if (foldButton == null)
            foldButton = CreateButton(header, FoldButtonName, UIWindowChromeLayout.FoldExpandedLabel, buttonSize);

        PlaceCluster(closeButton.transform as RectTransform, foldButton.transform as RectTransform, buttonSize);
        InsetHeaderTitle(headerRect, UIWindowChromeLayout.ClusterWidth(2));

        TMP_Text foldedTitle = null;
        if (addFoldedTitle)
            foldedTitle = EnsureFoldedTitle(headerRect, foldedTitleText);

        UIWindowChromeBar bar = header.GetComponent<UIWindowChromeBar>();
        if (bar == null)
            bar = header.gameObject.AddComponent<UIWindowChromeBar>();

        var so = new SerializedObject(bar);
        so.FindProperty("_window").objectReferenceValue = windowRect;
        so.FindProperty("_foldButton").objectReferenceValue = foldButton;
        so.FindProperty("_closeButton").objectReferenceValue = closeButton;
        so.FindProperty("_foldLabel").objectReferenceValue =
            foldButton != null ? foldButton.GetComponentInChildren<TMP_Text>(true) : null;
        so.FindProperty("_foldedTitle").objectReferenceValue = foldedTitle;
        so.FindProperty("_enableFold").boolValue = true;
        so.FindProperty("_enableClose").boolValue = true;
        so.ApplyModifiedPropertiesWithoutUndo();

        return bar;
    }

    static Transform FindHeader(Transform root)
    {
        Transform named = FindByName(root, "Area_Header") ?? FindByName(root, "Header");
        if (named != null)
            return named;

        UIWindowDragHandler[] drags = root.GetComponentsInChildren<UIWindowDragHandler>(true);
        for (int i = 0; i < drags.Length; i++)
        {
            if (drags[i] != null && drags[i].transform != root)
                return drags[i].transform;
        }

        return null;
    }

    static Transform FindByName(Transform root, string name)
    {
        Transform direct = root.Find(name);
        if (direct != null)
            return direct;

        Transform[] all = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != null && all[i].name == name)
                return all[i];
        }

        return null;
    }

    static Transform CreateHeader(Transform window)
    {
        var go = new GameObject("Area_Header", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.layer = window.gameObject.layer;
        go.transform.SetParent(window, false);
        go.transform.SetAsFirstSibling();

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(0f, UIWindowChromeLayout.FoldedHeaderHeight);

        Image image = go.GetComponent<Image>();
        image.color = new Color(0.2f, 0.2f, 0.24f, 0.95f);
        image.raycastTarget = true;

        CanvasGroup group = go.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;
        group.interactable = false;
        return go.transform;
    }

    static void EnsureDragHandler(Transform header, RectTransform window)
    {
        UIWindowDragHandler drag = header.GetComponent<UIWindowDragHandler>();
        if (drag == null)
            drag = header.gameObject.AddComponent<UIWindowDragHandler>();

        var so = new SerializedObject(drag);
        so.FindProperty("_window").objectReferenceValue = window;
        so.ApplyModifiedPropertiesWithoutUndo();
        drag.Initialize(window, null);
    }

    static void EnsureOverlayWindow(GameObject windowRoot)
    {
        if (!windowRoot.TryGetComponent(out UIOverlayWindow _))
            windowRoot.AddComponent<UIOverlayWindow>();
    }

    static float ResolveHeaderHeight(RectTransform header)
    {
        if (header == null)
            return UIWindowChromeLayout.FoldedHeaderHeight;

        bool stretchedY = !Mathf.Approximately(header.anchorMin.y, header.anchorMax.y);
        if (stretchedY)
            return UIWindowChromeLayout.FoldedHeaderHeight;

        float height = header.rect.height;
        if (height < 8f)
            height = header.sizeDelta.y;
        if (height < 8f)
            return UIWindowChromeLayout.FoldedHeaderHeight;
        return height;
    }

    static Button FindButton(Transform header, string name)
    {
        Transform child = header.Find(name);
        return child != null ? child.GetComponent<Button>() : null;
    }

    static Button CreateButton(Transform header, string name, string label, float size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.layer = header.gameObject.layer;
        go.transform.SetParent(header, false);

        Image image = go.GetComponent<Image>();
        image.color = UIWindowChromeLayout.ButtonColor;
        image.raycastTarget = true;

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 0.5f);
        rect.anchorMax = new Vector2(1f, 0.5f);
        rect.pivot = new Vector2(1f, 0.5f);
        rect.sizeDelta = new Vector2(size, size);

        TMP_Text tmp = CreateTmp(go.transform, "Label", label, UIWindowChromeLayout.ButtonFontSize);
        RectTransform tmpRect = tmp.rectTransform;
        tmpRect.anchorMin = Vector2.zero;
        tmpRect.anchorMax = Vector2.one;
        tmpRect.offsetMin = Vector2.zero;
        tmpRect.offsetMax = Vector2.zero;
        tmp.raycastTarget = false;

        return go.GetComponent<Button>();
    }

    static void PlaceCluster(RectTransform closeRect, RectTransform foldRect, float buttonSize)
    {
        float pad = UIWindowChromeLayout.ButtonEdgePadding;
        float gap = UIWindowChromeLayout.ButtonSpacing;
        if (closeRect != null)
        {
            closeRect.anchorMin = new Vector2(1f, 0.5f);
            closeRect.anchorMax = new Vector2(1f, 0.5f);
            closeRect.pivot = new Vector2(1f, 0.5f);
            closeRect.sizeDelta = new Vector2(buttonSize, buttonSize);
            closeRect.anchoredPosition = new Vector2(-pad, 0f);
        }

        if (foldRect != null)
        {
            foldRect.anchorMin = new Vector2(1f, 0.5f);
            foldRect.anchorMax = new Vector2(1f, 0.5f);
            foldRect.pivot = new Vector2(1f, 0.5f);
            foldRect.sizeDelta = new Vector2(buttonSize, buttonSize);
            foldRect.anchoredPosition = new Vector2(-(pad + buttonSize + gap), 0f);
        }
    }

    static void InsetHeaderTitle(RectTransform header, float rightInset)
    {
        TMP_Text[] texts = header.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            TMP_Text text = texts[i];
            if (text == null)
                continue;

            Transform t = text.transform;
            if (t.parent != header)
                continue;
            if (t.name == FoldedTitleName)
                continue;

            RectTransform rect = text.rectTransform;
            Vector2 offsetMax = rect.offsetMax;
            offsetMax.x = Mathf.Min(offsetMax.x, -rightInset);
            rect.offsetMax = offsetMax;
        }
    }

    static TMP_Text EnsureFoldedTitle(RectTransform header, string text)
    {
        Transform existing = header.Find(FoldedTitleName);
        TMP_Text tmp = existing != null
            ? existing.GetComponent<TMP_Text>()
            : CreateTmp(header, FoldedTitleName, text, UIWindowChromeLayout.ButtonFontSize);

        RectTransform rect = tmp.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(UIWindowChromeLayout.ButtonEdgePadding, 0f);
        rect.offsetMax = new Vector2(-UIWindowChromeLayout.ClusterWidth(2), 0f);
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.raycastTarget = false;
        tmp.gameObject.SetActive(false);
        DistUiFont.Apply(tmp);
        return tmp;
    }

    static TMP_Text CreateTmp(Transform parent, string name, string text, float fontSize)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        DistUiFont.Apply(tmp);
        tmp.fontSize = fontSize;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.text = text;
        tmp.raycastTarget = false;
        return tmp;
    }
}
#endif
