// ============================================================
// UIItemListView — 컨테이너 스택 가상화 목록 (LeanPool) + 선택 + 뷰 전용 정렬
// ============================================================

using System;
using System.Collections.Generic;
using Lean.Pool;
using UnityEngine;
using UnityEngine.UI;

public sealed class UIItemListView : MonoBehaviour
{
    /// <summary>LeanPool 행 프리워밍 개수 SSOT. viewport+overscan 상한.</summary>
    public const int RowPoolPrewarmCount = 48;

    /// <summary>가시 윈도우 위·아래 여분 행 수 SSOT.</summary>
    public const int RowOverscan = 2;

    [SerializeField] RectTransform _contentRoot;
    [SerializeField] UIItemListRow _rowPrefab;
    [SerializeField] UIItemListColumnHeader _columnHeader;

    readonly InventoryListSelection _selection = new();
    readonly List<UIItemListRow> _activeRows = new();
    readonly List<ItemStack> _orderedStacks = new();
    readonly List<InventoryListDisplayGroup> _orderedGroups = new();
    readonly Dictionary<int, UIItemListRow> _rowByIndex = new();
    readonly List<int> _indexScratch = new();
    readonly List<ItemStack> _selectionScratch = new();
    readonly HashSet<ItemStack> _orderedSetScratch = new();
    readonly Vector3[] _cornerScratch = new Vector3[4];

    InventorySession _session;
    IInventoryItemDragHost _dragHost;
    UIItemDragGhostService _dragGhost;
    ScrollRect _scrollRect;
    InventoryContainer _boundContainer;
    LootAggregateHost _lootAggregateHost;
    int _appliedContentVersion = -1;
    bool _aggregateDisplayMode;
    bool _viewportConfigured;
    bool _selectionEventsWired;
    bool _scrollEventsWired;
    bool _manualLayoutReady;
    static bool _rowPoolPrewarmed;

    ItemListSortKey _sortKey = ItemListSortKey.None;
    bool _sortAscending = true;

    public InventorySession Session => _session;
    public UIItemListRow RowPrefab => _rowPrefab;
    public InventoryListSelection Selection => _selection;
    public int ActiveRowCount => _activeRows.Count;
    public int BoundStackCount => GetBoundRowCount();
    public ItemListSortKey SortKey => _sortKey;
    public bool SortAscending => _sortAscending;

    void Awake()
    {
        _scrollRect = GetComponent<ScrollRect>();
        EnsureScrollViewport();
        EnsureManualContentLayout();
        WireScrollEvents();
    }

    public void Configure(
        InventorySession session,
        IInventoryItemDragHost dragHost,
        UIItemDragGhostService dragGhost)
    {
        _session = session;
        _dragHost = dragHost;
        _dragGhost = dragGhost;

        if (_selectionEventsWired)
            return;

        _selection.SelectionChanged += OnSelectionChanged;
        _selectionEventsWired = true;
    }

    public void SetLootAggregateHost(LootAggregateHost host) => _lootAggregateHost = host;

    public InventoryContainer ResolveAggregateStackOwner(ItemStack stack)
    {
        if (stack == null)
            return _boundContainer;

        if (_lootAggregateHost != null && _lootAggregateHost.TryGetOwner(stack, out InventoryContainer hostOwner))
            return hostOwner;

        if (_session == null)
            return _boundContainer;

        IReadOnlyList<InventoryContainer> sidebar = _session.GetSidebarContainers();
        for (int i = 0; i < sidebar.Count; i++)
        {
            InventoryContainer candidate = sidebar[i];
            if (candidate == null || LootAggregateHost.IsAggregateContainer(candidate))
                continue;

            if (candidate.ContainsStackReference(stack))
                return candidate;
        }

        return _boundContainer;
    }

    /// <summary>
    /// Prefab_ItemListRow LeanPool을 count까지 채운다. Primary/Loot가 같은 프리팹이면 프로세스당 1회.
    /// </summary>
    public void PrewarmRowPool(int count = RowPoolPrewarmCount)
    {
        if (_rowPoolPrewarmed || _rowPrefab == null || count <= 0)
            return;

        GameObject prefabGo = _rowPrefab.gameObject;
        LeanGameObjectPool pool = null;
        if (!LeanGameObjectPool.TryFindPoolByPrefab(prefabGo, ref pool) || pool == null)
        {
            pool = new GameObject("LeanPool (" + prefabGo.name + ")").AddComponent<LeanGameObjectPool>();
            pool.Prefab = prefabGo;
        }

        if (pool.Preload < count)
            pool.Preload = count;

        pool.PreloadAll();
        _rowPoolPrewarmed = true;
    }

    void OnDestroy()
    {
        _selection.SelectionChanged -= OnSelectionChanged;
        UnwireScrollEvents();
    }

    void OnRectTransformDimensionsChange()
    {
        if (!_manualLayoutReady || _boundContainer == null)
            return;

        RefreshVisibleRows(forceRebind: false);
    }

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
        EnsureManualContentLayout();
        WireScrollEvents();

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

        BuildOrderedItems(container);
        PruneSelectionToBoundStacks();
        ApplyContentHeight();
        RefreshVisibleRows(forceRebind: true);
        _appliedContentVersion = container.ContentVersion;

        _columnHeader?.RefreshSortVisual(_sortKey, _sortAscending);

        if (resetScrollPosition)
            ResetScrollTop();
    }

    void BuildOrderedItems(InventoryContainer container)
    {
        _orderedStacks.Clear();
        _orderedGroups.Clear();
        _aggregateDisplayMode = LootAggregateHost.IsAggregateContainer(container);

        IReadOnlyList<ItemStack> stacks = container.Stacks;
        if (!_aggregateDisplayMode)
        {
            for (int i = 0; i < stacks.Count; i++)
                _orderedStacks.Add(stacks[i]);

            if (_sortKey != ItemListSortKey.None)
                _orderedStacks.Sort(new ItemListStackComparer(_sortKey, _sortAscending));

            return;
        }

        var stackScratch = new List<ItemStack>(stacks.Count);
        for (int i = 0; i < stacks.Count; i++)
            stackScratch.Add(stacks[i]);

        _orderedGroups.AddRange(InventoryListDisplayGrouper.Group(
            stackScratch,
            ItemMergeKeyDisplayEquivalence.Instance,
            ResolveAggregateStackOwner));

        if (_sortKey != ItemListSortKey.None)
            _orderedGroups.Sort(new InventoryListDisplayGroupComparer(_sortKey, _sortAscending));
    }

    void PruneSelectionToBoundStacks()
    {
        IReadOnlyList<ItemStack> selected = _selection.GetSelectedStacks();
        if (selected.Count == 0)
            return;

        _orderedSetScratch.Clear();
        if (_aggregateDisplayMode)
        {
            for (int i = 0; i < _orderedGroups.Count; i++)
            {
                InventoryListDisplayGroup group = _orderedGroups[i];
                if (group?.RepresentativeStack != null)
                    _orderedSetScratch.Add(group.RepresentativeStack);
            }
        }
        else
        {
            for (int i = 0; i < _orderedStacks.Count; i++)
            {
                if (_orderedStacks[i] != null)
                    _orderedSetScratch.Add(_orderedStacks[i]);
            }
        }

        _selectionScratch.Clear();
        bool removed = false;
        for (int i = 0; i < selected.Count; i++)
        {
            ItemStack stack = selected[i];
            if (stack != null && _orderedSetScratch.Contains(stack))
                _selectionScratch.Add(stack);
            else
                removed = true;
        }

        if (removed)
            _selection.SetMany(_selectionScratch);
    }

    void ApplyContentHeight()
    {
        if (_contentRoot == null)
            return;

        int n = GetBoundRowCount();
        float topPad = InventoryListColumnLayout.ContentPaddingTopWithStickyHeader;
        float bottomPad = InventoryListColumnLayout.ContentPadding;
        float height = topPad + bottomPad;
        if (n > 0)
        {
            height += n * InventoryListColumnLayout.RowHeight
                + (n - 1) * InventoryListColumnLayout.RowSpacing;
        }

        Vector2 size = _contentRoot.sizeDelta;
        size.y = height;
        _contentRoot.sizeDelta = size;
    }

    void RefreshVisibleRows(bool forceRebind)
    {
        if (_contentRoot == null || _rowPrefab == null)
            return;

        GetVisibleRange(out int first, out int last);

        _indexScratch.Clear();
        foreach (KeyValuePair<int, UIItemListRow> entry in _rowByIndex)
        {
            if (entry.Key < first || entry.Key > last)
                _indexScratch.Add(entry.Key);
        }

        for (int i = 0; i < _indexScratch.Count; i++)
            RecycleRowAt(_indexScratch[i]);

        if (GetBoundRowCount() == 0 || first > last)
            return;

        for (int index = first; index <= last; index++)
        {
            if (_aggregateDisplayMode)
                RefreshVisibleAggregateRow(index, forceRebind);
            else
                RefreshVisibleStackRow(index, forceRebind);
        }
    }

    void RefreshVisibleStackRow(int index, bool forceRebind)
    {
        ItemStack stack = _orderedStacks[index];
        if (stack == null)
        {
            RecycleRowAt(index);
            return;
        }

        if (_rowByIndex.TryGetValue(index, out UIItemListRow row) && row != null)
        {
            if (forceRebind || row.Stack != stack)
                BindRow(row, stack);
            LayoutRow(row, index);
            return;
        }

        row = SpawnRow();
        if (row == null)
            return;

        BindRow(row, stack);
        LayoutRow(row, index);
        _rowByIndex[index] = row;
        _activeRows.Add(row);
    }

    void RefreshVisibleAggregateRow(int index, bool forceRebind)
    {
        InventoryListDisplayGroup group = _orderedGroups[index];
        ItemStack representative = group?.RepresentativeStack;
        if (representative == null)
        {
            RecycleRowAt(index);
            return;
        }

        if (_rowByIndex.TryGetValue(index, out UIItemListRow row) && row != null)
        {
            if (forceRebind || row.Stack != representative || row.DisplayGroup != group)
                BindRowGroup(row, group);
            LayoutRow(row, index);
            return;
        }

        row = SpawnRow();
        if (row == null)
            return;

        BindRowGroup(row, group);
        LayoutRow(row, index);
        _rowByIndex[index] = row;
        _activeRows.Add(row);
    }

    void GetVisibleRange(out int first, out int last)
    {
        int n = GetBoundRowCount();
        if (n <= 0)
        {
            first = 0;
            last = -1;
            return;
        }

        float stride = GetRowStride();
        float topPad = InventoryListColumnLayout.ContentPaddingTopWithStickyHeader;
        RectTransform viewport = _contentRoot.parent as RectTransform;
        float viewportHeight = viewport != null ? viewport.rect.height : 0f;
        float scrollY = _contentRoot.anchoredPosition.y;

        if (viewportHeight <= 0f || stride <= 0f)
        {
            first = 0;
            last = Mathf.Min(n - 1, RowOverscan * 2);
            return;
        }

        first = Mathf.FloorToInt((scrollY - topPad) / stride) - RowOverscan;
        last = Mathf.FloorToInt((scrollY + viewportHeight - topPad) / stride) + RowOverscan;
        first = Mathf.Clamp(first, 0, n - 1);
        last = Mathf.Clamp(last, 0, n - 1);
        if (last < first)
            last = first;
    }

    static float GetRowStride() =>
        InventoryListColumnLayout.RowHeight + InventoryListColumnLayout.RowSpacing;

    UIItemListRow SpawnRow()
    {
        UIItemListRow row = LeanPool.Spawn(_rowPrefab, _contentRoot);
        if (row == null)
        {
            Debug.LogError("[UIItemListView] LeanPool.Spawn returned null row.", this);
            return null;
        }

        return row;
    }

    void BindRow(UIItemListRow row, ItemStack stack)
    {
        row.gameObject.SetActive(false);
        row.Bind(stack, _boundContainer, _selection, _dragHost, _dragGhost, this);
        if (stack?.Item != null)
            row.gameObject.SetActive(true);
    }

    void BindRowGroup(UIItemListRow row, InventoryListDisplayGroup group)
    {
        row.gameObject.SetActive(false);
        row.BindDisplayGroup(group, _boundContainer, _selection, _dragHost, _dragGhost, this);
        if (group?.RepresentativeStack?.Item != null)
            row.gameObject.SetActive(true);
    }

    int GetBoundRowCount() =>
        _aggregateDisplayMode ? _orderedGroups.Count : _orderedStacks.Count;

    void LayoutRow(UIItemListRow row, int index)
    {
        RectTransform rt = row.RectTransform;
        if (rt == null)
            return;

        if (rt.parent != _contentRoot)
            rt.SetParent(_contentRoot, false);

        float padH = InventoryListColumnLayout.ContentPadding;
        rt.localScale = Vector3.one;
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(-2f * padH, InventoryListColumnLayout.RowHeight);

        float y = -(InventoryListColumnLayout.ContentPaddingTopWithStickyHeader
            + index * GetRowStride());
        rt.anchoredPosition = new Vector2(0f, y);
    }

    void RecycleRowAt(int index)
    {
        if (!_rowByIndex.TryGetValue(index, out UIItemListRow row))
            return;

        _rowByIndex.Remove(index);
        _activeRows.Remove(row);

        if (row != null)
            LeanPool.Despawn(row.gameObject);
    }

    void EnsureManualContentLayout()
    {
        if (_manualLayoutReady || _contentRoot == null)
            return;

        if (_contentRoot.TryGetComponent(out VerticalLayoutGroup layout))
            layout.enabled = false;

        if (_contentRoot.TryGetComponent(out ContentSizeFitter fitter))
            fitter.enabled = false;

        _manualLayoutReady = true;
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

    void WireScrollEvents()
    {
        if (_scrollEventsWired)
            return;

        if (_scrollRect == null)
            _scrollRect = GetComponent<ScrollRect>();

        if (_scrollRect == null)
            return;

        _scrollRect.onValueChanged.AddListener(OnScrollValueChanged);
        _scrollEventsWired = true;
    }

    void UnwireScrollEvents()
    {
        if (!_scrollEventsWired || _scrollRect == null)
            return;

        _scrollRect.onValueChanged.RemoveListener(OnScrollValueChanged);
        _scrollEventsWired = false;
    }

    void OnScrollValueChanged(Vector2 _)
    {
        if (_boundContainer == null)
            return;

        RefreshVisibleRows(forceRebind: false);
    }

    void ResetScrollTop()
    {
        if (_scrollRect == null)
            return;

        _scrollRect.verticalNormalizedPosition = 1f;
        _scrollRect.horizontalNormalizedPosition = 0f;
        RefreshVisibleRows(forceRebind: false);
    }

    public void SelectRowsInRect(Rect screenRect, Camera uiCamera = null)
    {
        _selectionScratch.Clear();
        if (_contentRoot == null || GetBoundRowCount() == 0)
        {
            _selection.SetMany(_selectionScratch);
            return;
        }

        if (!TryGetContentYRangeFromScreenRect(screenRect, uiCamera, out float yMin, out float yMax))
        {
            _selection.SetMany(_selectionScratch);
            return;
        }

        float topPad = InventoryListColumnLayout.ContentPaddingTopWithStickyHeader;
        float rowHeight = InventoryListColumnLayout.RowHeight;
        float stride = GetRowStride();
        int n = GetBoundRowCount();

        int first = Mathf.FloorToInt((yMin - topPad) / stride);
        int last = Mathf.FloorToInt((yMax - topPad) / stride);
        first = Mathf.Clamp(first, 0, n - 1);
        last = Mathf.Clamp(last, 0, n - 1);
        if (last < first)
        {
            int swap = first;
            first = last;
            last = swap;
        }

        for (int i = first; i <= last; i++)
        {
            ItemStack stack = TryGetSelectableStackAt(i);
            if (stack == null)
                continue;

            float rowTop = topPad + i * stride;
            float rowBottom = rowTop + rowHeight;
            if (rowBottom < yMin || rowTop > yMax)
                continue;

            if (!RowIndexIntersectsScreenRect(i, screenRect, uiCamera))
                continue;

            _selectionScratch.Add(stack);
        }

        _selection.SetMany(_selectionScratch);
    }

    bool TryGetContentYRangeFromScreenRect(
        Rect screenRect,
        Camera uiCamera,
        out float yMin,
        out float yMax)
    {
        yMin = float.PositiveInfinity;
        yMax = float.NegativeInfinity;

        Vector2[] points =
        {
            new(screenRect.xMin, screenRect.yMin),
            new(screenRect.xMin, screenRect.yMax),
            new(screenRect.xMax, screenRect.yMin),
            new(screenRect.xMax, screenRect.yMax),
        };

        for (int i = 0; i < points.Length; i++)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _contentRoot,
                    points[i],
                    uiCamera,
                    out Vector2 local))
                return false;

            float topDist = -local.y;
            yMin = Mathf.Min(yMin, topDist);
            yMax = Mathf.Max(yMax, topDist);
        }

        return yMax >= yMin;
    }

    bool RowIndexIntersectsScreenRect(int index, Rect screenRect, Camera uiCamera)
    {
        float topPad = InventoryListColumnLayout.ContentPaddingTopWithStickyHeader;
        float rowHeight = InventoryListColumnLayout.RowHeight;
        float stride = GetRowStride();
        float rowTop = topPad + index * stride;

        Rect contentRect = _contentRoot.rect;
        float padH = InventoryListColumnLayout.ContentPadding;
        float xMin = contentRect.xMin + padH;
        float xMax = contentRect.xMax - padH;
        _cornerScratch[0] = _contentRoot.TransformPoint(new Vector3(xMin, -rowTop, 0f));
        _cornerScratch[1] = _contentRoot.TransformPoint(new Vector3(xMax, -rowTop, 0f));
        _cornerScratch[2] = _contentRoot.TransformPoint(new Vector3(xMin, -(rowTop + rowHeight), 0f));
        _cornerScratch[3] = _contentRoot.TransformPoint(new Vector3(xMax, -(rowTop + rowHeight), 0f));

        float minX = float.PositiveInfinity;
        float minY = float.PositiveInfinity;
        float maxX = float.NegativeInfinity;
        float maxY = float.NegativeInfinity;

        for (int i = 0; i < _cornerScratch.Length; i++)
        {
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(uiCamera, _cornerScratch[i]);
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
        _rowByIndex.Clear();
        _orderedStacks.Clear();
        _orderedGroups.Clear();
        _aggregateDisplayMode = false;
        _boundContainer = null;
        _appliedContentVersion = -1;

        if (_contentRoot != null)
        {
            Vector2 size = _contentRoot.sizeDelta;
            size.y = InventoryListColumnLayout.ContentPaddingTopWithStickyHeader
                + InventoryListColumnLayout.ContentPadding;
            _contentRoot.sizeDelta = size;
        }
    }

    ItemStack TryGetSelectableStackAt(int index)
    {
        if (_aggregateDisplayMode)
            return index >= 0 && index < _orderedGroups.Count
                ? _orderedGroups[index]?.RepresentativeStack
                : null;

        return index >= 0 && index < _orderedStacks.Count ? _orderedStacks[index] : null;
    }
}
