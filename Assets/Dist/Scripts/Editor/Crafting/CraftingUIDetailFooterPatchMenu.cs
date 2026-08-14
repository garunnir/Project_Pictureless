// ============================================================
// CraftingUIDetailFooterPatchMenu — 상세 열 Outputs·수량·진행바 Patch
// ============================================================

#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

static class CraftingUIDetailFooterPatchMenu
{
    const string WindowPrefabPath =
        "Assets/Dist/Visual/Prefabs/UIComponents/Crafting/Grp_CraftingWindow.prefab";
    const string ColDetailName = "Col_Detail";
    const string LblRequiredName = "Lbl_Required";
    const string ScrollIngredientsName = "Scroll_Ingredients";
    const string BtnCraftName = "Btn_Craft";
    const string InputSearchName = "Input_Search";
    const string LblOutputsName = "Lbl_Outputs";
    const string ScrollOutputsName = "Scroll_Outputs";
    const string AreaFooterName = "Area_Footer";
    const string AreaQtyName = "Area_Qty";
    const string InputQtyName = "Input_Qty";
    const string BtnQtyMinusName = "Btn_QtyMinus";
    const string BtnQtyPlusName = "Btn_QtyPlus";
    const string BtnQtyMaxName = "Btn_QtyMax";
    const string TxtTimeRequiredName = "Txt_TimeRequired";
    const string AreaProgressName = "Area_Progress";
    const string ImgProgressFillName = "Img_ProgressFill";
    const string TxtSkillsName = "Txt_Skills";

    [MenuItem(DistMcpMenus.CraftingPatchDetailFooter)]
    static void PatchDetailFooter()
    {
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(WindowPrefabPath);
        if (existing == null)
        {
            Debug.LogError($"[CraftingUIDetailFooterPatchMenu] Missing prefab: {WindowPrefabPath}");
            return;
        }

        GameObject root = PrefabUtility.LoadPrefabContents(WindowPrefabPath);
        if (root == null)
        {
            Debug.LogError($"[CraftingUIDetailFooterPatchMenu] Failed to load: {WindowPrefabPath}");
            return;
        }

        try
        {
            Transform colDetail = FindNamed(root.transform, ColDetailName);
            if (colDetail == null)
            {
                Debug.LogError("[CraftingUIDetailFooterPatchMenu] Col_Detail missing.");
                return;
            }

            Transform scrollIngredients = FindNamed(colDetail, ScrollIngredientsName);
            Transform lblRequired = FindNamed(colDetail, LblRequiredName);
            Transform btnCraft = FindNamed(root.transform, BtnCraftName);
            Transform inputSearch = FindNamed(root.transform, InputSearchName);
            if (scrollIngredients == null || lblRequired == null || btnCraft == null)
            {
                Debug.LogError("[CraftingUIDetailFooterPatchMenu] Required detail children missing.");
                return;
            }

            TMP_Text lblOutputs = EnsureOutputsHeader(colDetail, lblRequired, scrollIngredients);
            RectTransform outputContent = EnsureOutputsScroll(colDetail, scrollIngredients);
            Transform footer = EnsureFooter(colDetail, scrollIngredients, btnCraft, inputSearch);
            EnsureSkillsStatusFit(colDetail);
            WireWindow(root, lblOutputs, outputContent, footer);

            bool saved = PrefabUtility.SaveAsPrefabAsset(root, WindowPrefabPath);
            if (!saved)
            {
                Debug.LogError("[CraftingUIDetailFooterPatchMenu] Failed to save prefab.");
                return;
            }

            Debug.Log("[CraftingUIDetailFooterPatchMenu] Patched Outputs + footer + status text.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    static TMP_Text EnsureOutputsHeader(
        Transform colDetail,
        Transform lblRequired,
        Transform scrollIngredients)
    {
        Transform existing = FindNamed(colDetail, LblOutputsName);
        GameObject go;
        if (existing != null)
        {
            go = existing.gameObject;
        }
        else
        {
            go = Object.Instantiate(lblRequired.gameObject, colDetail);
            go.name = LblOutputsName;
        }

        go.transform.SetSiblingIndex(scrollIngredients.GetSiblingIndex() + 1);
        TMP_Text text = go.GetComponent<TMP_Text>();
        if (text != null)
        {
            text.font = LoadFont();
            text.text = "결과";
        }

        return text;
    }

    static RectTransform EnsureOutputsScroll(Transform colDetail, Transform scrollIngredients)
    {
        Transform existing = FindNamed(colDetail, ScrollOutputsName);
        GameObject go;
        if (existing != null)
        {
            go = existing.gameObject;
        }
        else
        {
            go = Object.Instantiate(scrollIngredients.gameObject, colDetail);
            go.name = ScrollOutputsName;
        }

        go.transform.SetSiblingIndex(scrollIngredients.GetSiblingIndex() + 2);
        LayoutElement le = go.GetComponent<LayoutElement>();
        if (le != null)
        {
            le.minHeight = CraftingWindowLayout.OutputsScrollMinHeight;
            le.preferredHeight = CraftingWindowLayout.OutputsScrollMinHeight;
            le.flexibleHeight = 1f;
        }

        ScrollRect scroll = go.GetComponent<ScrollRect>();
        Transform content = FindNamed(go.transform, "Content");
        RectTransform contentRt = content as RectTransform;
        if (scroll != null && contentRt != null)
            scroll.content = contentRt;

        return contentRt;
    }

    static void EnsureSkillsStatusFit(Transform colDetail)
    {
        Transform skills = FindNamed(colDetail, TxtSkillsName);
        if (skills == null)
        {
            Debug.LogWarning("[CraftingUIDetailFooterPatchMenu] Txt_Skills missing.");
            return;
        }

        TMP_Text text = skills.GetComponent<TMP_Text>();
        if (text != null)
        {
            text.font = LoadFont();
            text.alignment = TextAlignmentOptions.TopLeft;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Overflow;
            text.richText = true;
        }

        ContentSizeFitter fitter = skills.GetComponent<ContentSizeFitter>();
        if (fitter == null)
            fitter = skills.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        LayoutElement le = skills.GetComponent<LayoutElement>();
        if (le != null)
        {
            le.minHeight = CraftingWindowLayout.FooterTimeRowHeight * 2f;
            le.preferredHeight = -1f;
            le.flexibleHeight = 0f;
        }
    }

    static Transform EnsureFooter(
        Transform colDetail,
        Transform scrollIngredients,
        Transform btnCraft,
        Transform inputSearch)
    {
        Transform existing = FindNamed(colDetail, AreaFooterName);
        GameObject footerGo;
        if (existing != null)
        {
            footerGo = existing.gameObject;
        }
        else
        {
            footerGo = CreateRect(AreaFooterName, colDetail, Color.clear);
            VerticalLayoutGroup vlg = footerGo.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(
                0,
                0,
                Mathf.RoundToInt(CraftingWindowLayout.FooterPadding),
                Mathf.RoundToInt(CraftingWindowLayout.FooterPadding));
            vlg.spacing = CraftingWindowLayout.FooterSpacing;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            LayoutElement footerLe = footerGo.AddComponent<LayoutElement>();
            footerLe.minHeight = FooterHeight();
            footerLe.preferredHeight = FooterHeight();
            footerLe.flexibleHeight = 0f;
        }

        footerGo.transform.SetSiblingIndex(scrollIngredients.GetSiblingIndex() + 3);

        Transform qty = EnsureQtyRow(footerGo.transform, inputSearch);
        qty.SetSiblingIndex(0);

        TMP_Text timeText = EnsureTmp(
            footerGo.transform,
            TxtTimeRequiredName,
            CraftingWindowLayout.FooterTimeRowHeight,
            CraftingWindowLayout.FontSizeSmall);
        timeText.transform.SetSiblingIndex(1);

        Image fill = EnsureProgress(footerGo.transform);
        fill.transform.parent.SetSiblingIndex(2);

        if (btnCraft.parent != footerGo.transform)
            btnCraft.SetParent(footerGo.transform, false);
        btnCraft.SetAsLastSibling();

        LayoutElement craftLe = btnCraft.GetComponent<LayoutElement>();
        if (craftLe != null)
        {
            craftLe.minHeight = CraftingWindowLayout.FooterCraftButtonHeight;
            craftLe.preferredHeight = CraftingWindowLayout.FooterCraftButtonHeight;
        }

        return footerGo.transform;
    }

    static Transform EnsureQtyRow(Transform footer, Transform inputSearch)
    {
        Transform existing = FindNamed(footer, AreaQtyName);
        GameObject row;
        if (existing != null)
        {
            row = existing.gameObject;
        }
        else
        {
            row = CreateRect(AreaQtyName, footer, Color.clear);
            HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = CraftingWindowLayout.FooterSpacing;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;
            LayoutElement le = row.AddComponent<LayoutElement>();
            le.minHeight = CraftingWindowLayout.FooterQtyRowHeight;
            le.preferredHeight = CraftingWindowLayout.FooterQtyRowHeight;
        }

        EnsureQtyButton(row.transform, BtnQtyMinusName, "-", CraftingWindowLayout.QtyButtonWidth);
        EnsureQtyInput(row.transform, inputSearch);
        EnsureQtyButton(row.transform, BtnQtyPlusName, "+", CraftingWindowLayout.QtyButtonWidth);
        EnsureQtyButton(row.transform, BtnQtyMaxName, "MAX", CraftingWindowLayout.QtyMaxButtonWidth);

        FindNamed(row.transform, BtnQtyMinusName)?.SetSiblingIndex(0);
        FindNamed(row.transform, InputQtyName)?.SetSiblingIndex(1);
        FindNamed(row.transform, BtnQtyPlusName)?.SetSiblingIndex(2);
        FindNamed(row.transform, BtnQtyMaxName)?.SetSiblingIndex(3);
        return row.transform;
    }

    static void EnsureQtyButton(Transform parent, string name, string label, float width)
    {
        Transform existing = FindNamed(parent, name);
        GameObject go;
        if (existing != null)
        {
            go = existing.gameObject;
        }
        else
        {
            go = CreateRect(name, parent, CraftingWindowLayout.ButtonColor);
            go.GetComponent<Image>().raycastTarget = true;
            go.AddComponent<Button>();
            TMP_Text text = CreateChildTmp(go.transform, "Label", label);
            text.alignment = TextAlignmentOptions.Center;
        }

        LayoutElement le = go.GetComponent<LayoutElement>();
        if (le == null)
            le = go.AddComponent<LayoutElement>();
        le.minWidth = width;
        le.preferredWidth = width;
        le.minHeight = CraftingWindowLayout.FooterQtyRowHeight;
        le.preferredHeight = CraftingWindowLayout.FooterQtyRowHeight;

        Image image = go.GetComponent<Image>();
        if (image != null)
            image.raycastTarget = true;
    }

    static void EnsureQtyInput(Transform parent, Transform inputSearch)
    {
        Transform existing = FindNamed(parent, InputQtyName);
        GameObject go;
        if (existing != null)
        {
            go = existing.gameObject;
        }
        else if (inputSearch != null)
        {
            go = Object.Instantiate(inputSearch.gameObject, parent);
            go.name = InputQtyName;
        }
        else
        {
            Debug.LogError("[CraftingUIDetailFooterPatchMenu] Input_Search missing; qty field skipped.");
            return;
        }

        LayoutElement le = go.GetComponent<LayoutElement>();
        if (le == null)
            le = go.AddComponent<LayoutElement>();
        le.minWidth = CraftingWindowLayout.QtyInputWidth;
        le.preferredWidth = CraftingWindowLayout.QtyInputWidth;
        le.flexibleWidth = 0f;
        le.minHeight = CraftingWindowLayout.FooterQtyRowHeight;
        le.preferredHeight = CraftingWindowLayout.FooterQtyRowHeight;

        TMP_InputField field = go.GetComponent<TMP_InputField>();
        if (field == null)
            return;

        field.contentType = TMP_InputField.ContentType.IntegerNumber;
        field.characterLimit = 3;
        field.text = "1";
        DistUiFont.Apply(field.textComponent);
        if (field.placeholder is TMP_Text placeholder)
        {
            DistUiFont.Apply(placeholder);
            placeholder.text = "1";
        }
    }

    static Image EnsureProgress(Transform footer)
    {
        Transform existing = FindNamed(footer, AreaProgressName);
        GameObject track;
        if (existing != null)
        {
            track = existing.gameObject;
        }
        else
        {
            track = CreateRect(AreaProgressName, footer, new Color(0.08f, 0.08f, 0.08f, 1f));
            LayoutElement le = track.AddComponent<LayoutElement>();
            le.minHeight = CraftingWindowLayout.FooterProgressHeight;
            le.preferredHeight = CraftingWindowLayout.FooterProgressHeight;
        }

        Transform fillT = FindNamed(track.transform, ImgProgressFillName);
        Image fill;
        if (fillT != null)
        {
            fill = fillT.GetComponent<Image>();
        }
        else
        {
            GameObject fillGo = CreateRect(ImgProgressFillName, track.transform, new Color(0.45f, 0.7f, 0.4f, 1f));
            RectTransform rt = fillGo.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            fill = fillGo.GetComponent<Image>();
        }

        fill.raycastTarget = false;
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        fill.fillAmount = 0f;
        if (fill.sprite == null)
        {
            Sprite sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            if (sprite == null)
                sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            fill.sprite = sprite;
        }

        Image trackImage = track.GetComponent<Image>();
        if (trackImage != null && trackImage.sprite == null)
            trackImage.sprite = fill.sprite;

        return fill;
    }

    static void WireWindow(
        GameObject root,
        TMP_Text outputsHeader,
        RectTransform outputContent,
        Transform footer)
    {
        UICraftingWindow window = root.GetComponent<UICraftingWindow>();
        if (window == null)
        {
            Debug.LogError("[CraftingUIDetailFooterPatchMenu] UICraftingWindow missing on root.");
            return;
        }

        Transform qtyRow = FindNamed(footer, AreaQtyName);
        if (qtyRow == null)
        {
            Debug.LogError("[CraftingUIDetailFooterPatchMenu] Area_Qty missing after ensure.");
            return;
        }
        Button minus = FindNamed(qtyRow, BtnQtyMinusName)?.GetComponent<Button>();
        Button plus = FindNamed(qtyRow, BtnQtyPlusName)?.GetComponent<Button>();
        Button max = FindNamed(qtyRow, BtnQtyMaxName)?.GetComponent<Button>();
        TMP_Text maxLabel = FindNamed(qtyRow, BtnQtyMaxName)?.GetComponentInChildren<TMP_Text>();
        TMP_InputField qtyField = FindNamed(qtyRow, InputQtyName)?.GetComponent<TMP_InputField>();
        TMP_Text timeText = FindNamed(footer, TxtTimeRequiredName)?.GetComponent<TMP_Text>();
        Image fill = FindNamed(footer, ImgProgressFillName)?.GetComponent<Image>();

        SerializedObject so = new(window);
        so.FindProperty("_outputsHeader").objectReferenceValue = outputsHeader;
        so.FindProperty("_outputContent").objectReferenceValue = outputContent;
        so.FindProperty("_qtyMinusButton").objectReferenceValue = minus;
        so.FindProperty("_qtyPlusButton").objectReferenceValue = plus;
        so.FindProperty("_qtyMaxButton").objectReferenceValue = max;
        so.FindProperty("_qtyMaxLabel").objectReferenceValue = maxLabel;
        so.FindProperty("_quantityField").objectReferenceValue = qtyField;
        so.FindProperty("_timeRequiredText").objectReferenceValue = timeText;
        so.FindProperty("_progressFill").objectReferenceValue = fill;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static TMP_Text EnsureTmp(Transform parent, string name, float height, int fontSize)
    {
        Transform existing = FindNamed(parent, name);
        GameObject go;
        if (existing != null)
        {
            go = existing.gameObject;
        }
        else
        {
            go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            go.layer = LayerMask.NameToLayer("UI");
        }

        LayoutElement le = go.GetComponent<LayoutElement>();
        if (le == null)
            le = go.AddComponent<LayoutElement>();
        le.minHeight = height;
        le.preferredHeight = height;

        TMP_Text text = go.GetComponent<TextMeshProUGUI>();
        text.font = LoadFont();
        text.fontSize = fontSize;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;
        return text;
    }

    static TMP_Text CreateChildTmp(Transform parent, string name, string value)
    {
        GameObject go = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        go.layer = LayerMask.NameToLayer("UI");
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        TMP_Text text = go.GetComponent<TextMeshProUGUI>();
        text.font = LoadFont();
        text.fontSize = CraftingWindowLayout.FontSizeSmall;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        text.text = value;
        return text;
    }

    static GameObject CreateRect(string name, Transform parent, Color color)
    {
        GameObject go = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        go.layer = LayerMask.NameToLayer("UI");
        go.GetComponent<Image>().color = color;
        go.GetComponent<Image>().raycastTarget = false;
        return go;
    }

    static float FooterHeight()
    {
        return CraftingWindowLayout.FooterPadding * 2f
            + CraftingWindowLayout.FooterQtyRowHeight
            + CraftingWindowLayout.FooterTimeRowHeight
            + CraftingWindowLayout.FooterProgressHeight
            + CraftingWindowLayout.FooterCraftButtonHeight
            + CraftingWindowLayout.FooterSpacing * 3f;
    }

    static TMP_FontAsset LoadFont()
    {
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(DistUiFont.AssetPath);
        if (font == null)
            Debug.LogError($"[CraftingUIDetailFooterPatchMenu] Font missing: {DistUiFont.AssetPath}");
        return font;
    }

    static Transform FindNamed(Transform root, string name)
    {
        if (root == null)
            return null;

        Transform direct = root.Find(name);
        if (direct != null)
            return direct;

        Transform[] all = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != null && all[i] != root && all[i].name == name)
                return all[i];
        }

        return null;
    }
}
#endif
