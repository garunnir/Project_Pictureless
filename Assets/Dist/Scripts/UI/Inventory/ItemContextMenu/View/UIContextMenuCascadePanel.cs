// ============================================================
// UIContextMenuCascadePanel — Entry 리스트를 행으로 그리는 패널
// ============================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class UIContextMenuCascadePanel : MonoBehaviour, IPointerEnterHandler
{
    [SerializeField] RectTransform _root;
    [SerializeField] Transform _rowContainer;
    [SerializeField] UIContextMenuItemRow _rowPrefab;

    readonly List<UIContextMenuItemRow> _rows = new();

    Action<UIContextMenuCascadePanel> _onPanelEnter;

    public RectTransform Root => _root != null ? _root : (RectTransform)transform;
    public int Depth { get; private set; }

    public void Bind(
        IReadOnlyList<ContextMenuEntry> entries,
        int depth,
        Action<UIContextMenuItemRow> onEnter,
        Action<UIContextMenuItemRow> onExit,
        Action<UIContextMenuItemRow> onClick,
        Action<UIContextMenuCascadePanel> onPanelEnter = null)
    {
        Depth = depth;
        _onPanelEnter = onPanelEnter;
        ClearRows();

        if (entries == null || _rowPrefab == null || _rowContainer == null)
            return;

        for (int i = 0; i < entries.Count; i++)
        {
            ContextMenuEntry entry = entries[i];
            if (entry == null)
                continue;

            UIContextMenuItemRow row = Instantiate(_rowPrefab, _rowContainer);
            row.gameObject.SetActive(true);
            row.Bind(entry, depth, onEnter, onExit, onClick);
            _rows.Add(row);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(Root);
        ApplyMaxHeight();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _onPanelEnter?.Invoke(this);
    }

    void ApplyMaxHeight()
    {
        if (_rowContainer == null)
            return;

        RectTransform content = _rowContainer as RectTransform;
        if (content == null)
            return;

        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        float contentHeight = LayoutUtility.GetPreferredHeight(content);
        float padded = contentHeight + ContextMenuStyle.PanelPadding * 2f;
        float height = Mathf.Min(padded, ContextMenuStyle.PanelMaxHeight);

        if (TryGetComponent(out LayoutElement rootLe))
            rootLe.preferredHeight = height;

        ScrollRect scroll = GetComponentInChildren<ScrollRect>(true);
        if (scroll != null && scroll.TryGetComponent(out LayoutElement scrollLe))
        {
            float inner = Mathf.Min(contentHeight, ContextMenuStyle.PanelMaxHeight - ContextMenuStyle.PanelPadding * 2f);
            scrollLe.preferredHeight = Mathf.Max(ContextMenuStyle.RowHeight, inner);
        }
    }

    public void ClearRows()
    {
        for (int i = _rows.Count - 1; i >= 0; i--)
        {
            if (_rows[i] != null)
                Destroy(_rows[i].gameObject);
        }

        _rows.Clear();
    }

    void OnDestroy()
    {
        ClearRows();
    }
}
