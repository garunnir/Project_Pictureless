// ============================================================
// UIItemContextMenu — 아이템 우클릭 캐스케이드 메뉴 호스트
// ============================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class UIItemContextMenu : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] RectTransform _panelRoot;
    [SerializeField] UIContextMenuCascadePanel _panelPrefab;
    [SerializeField] UIContextMenuItemRow _rowPrefab;

    readonly List<UIContextMenuCascadePanel> _openPanels = new();

    InventorySession _session;
    Canvas _rootCanvas;
    Image _rootRaycastImage;
    bool _isOpen;
    Coroutine _closeDelayRoutine;
    int _hoverDepth = -1;

    void Awake()
    {
        TryGetComponent(out _rootRaycastImage);
        SetRootRaycastEnabled(false);
    }

    void OnEnable()
    {
        UIItemListRow.RightClicked += OnItemRightClicked;
    }

    void OnDisable()
    {
        UIItemListRow.RightClicked -= OnItemRightClicked;
        Hide();
    }

    void Update()
    {
        if (!_isOpen)
            return;

        InputManager input = InputManager.Instance;
        if (input == null)
            return;

        if (input.TryReadCancelPerformedThisFrame(out bool canceled) && canceled)
            Hide();
    }

    public void Initialize(InventorySession session, Canvas rootCanvas)
    {
        _session = session;
        _rootCanvas = rootCanvas;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!_isOpen)
            return;

        Hide();
    }

    void OnItemRightClicked(ItemStack stack, InventoryContainer container, Vector2 screenPosition)
    {
        if (stack?.Item == null)
            return;

        ContextMenuModel model = ContextMenuBuilder.Build(
            stack,
            container,
            _session,
            InventoryContextMenuCatalog.All);

        if (model.IsEmpty)
        {
            Hide();
            return;
        }

        Show(model, screenPosition);
    }

    void Show(ContextMenuModel model, Vector2 screenPosition)
    {
        ClearPanels();
        CancelCloseDelay();

        UIContextMenuCascadePanel rootPanel = SpawnPanel();
        if (rootPanel == null)
            return;

        rootPanel.Bind(model.Roots, depth: 0, OnRowEnter, OnRowExit, OnRowClick, OnPanelEnter);
        PlacePanelAtScreenPoint(rootPanel.Root, screenPosition);
        ClampPanelToScreen(rootPanel.Root);
        _openPanels.Add(rootPanel);

        SetRootRaycastEnabled(true);
        _isOpen = true;
        _hoverDepth = 0;
    }

    public void Hide()
    {
        if (!_isOpen && _openPanels.Count == 0)
            return;

        CancelCloseDelay();
        ClearPanels();
        SetRootRaycastEnabled(false);
        _isOpen = false;
        _hoverDepth = -1;
    }

    void OnPanelEnter(UIContextMenuCascadePanel panel)
    {
        if (panel == null)
            return;

        CancelCloseDelay();
        _hoverDepth = panel.Depth;
    }

    void OnRowEnter(UIContextMenuItemRow row)
    {
        if (row?.Entry == null)
            return;

        CancelCloseDelay();
        _hoverDepth = row.Depth;

        if (row.Entry.HasChildren)
            OpenOrReplaceSubmenu(row);
        else
            ClosePanelsDeeperThan(row.Depth);
    }

    void OnRowExit(UIContextMenuItemRow row)
    {
        if (row == null)
            return;

        ScheduleCloseDeeperThan(row.Depth);
    }

    void OnRowClick(UIContextMenuItemRow row)
    {
        if (row?.Entry == null)
            return;

        CancelCloseDelay();

        if (row.Entry.HasChildren)
        {
            // 재클릭: 이미 이 depth+1이 열려 있으면 토글 닫기
            if (_openPanels.Count > row.Depth + 1)
            {
                ClosePanelsDeeperThan(row.Depth);
                return;
            }

            OpenOrReplaceSubmenu(row);
            return;
        }

        if (row.Entry.Action == null)
            return;

        if (!string.IsNullOrEmpty(row.Entry.Action.GetDisabledReason()))
            return;

        row.Entry.Action.Execute();
        Hide();
    }

    void OpenOrReplaceSubmenu(UIContextMenuItemRow row)
    {
        ClosePanelsDeeperThan(row.Depth);

        UIContextMenuCascadePanel panel = SpawnPanel();
        if (panel == null)
            return;

        panel.Bind(row.Entry.Children, row.Depth + 1, OnRowEnter, OnRowExit, OnRowClick, OnPanelEnter);
        PlaceSubmenuBeside(panel.Root, row.Rect);
        ClampPanelToScreen(panel.Root);
        _openPanels.Add(panel);
    }

    void ClosePanelsDeeperThan(int depth)
    {
        for (int i = _openPanels.Count - 1; i > depth; i--)
        {
            UIContextMenuCascadePanel panel = _openPanels[i];
            if (panel != null)
            {
                panel.ClearRows();
                Destroy(panel.gameObject);
            }

            _openPanels.RemoveAt(i);
        }
    }

    void ScheduleCloseDeeperThan(int depth)
    {
        CancelCloseDelay();
        _closeDelayRoutine = StartCoroutine(CloseDeeperAfterDelay(depth));
    }

    IEnumerator CloseDeeperAfterDelay(int depth)
    {
        yield return new WaitForSecondsRealtime(ContextMenuStyle.CloseDelaySeconds);
        _closeDelayRoutine = null;
        if (_hoverDepth <= depth)
            ClosePanelsDeeperThan(depth);
    }

    void CancelCloseDelay()
    {
        if (_closeDelayRoutine == null)
            return;

        StopCoroutine(_closeDelayRoutine);
        _closeDelayRoutine = null;
    }

    UIContextMenuCascadePanel SpawnPanel()
    {
        if (_panelPrefab == null || _panelRoot == null)
            return null;

        UIContextMenuCascadePanel panel = Instantiate(_panelPrefab, _panelRoot);
        panel.gameObject.SetActive(true);

        // rowPrefab는 패널 SerializeField에 이미 bake. 없으면 호스트 템플릿으로 보정하지 않음(런타임 AddComponent 금지).
        return panel;
    }

    void PlacePanelAtScreenPoint(RectTransform panel, Vector2 screenPosition)
    {
        UIPopupPositioner.PlaceAtScreenPoint(panel, screenPosition, _rootCanvas);
    }

    void PlaceSubmenuBeside(RectTransform panel, RectTransform anchorRow)
    {
        if (panel == null || anchorRow == null || _panelRoot == null)
            return;

        Camera camera = UIPopupPositioner.ResolveCamera(_rootCanvas);
        Vector3[] corners = new Vector3[4];
        anchorRow.GetWorldCorners(corners);
        // 0=BL 1=TL 2=TR 3=BR — 우측 상단 기준
        Vector3 worldTopRight = corners[2];
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(camera, worldTopRight);
        screenPoint.x += ContextMenuStyle.SubmenuGap;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _panelRoot, screenPoint, camera, out Vector2 localPoint);
        panel.anchoredPosition = localPoint;

        // 화면 밖이면 왼쪽으로 flip
        LayoutRebuilder.ForceRebuildLayoutImmediate(panel);
        Vector3[] panelCorners = new Vector3[4];
        panel.GetWorldCorners(panelCorners);
        float screenWidth = Screen.width;
        if (panelCorners[2].x > screenWidth)
        {
            Vector3 worldTopLeft = corners[1];
            Vector2 leftScreen = RectTransformUtility.WorldToScreenPoint(camera, worldTopLeft);
            leftScreen.x -= ContextMenuStyle.SubmenuGap;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _panelRoot, leftScreen, camera, out Vector2 leftLocal);
            panel.anchoredPosition = new Vector2(
                leftLocal.x - panel.rect.width,
                leftLocal.y);
        }
    }

    void ClampPanelToScreen(RectTransform panel)
    {
        if (panel == null || _panelRoot == null)
            return;

        LayoutRebuilder.ForceRebuildLayoutImmediate(panel);
        Camera camera = UIPopupPositioner.ResolveCamera(_rootCanvas);
        Vector3[] corners = new Vector3[4];
        panel.GetWorldCorners(corners);

        float dx = 0f;
        float dy = 0f;
        if (corners[0].x < 0f)
            dx = -corners[0].x;
        if (corners[2].x > Screen.width)
            dx = Screen.width - corners[2].x;
        if (corners[0].y < 0f)
            dy = -corners[0].y;
        if (corners[1].y > Screen.height)
            dy = Screen.height - corners[1].y;

        if (Mathf.Approximately(dx, 0f) && Mathf.Approximately(dy, 0f))
            return;

        Vector2 screenCenter = RectTransformUtility.WorldToScreenPoint(camera, panel.position);
        screenCenter.x += dx;
        screenCenter.y += dy;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _panelRoot, screenCenter, camera, out Vector2 local);
        panel.anchoredPosition = local;
    }

    void SetRootRaycastEnabled(bool enabled)
    {
        if (_rootRaycastImage == null)
            TryGetComponent(out _rootRaycastImage);

        if (_rootRaycastImage != null)
            _rootRaycastImage.raycastTarget = enabled;
    }

    void ClearPanels()
    {
        for (int i = _openPanels.Count - 1; i >= 0; i--)
        {
            UIContextMenuCascadePanel panel = _openPanels[i];
            if (panel != null)
            {
                panel.ClearRows();
                Destroy(panel.gameObject);
            }
        }

        _openPanels.Clear();
    }
}
