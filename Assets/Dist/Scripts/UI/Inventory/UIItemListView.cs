// ============================================================

// UIItemListView — 컨테이너 스택 목록 (LeanPool) + 선택

// ============================================================



using System.Collections.Generic;

using Lean.Pool;

using TMPro;

using UnityEngine;

using UnityEngine.UI;



public sealed class UIItemListView : MonoBehaviour

{

    [SerializeField] RectTransform _contentRoot;

    [SerializeField] UIItemListRow _rowPrefab;



    readonly InventoryListSelection _selection = new();

    readonly List<UIItemListRow> _activeRows = new();



    InventorySession _session;

    IInventoryItemDragHost _dragHost;

    ScrollRect _scrollRect;

    bool _viewportConfigured;



    public InventoryListSelection Selection => _selection;

    public int ActiveRowCount => _activeRows.Count;

    bool _selectionEventsWired;



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



    public void Bind(InventoryContainer container)

    {

        EnsureScrollViewport();

        ClearRows();



        if (container == null || _rowPrefab == null || _contentRoot == null)

        {

            Debug.LogWarning("[UIItemListView] Bind skipped: missing container or references.", this);

            return;

        }



        int stackCount = container.Stacks.Count;

        for (int i = 0; i < stackCount; i++)

        {

            UIItemListRow row = LeanPool.Spawn(_rowPrefab, _contentRoot);

            if (row == null)

            {

                Debug.LogError("[UIItemListView] LeanPool.Spawn returned null row.", this);

                continue;

            }



            ApplyRowLayout(row.RectTransform);

            row.Bind(container.Stacks[i], container, _selection, _dragHost, this);

            _activeRows.Add(row);

        }



        RebuildLayout();

        ResetScrollTop();



        Debug.Log(

            $"[UIItemListView] Bind container='{container.InstanceId}' stacks={stackCount} rows={_activeRows.Count} contentHeight={_contentRoot.rect.height:0.##}",

            this);

    }



    void EnsureScrollViewport()

    {

        if (_viewportConfigured || _contentRoot == null)

            return;



        RectTransform viewport = _contentRoot.parent as RectTransform;

        if (viewport == null)

            return;



        Mask stencilMask = viewport.GetComponent<Mask>();

        if (stencilMask != null)

            stencilMask.enabled = false;



        if (viewport.GetComponent<RectMask2D>() == null)
            Debug.LogError("[UIItemListView] RectMask2D missing on scroll viewport prefab.", viewport);



        Image viewportImage = viewport.GetComponent<Image>();

        if (viewportImage != null)

        {

            viewportImage.color = new Color(1f, 1f, 1f, 0f);

            viewportImage.raycastTarget = true;

        }



        if (_scrollRect == null)

            _scrollRect = GetComponent<ScrollRect>();



        if (_scrollRect != null)

        {

            _scrollRect.viewport = viewport;

            _scrollRect.content = _contentRoot;

            _scrollRect.horizontal = false;

            _scrollRect.vertical = true;

            _scrollRect.movementType = ScrollRect.MovementType.Clamped;

        }



        _viewportConfigured = true;

    }



    static void ApplyRowLayout(RectTransform rowRect)

    {

        if (rowRect == null)

            return;



        rowRect.localScale = Vector3.one;



        float height = 0f;

        if (rowRect.TryGetComponent(out LayoutElement layoutElement) && layoutElement.preferredHeight > 0f)

            height = layoutElement.preferredHeight;

        else if (rowRect.sizeDelta.y > 1f)

            height = rowRect.sizeDelta.y;



        if (height <= 1f)

        {

            Debug.LogWarning("[UIItemListView] Row prefab missing LayoutElement.preferredHeight / sizeDelta; layout may be wrong.", rowRect);

            return;

        }



        Vector2 size = rowRect.sizeDelta;

        size.y = height;

        rowRect.sizeDelta = size;

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

    }

}


