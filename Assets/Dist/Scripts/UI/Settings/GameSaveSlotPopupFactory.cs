// ============================================================
// GameSaveSlotPopupFactory — 슬롯 팝업 계층 생성 (프리팹/MCP용)
// ============================================================

using IsoTilemap;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class GameSaveSlotPopupFactory
{
    public const string DefaultPrefabPath =
        "Assets/Dist/Visual/Prefabs/UIComponents/Settings/Grp_GameSaveSlotPopup.prefab";

    public static UIGameSaveSlotPopup CreatePopupRoot()
    {
        GameObject root = CreateRect("Grp_GameSaveSlotPopup", null, GameSaveSlotPopupLayout.BackdropColor);
        RectTransform rootRect = root.GetComponent<RectTransform>();
        Stretch(rootRect, 0f, 0f, 0f, 0f);
        root.GetComponent<Image>().raycastTarget = true;

        GameObject panelGo = CreateRect("Panel", root.transform, GameSaveSlotPopupLayout.PanelColor);
        RectTransform panelRect = panelGo.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(
            GameSaveSlotPopupLayout.PanelWidth,
            GameSaveSlotPopupLayout.PanelHeight);

        GameObject headerGo = CreateRect("Area_Header", panelGo.transform, GameSaveSlotPopupLayout.HeaderColor);
        RectTransform headerRect = headerGo.GetComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0f, 1f);
        headerRect.anchorMax = new Vector2(1f, 1f);
        headerRect.pivot = new Vector2(0.5f, 1f);
        headerRect.anchoredPosition = Vector2.zero;
        headerRect.sizeDelta = new Vector2(0f, GameSaveSlotPopupLayout.HeaderHeight);
        headerGo.GetComponent<Image>().raycastTarget = true;

        TMP_Text title = CreateTmp(
            "Txt_Title",
            headerGo.transform,
            GameSaveSlotPopupLayout.FontSizeHeader,
            TextAlignmentOptions.MidlineLeft);
        Stretch(title.rectTransform, GameSaveSlotPopupLayout.ChromePadding, 72f, 0f, 0f);

        Button closeButton = CreateActionButton(headerGo.transform, "Btn_Close", 56f, 24f);
        RectTransform closeRect = closeButton.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(1f, 0.5f);
        closeRect.anchorMax = new Vector2(1f, 0.5f);
        closeRect.pivot = new Vector2(1f, 0.5f);
        closeRect.anchoredPosition = new Vector2(-GameSaveSlotPopupLayout.ChromePadding, 0f);

        GameObject listGo = CreateRect("Area_SlotList", panelGo.transform, Color.clear);
        RectTransform listRect = listGo.GetComponent<RectTransform>();
        listRect.anchorMin = Vector2.zero;
        listRect.anchorMax = Vector2.one;
        listRect.offsetMin = new Vector2(
            GameSaveSlotPopupLayout.ChromePadding,
            GameSaveSlotPopupLayout.ChromePadding);
        listRect.offsetMax = new Vector2(
            -GameSaveSlotPopupLayout.ChromePadding,
            -(GameSaveSlotPopupLayout.HeaderHeight + GameSaveSlotPopupLayout.ChromePadding));

        VerticalLayoutGroup stack = listGo.AddComponent<VerticalLayoutGroup>();
        stack.childAlignment = TextAnchor.UpperLeft;
        stack.spacing = GameSaveSlotPopupLayout.SlotStackSpacing;
        stack.childControlWidth = true;
        stack.childControlHeight = true;
        stack.childForceExpandWidth = true;
        stack.childForceExpandHeight = false;

        var slotButtons = new Button[GameSaveSlotPaths.SlotCount];
        var slotTitles = new TMP_Text[GameSaveSlotPaths.SlotCount];
        var slotSubtitles = new TMP_Text[GameSaveSlotPaths.SlotCount];

        for (int i = 0; i < GameSaveSlotPaths.SlotCount; i++)
        {
            Button slotButton = CreateSlotRow(listGo.transform, $"Btn_Slot_{i + 1:D2}", out TMP_Text rowTitle, out TMP_Text rowSubtitle);
            slotButtons[i] = slotButton;
            slotTitles[i] = rowTitle;
            slotSubtitles[i] = rowSubtitle;
        }

        GameObject confirmGo = CreateRect("Area_Confirm", panelGo.transform, GameSaveSlotPopupLayout.ConfirmColor);
        RectTransform confirmRect = confirmGo.GetComponent<RectTransform>();
        Stretch(confirmRect, GameSaveSlotPopupLayout.ChromePadding);
        confirmGo.SetActive(false);

        TMP_Text confirmMessage = CreateTmp(
            "Txt_Message",
            confirmGo.transform,
            GameSaveSlotPopupLayout.FontSizeBody,
            TextAlignmentOptions.TopLeft);
        RectTransform messageRect = confirmMessage.rectTransform;
        messageRect.anchorMin = new Vector2(0f, 1f);
        messageRect.anchorMax = new Vector2(1f, 1f);
        messageRect.pivot = new Vector2(0.5f, 1f);
        messageRect.anchoredPosition = Vector2.zero;
        messageRect.sizeDelta = new Vector2(0f, GameSaveSlotPopupLayout.ConfirmPanelHeight - 40f);

        GameObject confirmActions = CreateRect("Area_Actions", confirmGo.transform, Color.clear);
        RectTransform actionsRect = confirmActions.GetComponent<RectTransform>();
        actionsRect.anchorMin = new Vector2(0f, 0f);
        actionsRect.anchorMax = new Vector2(1f, 0f);
        actionsRect.pivot = new Vector2(0.5f, 0f);
        actionsRect.anchoredPosition = Vector2.zero;
        actionsRect.sizeDelta = new Vector2(0f, GameSaveSlotPopupLayout.ActionButtonHeight);

        HorizontalLayoutGroup actionsLayout = confirmActions.AddComponent<HorizontalLayoutGroup>();
        actionsLayout.childAlignment = TextAnchor.MiddleRight;
        actionsLayout.spacing = GameSaveSlotPopupLayout.ActionButtonSpacing;
        actionsLayout.childControlWidth = true;
        actionsLayout.childControlHeight = true;
        actionsLayout.childForceExpandWidth = false;
        actionsLayout.childForceExpandHeight = true;

        Button confirmYes = CreateActionButton(confirmActions.transform, "Btn_ConfirmYes", 72f, GameSaveSlotPopupLayout.ActionButtonHeight);
        Button confirmNo = CreateActionButton(confirmActions.transform, "Btn_ConfirmNo", 72f, GameSaveSlotPopupLayout.ActionButtonHeight);

        root.AddComponent<UIOverlayWindow>();
        UIGameSaveSlotPopup popup = root.AddComponent<UIGameSaveSlotPopup>();
        popup.Wire(
            rootRect,
            title,
            closeButton,
            slotButtons,
            slotTitles,
            slotSubtitles,
            confirmGo,
            confirmMessage,
            confirmYes,
            confirmNo);
        return popup;
    }

    static Button CreateSlotRow(Transform parent, string name, out TMP_Text title, out TMP_Text subtitle)
    {
        GameObject row = CreateRect(name, parent, GameSaveSlotPopupLayout.SlotColor);
        LayoutElement layout = row.AddComponent<LayoutElement>();
        layout.preferredHeight = GameSaveSlotPopupLayout.SlotRowHeight;
        layout.minHeight = GameSaveSlotPopupLayout.SlotRowHeight;
        row.GetComponent<Image>().raycastTarget = true;

        Button button = row.AddComponent<Button>();

        title = CreateTmp("Txt_Title", row.transform, GameSaveSlotPopupLayout.FontSizeBody, TextAlignmentOptions.BottomLeft);
        RectTransform titleRect = title.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 0.5f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.offsetMin = new Vector2(8f, 0f);
        titleRect.offsetMax = new Vector2(-8f, -2f);

        subtitle = CreateTmp("Txt_Subtitle", row.transform, GameSaveSlotPopupLayout.FontSizeSubtitle, TextAlignmentOptions.TopLeft);
        RectTransform subtitleRect = subtitle.rectTransform;
        subtitleRect.anchorMin = new Vector2(0f, 0f);
        subtitleRect.anchorMax = new Vector2(1f, 0.5f);
        subtitleRect.offsetMin = new Vector2(8f, 2f);
        subtitleRect.offsetMax = new Vector2(-8f, 0f);
        subtitle.color = new Color(0.75f, 0.75f, 0.78f, 1f);

        return button;
    }

    static Button CreateActionButton(Transform parent, string name, float width, float height)
    {
        GameObject go = CreateRect(name, parent, GameSaveSlotPopupLayout.SlotColor);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(width, height);
        go.GetComponent<Image>().raycastTarget = true;

        LayoutElement layout = go.AddComponent<LayoutElement>();
        layout.preferredWidth = width;
        layout.minWidth = width;
        layout.preferredHeight = height;
        layout.minHeight = height;

        Button button = go.AddComponent<Button>();
        TMP_Text label = CreateTmp("Label", go.transform, GameSaveSlotPopupLayout.FontSizeBody, TextAlignmentOptions.Center);
        Stretch(label.rectTransform, 4f, 4f, 0f, 0f);
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

    static void Stretch(RectTransform rect, float padding) =>
        Stretch(rect, padding, padding, padding, padding);

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
        return UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(SettingsUIFactory.DefaultUIFontPath);
#else
        return DistUiFont.Get();
#endif
    }
}
