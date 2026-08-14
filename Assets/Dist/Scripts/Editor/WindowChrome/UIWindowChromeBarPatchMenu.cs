// ============================================================
// UIWindowChromeBarPatchMenu — Dist/MCP 공용 헤더 접기/끄기 패치
// ============================================================

#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

static class UIWindowChromeBarPatchMenu
{
    const string InventoryPath =
        "Assets/Dist/Visual/Prefabs/UIComponents/Inventory/Grp_InventoryListWindow.prefab";
    const string CharacterPath =
        "Assets/Dist/Visual/Prefabs/UIComponents/PlayerStatus/Grp_PlayerStatusWindow.prefab";
    const string CraftingPath =
        "Assets/Dist/Visual/Prefabs/UIComponents/Crafting/Grp_CraftingWindow.prefab";
    const string TimePath =
        "Assets/Dist/Visual/Prefabs/UIComponents/Time/Grp_TimeDisplay.prefab";
    const string SummaryPath =
        "Assets/Dist/Visual/Prefabs/UIComponents/PlayerStatus/Grp_PlayerStatusSummary.prefab";
    const string MessageLogPath =
        "Assets/Dist/Visual/Prefabs/UIComponents/MessageLog/Hud_MessageLog.prefab";

    [MenuItem(DistMcpMenus.WindowChromePatchFoldCloseButtons)]
    static void PatchFoldCloseButtons()
    {
        Patch(InventoryPath, createHeader: false, foldedTitle: false, null);
        Patch(CharacterPath, createHeader: false, foldedTitle: false, null);
        Patch(CraftingPath, createHeader: false, foldedTitle: false, null);
        Patch(TimePath, createHeader: false, foldedTitle: true, string.Empty);
        Patch(SummaryPath, createHeader: false, foldedTitle: false, null);
        PatchMessageLog(MessageLogPath);
        AssetDatabase.SaveAssets();
        Debug.Log("[UIWindowChromeBarPatchMenu] Fold/close buttons patched on window prefabs.");
    }

    static void Patch(string path, bool createHeader, bool foldedTitle, string foldedTitleText)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        if (root == null)
        {
            Debug.LogError($"[UIWindowChromeBarPatchMenu] Failed to load: {path}");
            return;
        }

        try
        {
            UIWindowChromeBarPrefabPatch.Apply(root, createHeader, foldedTitle, foldedTitleText);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    static void PatchMessageLog(string path)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        if (root == null)
        {
            Debug.LogError($"[UIWindowChromeBarPatchMenu] Failed to load: {path}");
            return;
        }

        try
        {
            EnsureMessageLogViewportInset(root);
            UIWindowChromeBarPrefabPatch.Apply(
                root,
                createHeaderIfMissing: true,
                addFoldedTitle: false,
                foldedTitleText: null);

            Transform header = root.transform.Find("Area_Header");
            if (header != null && header.Find("Txt_Title") == null)
            {
                TMP_Text title = CreateHeaderTitle(header, MessageLogLabels.Title);
                InsetTitle(title);
            }

            UIWindowDragHandler drag = header != null
                ? header.GetComponent<UIWindowDragHandler>()
                : null;
            if (drag != null)
                drag.SetProximityRevealEnabled(true);

            UIMessageLogPanel panel = root.GetComponent<UIMessageLogPanel>();
            if (panel != null && header != null)
            {
                TMP_Text title = header.Find("Txt_Title")?.GetComponent<TMP_Text>();
                var so = new SerializedObject(panel);
                SerializedProperty titleProp = so.FindProperty("_headerTitle");
                if (titleProp != null)
                    titleProp.objectReferenceValue = title;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    static void EnsureMessageLogViewportInset(GameObject root)
    {
        Transform viewport = root.transform.Find("Viewport");
        if (viewport == null)
            return;

        RectTransform rect = viewport as RectTransform;
        if (rect == null)
            return;

        Vector2 offsetMax = rect.offsetMax;
        float top = UIWindowChromeLayout.FoldedHeaderHeight + 2f;
        if (offsetMax.y > -top)
            offsetMax.y = -top;
        rect.offsetMax = offsetMax;
    }

    static TMP_Text CreateHeaderTitle(Transform header, string text)
    {
        var go = new GameObject(
            "Txt_Title",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        go.layer = header.gameObject.layer;
        go.transform.SetParent(header, false);
        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        DistUiFont.Apply(tmp);
        tmp.fontSize = UIWindowChromeLayout.ButtonFontSize;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.text = text;
        tmp.raycastTarget = false;
        RectTransform rect = tmp.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(UIWindowChromeLayout.ButtonEdgePadding, 0f);
        rect.offsetMax = new Vector2(-UIWindowChromeLayout.ClusterWidth(2), 0f);
        return tmp;
    }

    static void InsetTitle(TMP_Text title)
    {
        if (title == null)
            return;

        RectTransform rect = title.rectTransform;
        Vector2 offsetMax = rect.offsetMax;
        offsetMax.x = Mathf.Min(offsetMax.x, -UIWindowChromeLayout.ClusterWidth(2));
        rect.offsetMax = offsetMax;
    }
}
#endif
