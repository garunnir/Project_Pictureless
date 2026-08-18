// ============================================================
// UIContextMenuHost — 공용 캐스케이드 컨텍스트 메뉴 호스트 (Model 표시)
// ============================================================

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public sealed class UIContextMenuHost : MonoBehaviour, IPointerClickHandler
{
    public const string ResourcePath = "UI/TileObjectContextMenu";

    public static UIContextMenuHost Instance { get; private set; }

    /// <summary>선택: ContextMenu 레이어 Transform. DistScript의 UICanvasLayerHost가 등록.</summary>
    public static Func<Transform> TryResolveParent;

    [SerializeField] RectTransform _panelRoot;
    [SerializeField] UIContextMenuCascadePanel _panelPrefab;
    [SerializeField] UIContextMenuItemRow _rowPrefab;

    readonly List<UIContextMenuCascadePanel> _openPanels = new();

    Canvas _rootCanvas;
    Image _rootRaycastImage;
    bool _isOpen;
    Coroutine _closeDelayRoutine;
    int _hoverDepth = -1;

    public bool IsOpen => _isOpen;

    public static bool TryShow(ContextMenuModel model, Vector2 screenPosition)
    {
        if (model == null || model.IsEmpty)
            return false;

        UIContextMenuHost host = EnsureInstance();
        if (host == null)
            return false;

        host.Show(model, screenPosition);
        return true;
    }

    public static UIContextMenuHost EnsureInstance()
    {
        if (Instance != null)
            return Instance;

        UIContextMenuHost prefab = Resources.Load<UIContextMenuHost>(ResourcePath);
        if (prefab == null)
        {
            Debug.LogError("[UIContextMenuHost] Missing Resources/" + ResourcePath + " prefab.");
            return null;
        }

        Transform parent = ResolveDefaultParent();
        UIContextMenuHost host = Instantiate(prefab, parent);
        host.name = "TileObjectContextMenu";
        host.Initialize(ResolveDefaultCanvas(parent));
        host.Hide();
        return Instance;
    }

    static Transform ResolveDefaultParent()
    {
        Transform resolved = TryResolveParent?.Invoke();
        if (resolved != null)
            return resolved;

        Canvas canvas = FindFirstObjectByType<Canvas>();
        return canvas != null ? canvas.transform : null;
    }

    static Canvas ResolveDefaultCanvas(Transform parent)
    {
        if (parent != null)
        {
            Canvas canvas = parent.GetComponentInParent<Canvas>();
            if (canvas != null)
                return canvas;
        }

        return FindFirstObjectByType<Canvas>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[UIContextMenuHost] Duplicate instance ignored.", this);
            return;
        }

        Instance = this;
        TryGetComponent(out _rootRaycastImage);
        SetRootRaycastEnabled(false);
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void OnEnable()
    {
        ContextMenuHostEvents.HideRequested += Hide;
    }

    void OnDisable()
    {
        ContextMenuHostEvents.HideRequested -= Hide;
        Hide();
    }

    void Update()
    {
        if (!_isOpen)
            return;

        TryHideOnOutsidePress();
    }

    void TryHideOnOutsidePress()
    {
        if (!ContextMenuOutsideClick.TryGetPressScreenPosition(out Vector2 screen))
            return;

        Camera camera = UIPopupPositioner.ResolveCamera(_rootCanvas);
        if (ContextMenuOutsideClick.IsOverAnyPanel(_openPanels, screen, camera))
            return;

        Hide();
    }

    public void Initialize(Canvas rootCanvas)
    {
        _rootCanvas = rootCanvas;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!_isOpen)
            return;

        Hide();
    }

    public void Show(ContextMenuModel model, Vector2 screenPosition)
    {
        if (model == null || model.IsEmpty)
        {
            Hide();
            return;
        }

        ContextMenuHostEvents.RequestHide();
        ClearPanels();
        CancelCloseDelay();

        UIContextMenuCascadePanel rootPanel = SpawnPanel();
        if (rootPanel == null)
            return;

        rootPanel.Bind(
            ContextMenuOverflow.Fold(model.Roots),
            depth: 0,
            OnRowEnter,
            OnRowExit,
            OnRowClick,
            OnPanelEnter);
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
        Vector3 worldTopRight = corners[2];
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(camera, worldTopRight);
        screenPoint.x += ContextMenuStyle.SubmenuGap;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _panelRoot, screenPoint, camera, out Vector2 localPoint);
        panel.anchoredPosition = localPoint;

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
