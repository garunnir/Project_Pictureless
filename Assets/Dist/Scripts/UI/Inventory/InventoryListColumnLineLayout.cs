// ============================================================
// InventoryListColumnLineLayout — 행/헤더 HLG 열 폭·폰트를 Settings로 프리팹에 반영
// ============================================================

using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(HorizontalLayoutGroup))]
public sealed class InventoryListColumnLineLayout : MonoBehaviour
{
    [SerializeField] InventoryListColumnLayoutSettings _settings;

    public InventoryListColumnLayoutSettings Settings => _settings;

    public void SetSettings(InventoryListColumnLayoutSettings settings) => _settings = settings;

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!gameObject.activeInHierarchy || _settings == null)
            return;

        Apply(asHeader: transform.name == "Area_ColumnHeader");
    }
#endif

    public void Apply(bool asHeader)
    {
        if (_settings == null)
            return;

        if (!TryGetComponent(out HorizontalLayoutGroup layout))
            return;

        int padH = asHeader ? _settings.ListInsetHorizontal : _settings.RowPaddingH;
        layout.padding = new RectOffset(padH, padH, _settings.RowPaddingV, _settings.RowPaddingV);
        layout.spacing = _settings.Spacing;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        SetFixedColumn("IconWarpper", _settings.IconSize);
        SetFixedColumn("IconSpacer", _settings.IconSize);
        SetFixedColumn("Category", _settings.CategoryWidth);
        SetFixedColumn("Count", _settings.CountWidth);
        SetFixedColumn("WeightValue", _settings.WeightValueWidth);
        SetFixedColumn("WeightUnit", _settings.WeightUnitWidth);
        SetFixedColumn("VolumeValue", _settings.VolumeValueWidth);
        SetFixedColumn("VolumeUnit", _settings.VolumeUnitWidth);
        SetFlexColumn("Name", _settings.NameMinWidth);
        ApplyNameLabelStretch();

        ApplyFonts(asHeader);
    }

    public void ApplyDataRowGeometry()
    {
        Apply(asHeader: false);

        var rowRect = transform as RectTransform;
        if (rowRect == null || _settings == null)
            return;

        rowRect.anchorMin = new Vector2(0f, 1f);
        rowRect.anchorMax = new Vector2(1f, 1f);
        rowRect.pivot = new Vector2(0.5f, 1f);
        rowRect.sizeDelta = Vector2.zero;

        if (!TryGetComponent(out LayoutElement rowLayout))
            rowLayout = gameObject.AddComponent<LayoutElement>();

        rowLayout.minHeight = _settings.RowHeight;
        rowLayout.preferredHeight = _settings.RowHeight;
        rowLayout.flexibleHeight = 0f;
    }

    void ApplyFonts(bool asHeader)
    {
        if (asHeader)
        {
            float header = _settings.FontHeader;
            SetFont("Category", header);
            SetFont("Name", header);
            SetFont("Count", header);
            SetFont("WeightValue", header);
            SetFont("WeightUnit", header);
            SetFont("VolumeValue", header);
            SetFont("VolumeUnit", header);
            return;
        }

        SetFont("Category", _settings.FontCategory);
        SetFont("Name", _settings.FontName);
        SetFont("Count", _settings.FontDetail);
        SetFont("WeightValue", _settings.FontDetail);
        SetFont("WeightUnit", _settings.FontDetail);
        SetFont("VolumeValue", _settings.FontDetail);
        SetFont("VolumeUnit", _settings.FontDetail);
    }

    void ApplyNameLabelStretch()
    {
        Transform name = transform.Find("Name");
        if (name == null)
            return;

        StretchExisting(name.Find(ItemNameStatusBar.LabelObjectName));
    }

    static void StretchExisting(Transform child)
    {
        if (child is not RectTransform rt)
            return;

        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);
    }

    void SetFont(string childName, float fontSize)
    {
        Transform child = transform.Find(childName);
        if (child == null)
            return;

        if (!child.TryGetComponent(out TextMeshProUGUI text))
        {
            Transform label = child.Find(ItemNameStatusBar.LabelObjectName);
            if (label == null || !label.TryGetComponent(out text))
                return;
        }

        text.enableAutoSizing = false;
        text.fontSize = fontSize;
#if UNITY_EDITOR
        var so = new UnityEditor.SerializedObject(text);
        so.FindProperty("m_fontSize").floatValue = fontSize;
        so.FindProperty("m_fontSizeBase").floatValue = fontSize;
        so.FindProperty("m_enableAutoSizing").boolValue = false;
        so.ApplyModifiedPropertiesWithoutUndo();
        UnityEditor.EditorUtility.SetDirty(text);
#endif
    }

    void SetFixedColumn(string childName, float width)
    {
        Transform child = transform.Find(childName);
        if (child == null)
            return;

        if (!child.TryGetComponent(out LayoutElement layout))
            layout = child.gameObject.AddComponent<LayoutElement>();

        layout.flexibleWidth = 0f;
        layout.preferredWidth = width;
        layout.minWidth = width;
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(layout);
#endif
    }

    void SetFlexColumn(string childName, float minWidth)
    {
        Transform child = transform.Find(childName);
        if (child == null)
            return;

        if (!child.TryGetComponent(out LayoutElement layout))
            layout = child.gameObject.AddComponent<LayoutElement>();

        layout.flexibleWidth = 1f;
        layout.preferredWidth = -1f;
        layout.minWidth = minWidth;
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(layout);
#endif
    }
}
