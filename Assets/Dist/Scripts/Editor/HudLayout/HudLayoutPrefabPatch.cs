// ============================================================
// HudLayoutPrefabPatch — HUD 프리팹에 HudLayoutParticipant·LayoutHit 부착
// ============================================================

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

static class HudLayoutPrefabPatch
{
    public static void Apply(
        GameObject root,
        string participantId,
        Vector2 minSize,
        Vector2 maxSize,
        bool ensureOverlayWindow = true,
        bool ensureResizeChrome = false,
        float resizeEdgeThickness = UIWindowResizeHandles.DefaultHandleWidth)
    {
        if (root == null)
            return;

        RectTransform window = root.transform as RectTransform;

        if (ensureOverlayWindow && root.GetComponent<UIOverlayWindow>() == null)
            root.AddComponent<UIOverlayWindow>();

        Transform header = root.transform.Find("Area_Header");
        UIWindowDragHandler headerDrag = header != null
            ? header.GetComponent<UIWindowDragHandler>()
            : null;

        UIWindowChromeBar chromeBar = header != null
            ? header.GetComponent<UIWindowChromeBar>()
            : null;

        Transform layoutHit = root.transform.Find("Area_LayoutHit");
        UIWindowDragHandler layoutDrag;
        if (layoutHit == null)
        {
            GameObject layoutGo = new GameObject(
                "Area_LayoutHit",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            layoutGo.transform.SetParent(root.transform, false);
            layoutGo.layer = LayerMask.NameToLayer("UI");
            RectTransform layoutRect = layoutGo.GetComponent<RectTransform>();
            StretchFill(layoutRect);
            Image layoutImage = layoutGo.GetComponent<Image>();
            layoutImage.color = Color.clear;
            layoutImage.raycastTarget = false;
            if (layoutGo.GetComponent<CanvasGroup>() == null)
            {
                CanvasGroup layoutGroup = layoutGo.AddComponent<CanvasGroup>();
                layoutGroup.alpha = 0f;
                layoutGroup.blocksRaycasts = false;
                layoutGroup.interactable = false;
            }
            layoutHit = layoutGo.transform;
        }
        else if (layoutHit.GetComponent<CanvasGroup>() == null)
        {
            CanvasGroup layoutGroup = layoutHit.gameObject.AddComponent<CanvasGroup>();
            layoutGroup.alpha = 0f;
            layoutGroup.blocksRaycasts = false;
            layoutGroup.interactable = false;
        }

        layoutDrag = layoutHit.GetComponent<UIWindowDragHandler>();
        if (layoutDrag == null)
            layoutDrag = layoutHit.gameObject.AddComponent<UIWindowDragHandler>();

        var layoutSo = new SerializedObject(layoutDrag);
        layoutSo.FindProperty("_window").objectReferenceValue = window;
        layoutSo.ApplyModifiedPropertiesWithoutUndo();
        layoutDrag.SetVisualActive(false);

        // 루트 Drag는 Time 시계용. HUD는 Area_Header + Area_LayoutHit만.
        UIWindowDragHandler rootDrag = root.GetComponent<UIWindowDragHandler>();
        if (rootDrag != null && (headerDrag != null || layoutDrag != null))
            Object.DestroyImmediate(rootDrag);

        UIWindowResizeHandles resizeHandles = root.GetComponent<UIWindowResizeHandles>();
        UIWindowResizeProximity proximity = root.GetComponent<UIWindowResizeProximity>();

        if (ensureResizeChrome)
        {
            resizeHandles = UIWindowResizeHandlesPrefabPatch.Apply(
                root,
                resizeEdgeThickness,
                proximityReveal: false,
                minSize,
                maxSize);

            // HUD 조정 모드는 AlwaysHit. Time 근접 리빌은 ensureResizeChrome=false 경로만 유지.
            proximity = root.GetComponent<UIWindowResizeProximity>();
            if (proximity != null)
                Object.DestroyImmediate(proximity);
            proximity = null;
        }

        HudLayoutParticipant participant = root.GetComponent<HudLayoutParticipant>();
        if (participant == null)
            participant = root.AddComponent<HudLayoutParticipant>();

        var partSo = new SerializedObject(participant);
        partSo.FindProperty("_participantId").stringValue = participantId;
        partSo.FindProperty("_window").objectReferenceValue = window;
        partSo.FindProperty("_headerDrag").objectReferenceValue = headerDrag;
        partSo.FindProperty("_layoutDrag").objectReferenceValue = layoutDrag;
        partSo.FindProperty("_chromeBar").objectReferenceValue = chromeBar;
        partSo.FindProperty("_resizeHandles").objectReferenceValue = resizeHandles;
        partSo.FindProperty("_resizeProximity").objectReferenceValue = proximity;
        partSo.FindProperty("_minSize").vector2Value = minSize;
        partSo.FindProperty("_maxSize").vector2Value = maxSize;
        partSo.ApplyModifiedPropertiesWithoutUndo();
    }

    static void StretchFill(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
#endif
