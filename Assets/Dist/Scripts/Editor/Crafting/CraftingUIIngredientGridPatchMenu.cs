// ============================================================
// CraftingUIIngredientGridPatchMenu — 재료/출력 그리드 아이콘 Patch
// ============================================================

#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

static class CraftingUIIngredientGridPatchMenu
{
    const string WindowPrefabPath =
        "Assets/Dist/Visual/Prefabs/UIComponents/Crafting/Grp_CraftingWindow.prefab";
    const string CardPrefabPath =
        "Assets/Dist/Visual/Prefabs/UIComponents/Crafting/Grp_IngredientCard.prefab";
    const string ScrollIngredientsName = "Scroll_Ingredients";
    const string ScrollOutputsName = "Scroll_Outputs";
    const string ContentName = "Content";
    const string IconName = "Icon";
    const string KindIconName = "KindIcon";
    const string CountName = "Count";
    const string NameName = "Name";
    const string TextsName = "Texts";
    const string SwapName = "Btn_Swap";

    [MenuItem(DistMcpMenus.CraftingPatchIngredientGrid)]
    static void PatchIngredientGrid()
    {
        if (!PatchCardPrefab())
            return;
        if (!PatchWindowContents())
            return;

        Debug.Log("[CraftingUIIngredientGridPatchMenu] Patched ingredient card + grid contents.");
    }

    static bool PatchCardPrefab()
    {
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(CardPrefabPath);
        if (existing == null)
        {
            Debug.LogError($"[CraftingUIIngredientGridPatchMenu] Missing prefab: {CardPrefabPath}");
            return false;
        }

        GameObject root = PrefabUtility.LoadPrefabContents(CardPrefabPath);
        if (root == null)
        {
            Debug.LogError($"[CraftingUIIngredientGridPatchMenu] Failed to load: {CardPrefabPath}");
            return false;
        }

        try
        {
            HorizontalLayoutGroup hlg = root.GetComponent<HorizontalLayoutGroup>();
            if (hlg != null)
                Object.DestroyImmediate(hlg);

            Vector2 cell = CraftingWindowLayout.IngredientGridCellSize;
            RectTransform rootRt = root.GetComponent<RectTransform>();
            rootRt.sizeDelta = cell;

            LayoutElement rootLe = root.GetComponent<LayoutElement>();
            if (rootLe == null)
                rootLe = root.AddComponent<LayoutElement>();
            rootLe.minWidth = cell.x;
            rootLe.preferredWidth = cell.x;
            rootLe.flexibleWidth = 0f;
            rootLe.minHeight = cell.y;
            rootLe.preferredHeight = cell.y;
            rootLe.flexibleHeight = 0f;

            Transform iconT = FindNamed(root.transform, IconName);
            Transform kindT = FindNamed(root.transform, KindIconName);
            Transform countT = FindNamed(root.transform, CountName);
            Transform nameT = FindNamed(root.transform, NameName);
            Transform swapT = FindNamed(root.transform, SwapName);
            if (iconT == null || kindT == null || countT == null || nameT == null || swapT == null)
            {
                Debug.LogError("[CraftingUIIngredientGridPatchMenu] Card children missing.");
                return false;
            }

            kindT.SetParent(iconT, false);
            countT.SetParent(iconT, false);
            nameT.SetParent(iconT, false);
            kindT.SetAsLastSibling();
            countT.SetAsLastSibling();

            Transform texts = FindNamed(root.transform, TextsName);
            if (texts != null)
                Object.DestroyImmediate(texts.gameObject);

            SetLeftSquare(iconT as RectTransform, CraftingWindowLayout.IngredientCellSize);
            IgnoreLayout(iconT);
            Image iconImage = iconT.GetComponent<Image>();
            if (iconImage != null)
            {
                iconImage.preserveAspect = true;
                iconImage.raycastTarget = true;
            }

            SetTopLeft(
                kindT as RectTransform,
                CraftingWindowLayout.IngredientKindBadgeSize,
                CraftingWindowLayout.IngredientOverlayInset);
            IgnoreLayout(kindT);
            Image kindImage = kindT.GetComponent<Image>();
            if (kindImage != null)
                kindImage.raycastTarget = false;

            SetTopRight(
                countT as RectTransform,
                CraftingWindowLayout.IngredientCountWidth,
                CraftingWindowLayout.IngredientCountHeight,
                CraftingWindowLayout.IngredientOverlayInset);
            IgnoreLayout(countT);
            PatchCount(countT.GetComponent<TMP_Text>());

            SetBottomStrip(
                nameT as RectTransform,
                CraftingWindowLayout.IngredientQualityNameHeight,
                CraftingWindowLayout.IngredientOverlayInset);
            IgnoreLayout(nameT);
            PatchQualityName(nameT.GetComponent<TMP_Text>());
            nameT.gameObject.SetActive(false);

            SetRightStrip(swapT as RectTransform, CraftingWindowLayout.IngredientSwapWidth);
            IgnoreLayout(swapT);
            PatchSwapLabel(swapT);
            swapT.gameObject.SetActive(false);

            UICraftingIngredientCard card = root.GetComponent<UICraftingIngredientCard>();
            if (card == null)
            {
                Debug.LogError("[CraftingUIIngredientGridPatchMenu] UICraftingIngredientCard missing.");
                return false;
            }

            SerializedObject so = new(card);
            so.FindProperty("_icon").objectReferenceValue = iconImage;
            so.FindProperty("_kindIcon").objectReferenceValue = kindImage;
            so.FindProperty("_name").objectReferenceValue = nameT.GetComponent<TMP_Text>();
            so.FindProperty("_count").objectReferenceValue = countT.GetComponent<TMP_Text>();
            so.FindProperty("_iconButton").objectReferenceValue = iconT.GetComponent<Button>();
            so.FindProperty("_swapButton").objectReferenceValue = swapT.GetComponent<Button>();
            so.ApplyModifiedPropertiesWithoutUndo();

            bool saved = PrefabUtility.SaveAsPrefabAsset(root, CardPrefabPath);
            if (!saved)
            {
                Debug.LogError("[CraftingUIIngredientGridPatchMenu] Failed to save card prefab.");
                return false;
            }

            return true;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    static bool PatchWindowContents()
    {
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(WindowPrefabPath);
        if (existing == null)
        {
            Debug.LogError($"[CraftingUIIngredientGridPatchMenu] Missing prefab: {WindowPrefabPath}");
            return false;
        }

        GameObject root = PrefabUtility.LoadPrefabContents(WindowPrefabPath);
        if (root == null)
        {
            Debug.LogError($"[CraftingUIIngredientGridPatchMenu] Failed to load: {WindowPrefabPath}");
            return false;
        }

        try
        {
            Transform ingredients = FindNamed(root.transform, ScrollIngredientsName);
            Transform outputs = FindNamed(root.transform, ScrollOutputsName);
            if (ingredients == null || outputs == null)
            {
                Debug.LogError("[CraftingUIIngredientGridPatchMenu] Scroll_Ingredients or Scroll_Outputs missing.");
                return false;
            }

            PatchGridContent(FindNamed(ingredients, ContentName));
            PatchGridContent(FindNamed(outputs, ContentName));

            bool saved = PrefabUtility.SaveAsPrefabAsset(root, WindowPrefabPath);
            if (!saved)
            {
                Debug.LogError("[CraftingUIIngredientGridPatchMenu] Failed to save window prefab.");
                return false;
            }

            return true;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    static void PatchGridContent(Transform content)
    {
        if (content == null)
        {
            Debug.LogError("[CraftingUIIngredientGridPatchMenu] Content missing.");
            return;
        }

        VerticalLayoutGroup vlg = content.GetComponent<VerticalLayoutGroup>();
        if (vlg != null)
            Object.DestroyImmediate(vlg);

        GridLayoutGroup grid = content.GetComponent<GridLayoutGroup>();
        if (grid == null)
            grid = content.gameObject.AddComponent<GridLayoutGroup>();

        grid.cellSize = CraftingWindowLayout.IngredientGridCellSize;
        grid.spacing = new Vector2(
            CraftingWindowLayout.IngredientGridSpacing,
            CraftingWindowLayout.IngredientGridSpacing);
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.UpperLeft;
        grid.constraint = GridLayoutGroup.Constraint.Flexible;
        grid.padding = new RectOffset(0, 0, 0, 0);

        ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
        if (fitter == null)
            fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    static void PatchCount(TMP_Text text)
    {
        if (text == null)
            return;

        text.font = LoadFont();
        text.fontSize = CraftingWindowLayout.FontSizeSmall;
        text.alignment = TextAlignmentOptions.TopRight;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;
        DistUiFont.Apply(text);
    }

    static void PatchQualityName(TMP_Text text)
    {
        if (text == null)
            return;

        text.font = LoadFont();
        text.fontSize = CraftingWindowLayout.FontSizeSmall;
        text.alignment = TextAlignmentOptions.Bottom;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.raycastTarget = false;
        DistUiFont.Apply(text);
    }

    static void PatchSwapLabel(Transform swap)
    {
        TMP_Text label = swap.GetComponentInChildren<TMP_Text>(true);
        if (label == null)
            return;

        label.font = LoadFont();
        label.fontSize = CraftingWindowLayout.FontSizeSmall;
        label.alignment = TextAlignmentOptions.Center;
        label.overflowMode = TextOverflowModes.Ellipsis;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        DistUiFont.Apply(label);
    }

    static void SetLeftSquare(RectTransform rt, float size)
    {
        if (rt == null)
            return;

        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.sizeDelta = new Vector2(size, 0f);
        rt.anchoredPosition = Vector2.zero;
        rt.localScale = Vector3.one;
    }

    static void SetRightStrip(RectTransform rt, float width)
    {
        if (rt == null)
            return;

        rt.anchorMin = new Vector2(1f, 0f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 0.5f);
        rt.sizeDelta = new Vector2(width, 0f);
        rt.anchoredPosition = Vector2.zero;
        rt.localScale = Vector3.one;
    }

    static void SetTopLeft(RectTransform rt, float size, float inset)
    {
        if (rt == null)
            return;

        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.sizeDelta = new Vector2(size, size);
        rt.anchoredPosition = new Vector2(inset, -inset);
        rt.localScale = Vector3.one;
    }

    static void SetTopRight(RectTransform rt, float width, float height, float inset)
    {
        if (rt == null)
            return;

        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.sizeDelta = new Vector2(width, height);
        rt.anchoredPosition = new Vector2(-inset, -inset);
        rt.localScale = Vector3.one;
    }

    static void SetBottomStrip(RectTransform rt, float height, float inset)
    {
        if (rt == null)
            return;

        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.sizeDelta = new Vector2(-inset * 2f, height);
        rt.anchoredPosition = new Vector2(0f, inset);
        rt.localScale = Vector3.one;
    }

    static void IgnoreLayout(Transform t)
    {
        if (t == null)
            return;

        LayoutElement le = t.GetComponent<LayoutElement>();
        if (le == null)
            le = t.gameObject.AddComponent<LayoutElement>();
        le.ignoreLayout = true;
    }

    static TMP_FontAsset LoadFont()
    {
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(DistUiFont.AssetPath);
        if (font == null)
            Debug.LogError($"[CraftingUIIngredientGridPatchMenu] Font missing: {DistUiFont.AssetPath}");
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
