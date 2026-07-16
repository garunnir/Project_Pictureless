// ============================================================
// UIContextMenuItemRow — 캐스케이드 메뉴 한 행
// ============================================================

using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class UIContextMenuItemRow : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [SerializeField] Image _background;
    [SerializeField] TMP_Text _label;
    [SerializeField] TMP_Text _chevron;

    ContextMenuEntry _entry;
    int _depth;
    bool _interactable = true;
    Action<UIContextMenuItemRow> _onEnter;
    Action<UIContextMenuItemRow> _onExit;
    Action<UIContextMenuItemRow> _onClick;

    public ContextMenuEntry Entry => _entry;
    public int Depth => _depth;
    public RectTransform Rect => (RectTransform)transform;

    public void Bind(
        ContextMenuEntry entry,
        int depth,
        Action<UIContextMenuItemRow> onEnter,
        Action<UIContextMenuItemRow> onExit,
        Action<UIContextMenuItemRow> onClick)
    {
        _entry = entry;
        _depth = depth;
        _onEnter = onEnter;
        _onExit = onExit;
        _onClick = onClick;

        bool hasChildren = entry != null && entry.HasChildren;
        string disabledReason = null;
        if (entry != null && !hasChildren && entry.Action != null)
            disabledReason = entry.Action.GetDisabledReason();

        _interactable = hasChildren || string.IsNullOrEmpty(disabledReason);

        if (_label != null)
        {
            _label.textWrappingMode = TextWrappingModes.NoWrap;
            _label.overflowMode = TextOverflowModes.Ellipsis;

            string text = entry?.Label ?? "";
            if (!_interactable && !string.IsNullOrEmpty(disabledReason))
                text = $"{text} — {disabledReason}";
            _label.text = text;
            _label.color = _interactable ? Color.white : new Color(0.65f, 0.65f, 0.65f, 1f);
        }

        if (_chevron != null)
        {
            _chevron.gameObject.SetActive(hasChildren);
            if (hasChildren)
                _chevron.text = ItemContextMenuLabels.SubmenuChevron;
        }

        ApplyBackground(hovered: false);
    }

    /// <summary>랩/ellipsis 전 선호 폭 (패딩·쉐브론 포함).</summary>
    public float MeasurePreferredWidth()
    {
        float labelW = 0f;
        if (_label != null && !string.IsNullOrEmpty(_label.text))
            labelW = _label.GetPreferredValues(_label.text).x;

        float chevronW = 0f;
        if (_chevron != null && _chevron.gameObject.activeSelf)
            chevronW = ContextMenuStyle.ChevronWidth + ContextMenuStyle.RowLabelChevronGap;

        return ContextMenuStyle.RowPaddingLeft
            + labelW
            + chevronW
            + ContextMenuStyle.RowPaddingRight;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_entry == null)
            return;

        ApplyBackground(hovered: true);
        _onEnter?.Invoke(this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ApplyBackground(hovered: false);
        _onExit?.Invoke(this);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (!_interactable && !(_entry?.HasChildren ?? false))
            return;

        _onClick?.Invoke(this);
    }

    void ApplyBackground(bool hovered)
    {
        if (_background == null)
            return;

        if (!_interactable && !(_entry?.HasChildren ?? false))
        {
            _background.color = ContextMenuStyle.RowDisabledColor;
            return;
        }

        _background.color = hovered ? ContextMenuStyle.RowHoverColor : ContextMenuStyle.RowColor;
    }
}
