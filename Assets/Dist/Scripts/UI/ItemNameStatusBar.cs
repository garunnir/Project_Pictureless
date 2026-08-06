// ============================================================
// ItemNameStatusBar — 이름 셀 뒤 겹침 fill (내구도 idle | 로딩 busy)
// ============================================================

using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Name 셀 Rect 전체 stretch fill. TMP는 형제 위(앞), fill은 뒤. raycast 없음.
/// </summary>
public sealed class ItemNameStatusBar
{
    public const string FillObjectName = "NameStatusFill";
    public const string LabelObjectName = "Label";

    const int MaxDamageLevel = 4;

    static readonly Color DurabilityColor = new(0.28f, 0.48f, 0.62f, 0.55f);
    static readonly Color ProgressColor = new(0.35f, 0.7f, 0.4f, 0.55f);

    static Sprite _whiteSprite;

    readonly Image _fill;

    ItemNameStatusBar(Image fill)
    {
        _fill = fill;
    }

    /// <summary>
    /// nameText가 Name 루트에 붙어 있으면 Label 자식으로 옮기고 Fill을 뒤에 둔다.
    /// </summary>
    public static ItemNameStatusBar Ensure(ref TMP_Text nameText)
    {
        if (nameText == null)
            return null;

        RectTransform nameCell = ResolveNameCell(ref nameText);
        if (nameCell == null)
            return null;

        Image fill = FindOrCreateFill(nameCell);
        if (fill == null)
            return null;

        if (nameText != null)
            nameText.transform.SetAsLastSibling();

        return new ItemNameStatusBar(fill);
    }

    public static ItemNameStatusBar EnsureOnCell(RectTransform nameCell, TMP_Text nameText)
    {
        if (nameCell == null)
            return null;

        Image fill = FindOrCreateFill(nameCell);
        if (fill == null)
            return null;

        if (nameText != null)
            nameText.transform.SetAsLastSibling();

        return new ItemNameStatusBar(fill);
    }

    public void SetDurability(ItemStack stack)
    {
        if (_fill == null)
            return;

        if (stack?.Item == null ||
            !ItemDurabilityRules.ShouldShowDurability(stack.Item, stack.DamageLevel))
        {
            Clear();
            return;
        }

        float ratio = 1f - Mathf.Clamp01(stack.DamageLevel / (float)MaxDamageLevel);
        _fill.gameObject.SetActive(true);
        _fill.color = DurabilityColor;
        _fill.fillAmount = ratio;
    }

    public void SetProgress01(float progress01)
    {
        if (_fill == null)
            return;

        _fill.gameObject.SetActive(true);
        _fill.color = ProgressColor;
        _fill.fillAmount = Mathf.Clamp01(progress01);
    }

    public void Clear()
    {
        if (_fill == null)
            return;

        _fill.fillAmount = 0f;
        _fill.gameObject.SetActive(false);
    }

    public void Refresh(ItemStack stack, bool progressActive, float progress01)
    {
        if (progressActive)
            SetProgress01(progress01);
        else
            SetDurability(stack);
    }

    static RectTransform ResolveNameCell(ref TMP_Text nameText)
    {
        Transform t = nameText.transform;
        Transform label = t.parent != null ? t.parent.Find(LabelObjectName) : null;
        if (label != null && label.GetComponent<TMP_Text>() == nameText)
            return t.parent as RectTransform;

        // TMP on Name root — promote text to Label child so Fill can sit behind.
        if (t.Find(FillObjectName) == null && t.childCount == 0)
        {
            RectTransform cell = t as RectTransform;
            GameObject labelGo = new(LabelObjectName);
            labelGo.transform.SetParent(cell, false);
            RectTransform labelRt = labelGo.AddComponent<RectTransform>();
            Stretch(labelRt);

            TextMeshProUGUI dst = labelGo.AddComponent<TextMeshProUGUI>();
            CopyTmp(nameText, dst);
            dst.raycastTarget = false;

            DestroyTmp(nameText);
            nameText = dst;
            return cell;
        }

        if (t.parent != null && t.parent.Find(FillObjectName) != null)
            return t.parent as RectTransform;

        return t as RectTransform;
    }

    static void DestroyTmp(TMP_Text tmp)
    {
        if (tmp == null)
            return;

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            Object.DestroyImmediate(tmp);
            return;
        }
#endif
        Object.Destroy(tmp);
    }

    static void CopyTmp(TMP_Text src, TextMeshProUGUI dst)
    {
        dst.text = src.text;
        dst.font = src.font;
        dst.fontSharedMaterial = src.fontSharedMaterial;
        dst.fontSize = src.fontSize;
        dst.fontStyle = src.fontStyle;
        dst.alignment = src.alignment;
        dst.color = src.color;
        dst.textWrappingMode = src.textWrappingMode;
        dst.overflowMode = src.overflowMode;
        dst.raycastTarget = false;
    }

    static Image FindOrCreateFill(RectTransform nameCell)
    {
        Transform existing = nameCell.Find(FillObjectName);
        if (existing != null)
        {
            Image image = existing.GetComponent<Image>();
            if (image != null)
            {
                ConfigureFill(image);
                existing.SetAsFirstSibling();
                return image;
            }
        }

        GameObject go = new(FillObjectName);
        go.transform.SetParent(nameCell, false);
        go.transform.SetAsFirstSibling();

        RectTransform rt = go.AddComponent<RectTransform>();
        Stretch(rt);

        Image fill = go.AddComponent<Image>();
        ConfigureFill(fill);
        go.SetActive(false);
        return fill;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);
    }

    static void ConfigureFill(Image fill)
    {
        fill.raycastTarget = false;
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        fill.fillAmount = 0f;
        if (fill.sprite == null)
            fill.sprite = BuiltinWhiteSprite();
    }

    static Sprite BuiltinWhiteSprite()
    {
        if (_whiteSprite != null)
            return _whiteSprite;

        Texture2D tex = Texture2D.whiteTexture;
        _whiteSprite = Sprite.Create(
            tex,
            new Rect(0f, 0f, tex.width, tex.height),
            new Vector2(0.5f, 0.5f),
            100f);
        return _whiteSprite;
    }
}
