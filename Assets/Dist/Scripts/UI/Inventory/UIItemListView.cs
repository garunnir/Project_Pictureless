// ============================================================
// UIItemListView — 컨테이너 스택 목록 (LeanPool) + 선택 + 뷰 전용 정렬
// ============================================================

using System;
using System.Collections.Generic;
using Lean.Pool;
using UnityEngine;
using UnityEngine.UI;

public sealed class UIItemListView : MonoBehaviour
{
    [SerializeField] RectTransform _contentRoot;
    [SerializeField] UIItemListRow _rowPrefab;
    [SerializeField] UIItemListColumnHeader _columnHeader;

    readonly InventoryListSelection _selection = new();
    readonly List<UIItemListRow> _activeRows = new();
    readonly List<ItemStack> _orderedStacks = new();
    readonly Dictionary<ItemStack, UIItemListRow> _rowsByStack = new();

    InventorySession _session;
    IInventoryItemDragHost _dragHost;
    ScrollRect _scrollRect;
    InventoryContainer _boundContainer;
    int _appliedContentVersion = -1;
    bool _viewportConfigured;
    bool _selectionEventsWired;

    ItemListSortKey _sortKey = ItemListSortKey.None;
    bool _sortAscending = true;

    public InventoryListSelection Selection => _selection;
    public int ActiveRowCount => _activeRows.Count;
    public ItemListSortKey SortKey => _sortKey;
    public bool SortAscending => _sortAscending;

    void Awake()
    {
        _scrollRect = GetComponent<ScrollRect>();
        EnsureScrollViewport();
    }

    public void Configure(InventorySession session, IInventoryItemDragHost dragHost)
    {
        _session = session;
        _dragHost = dragHost;

        if (_selectionEventsWired)
            return;

        _selection.SelectionChanged += OnSelectionChanged;
        _selectionEventsWired = true;
    }

    void OnDestroy() => _selection.SelectionChanged -= OnSelectionChanged;

    public void SetSort(ItemListSortKey key)
    {
        if (key == ItemListSortKey.None)
        {
            _sortKey = ItemListSortKey.None;
            _sortAscending = true;
        }
        else if (_sortKey == key)
        {
            _sortAscending = !_sortAscending;
        }
        else
        {
            _sortKey = key;
            _sortAscending = true;
        }

        _columnHeader?.RefreshSortVisual(_sortKey, _sortAscending);

        if (_boundContainer != null)
            Bind(_boundContainer, force: true, resetScroll: true);
    }

    public void Bind(InventoryContainer container) =>
        Bind(container, force: false, resetScroll: false);

    void Bind(InventoryContainer container, bool force, bool resetScroll)
    {
        EnsureScrollViewport();

        if (container == null || _rowPrefab == null || _contentRoot == null)
        {
            ClearRows();
            _boundContainer = null;
            _appliedContentVersion = -1;
            _columnHeader?.RefreshSortVisual(_sortKey, _sortAscending);
            return;
        }

        bool containerChanged = _boundContainer != container;
        if (!force &&
            !containerChanged &&
            container.ContentVersion == _appliedContentVersion)
            return;

        _boundContainer = container;
        bool resetScrollPosition = resetScroll || containerChanged;

        BuildOrderedStacks(container);

        bool structureChanged = SyncRows(container, resetScrollPosition);
        _appliedContentVersion = container.ContentVersion;

        _columnHeader?.RefreshSortVisual(_sortKey, _sortAscending);

        if (structureChanged)
            RebuildLayout();

        if (resetScrollPosition)
            ResetScrollTop();
    }

    void BuildOrderedStacks(InventoryContainer container)
    {
        _orderedStacks.Clear();
        IReadOnlyList<ItemStack> stacks = container.Stacks;
        for (int i = 0; i < stacks.Count; i++)
            _orderedStacks.Add(stacks[i]);

        if (_sortKey != ItemListSortKey.None)
            _orderedStacks.Sort(new ItemListStackComparer(_sortKey, _sortAscending));
    }

    bool SyncRows(InventoryContainer container, bool pruneSelection)
    {
        var desired = new HashSet<ItemStack>();
        for (int i = 0; i < _orderedStacks.Count; i++)
        {
            if (_orderedStacks[i] != null)
                desired.Add(_orderedStacks[i]);
        }

        bool structureChanged = false;

        var removeStacks = new List<ItemStack>();
        foreach (KeyValuePair<ItemStack, UIItemListRow> entry in _rowsByStack)
        {
            if (!desired.Contains(entry.Key))
                removeStacks.Add(entry.Key);
        }

        for (int i = 0; i < removeStacks.Count; i++)
        {
            RemoveRow(removeStacks[i], pruneSelection);
            structureChanged = true;
        }

        for (int i = 0; i < _orderedStacks.Count; i++)
        {
            ItemStack stack = _orderedStacks[i];
            if (stack == null)
                continue;

            if (!_rowsByStack.TryGetValue(stack, out UIItemListRow row) || row == null)
            {
                row = SpawnRow(container, stack);
                if (row == null)
                    continue;

                _rowsByStack[stack] = row;
                structureChanged = true;
            }
            else
            {
                row.Bind(stack, container, _selection, _dragHost, this);
            }
        }

        structureChanged |= ReorderActiveRows();
        return structureChanged;
    }

    UIItemListRow SpawnRow(InventoryContainer container, ItemStack stack)
    {
        UIItemListRow row = LeanPool.Spawn(_rowPrefab, _contentRoot);
        if (row == null)
        {
            Debug.LogError("[UIItemListView] LeanPool.Spawn returned null row.", this);
            return null;
        }

        if (row.RectTransform != null)
            row.RectTransform.localScale = Vector3.one;

        row.Bind(stack, container, _selection, _dragHost, this);
        return row;
    }

    void RemoveRow(ItemStack stack, bool pruneSelection)
    {
        if (!_rowsByStack.TryGetValue(stack, out UIItemListRow row))
            return;

        _rowsByStack.Remove(stack);
        _activeRows.Remove(row);

        if (pruneSelection && _selection.IsSelected(stack))
            _selection.Remove(stack);

        if (row != null)
            LeanPool.Despawn(row.gameObject);
    }

    bool ReorderActiveRows()
    {
        bool orderChanged = false;

        _activeRows.Clear();
        for (int i = 0; i < _orderedStacks.Count; i++)
        {
            ItemStack stack = _orderedStacks[i];
            if (stack == null || !_rowsByStack.TryGetValue(stack, out UIItemListRow row) || row == null)
                continue;

            if (row.transform.GetSiblingIndex() != i)
                orderChanged = true;

            row.transform.SetSiblingIndex(i);
            _activeRows.Add(row);
        }

        return orderChanged;
    }

    void EnsureScrollViewport()
    {
        if (_viewportConfigured || _contentRoot == null)
            return;

        RectTransform viewport = _contentRoot.parent as RectTransform;
        if (viewport == null)
            return;

        if (viewport.GetComponent<RectMask2D>() == null)
            Debug.LogError("[UIItemListView] RectMask2D missing on scroll viewport prefab.", viewport);

        if (_scrollRect == null)
            _scrollRect = GetComponent<ScrollRect>();

        _viewportConfigured = true;
    }

    void RebuildLayout()
    {
        if (_contentRoot == null)
            return;

        LayoutRebuilder.ForceRebuildLayoutImmediate(_contentRoot);

        RectTransform viewport = _contentRoot.parent as RectTransform;
        if (viewport != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(viewport);

        RectTransform listArea = viewport != null ? viewport.parent as RectTransform : null;
        if (listArea != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(listArea);

        Canvas.ForceUpdateCanvases();
    }

    void ResetScrollTop()
    {
        if (_scrollRect == null)
            return;

        _scrollRect.verticalNormalizedPosition = 1f;
        _scrollRect.horizontalNormalizedPosition = 0f;
    }

    public void SelectRowsInRect(Rect screenRect, Camera uiCamera = null)
    {
        var selected = new List<ItemStack>();
        for (int i = 0; i < _activeRows.Count; i++)
        {
            UIItemListRow row = _activeRows[i];
            if (row == null || row.Stack == null || row.RectTransform == null)
                continue;

            if (RowIntersectsScreenRect(row.RectTransform, screenRect, uiCamera))
                selected.Add(row.Stack);
        }

        _selection.SetMany(selected);
    }

    static bool RowIntersectsScreenRect(RectTransform rowRect, Rect screenRect, Camera uiCamera)
    {
        Vector3[] corners = new Vector3[4];
        rowRect.GetWorldCorners(corners);
        float minX = float.PositiveInfinity;
        float minY = float.PositiveInfinity;
        float maxX = float.NegativeInfinity;
        float maxY = float.NegativeInfinity;

        for (int i = 0; i < corners.Length; i++)
        {
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(uiCamera, corners[i]);
            minX = Mathf.Min(minX, screen.x);
            minY = Mathf.Min(minY, screen.y);
            maxX = Mathf.Max(maxX, screen.x);
            maxY = Mathf.Max(maxY, screen.y);
        }

        var rowScreen = Rect.MinMaxRect(minX, minY, maxX, maxY);
        return rowScreen.Overlaps(screenRect, true);
    }

    void OnSelectionChanged()
    {
        for (int i = 0; i < _activeRows.Count; i++)
        {
            if (_activeRows[i] != null)
                _activeRows[i].RefreshSelectionVisual();
        }
    }

    public void ClearRows()
    {
        _selection.Clear();

        for (int i = _activeRows.Count - 1; i >= 0; i--)
        {
            if (_activeRows[i] != null)
                LeanPool.Despawn(_activeRows[i].gameObject);
        }

        _activeRows.Clear();
        _rowsByStack.Clear();
        _orderedStacks.Clear();
        _boundContainer = null;
        _appliedContentVersion = -1;
    }
}
