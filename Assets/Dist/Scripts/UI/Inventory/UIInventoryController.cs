// ============================================================
// UIInventoryController — 인벤 UI + [B] PlayerAction 억제 + 스크롤 드래그 overlay
// ============================================================
// [A] UiMenu 입력은 사용하지 않음 (건설 UI 전용).
// 창 위 마우스 구간: Zoom/Aim만 SuppressPlayerAction. Move(WASD)는 유지.

using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public sealed class UIInventoryController : MonoBehaviour, IInventoryOverlayController, IInventoryScrollDragHost, IInventoryItemDragHost
{
    [Required, SerializeField] UIInventoryListWindow _windowPrefab;
    [SerializeField] GameObject _scrollDragOverlayPrefab;
    [SerializeField] UIItemContextMenu _contextMenuPrefab;
    [SerializeField] UIInventoryItemDetailPanel _itemDetailPanelPrefab;
    [SerializeField] UIInventoryListWindow _primaryWindow;
    [SerializeField] UIInventoryListWindow _lootWindow;
    [SerializeField] Canvas _uiCanvas;
    [SerializeField] UICanvasLayerHost _layerHost;
    [SerializeField] GameObject _scrollDragOverlay;
    [SerializeField] UIItemContextMenu _contextMenu;
    [SerializeField] UIInventoryItemDetailPanel _itemDetailPanel;
    [SerializeField] Vector2 _primaryWindowInitialPosition = new(-220f, 0f);
    [SerializeField] Vector2 _lootWindowInitialPosition = new(200f, 0f);
    [SerializeField] InventoryWindowLauncher _primaryLauncher;
    [SerializeField] InventoryWindowLauncher _lootLauncher;

    PlayerInventoryRuntime _activeRuntime;
    UIItemDragGhostService _dragGhostService;
    int _itemDragDepth;
    int _scrollDragDepth;
    bool _isPrimaryOpen;
    bool _isLootOpen;
    bool _cachedSuppressMouseActions;
    bool _hasCachedSuppressState;

    bool IsAnyWindowOpen => _isPrimaryOpen || _isLootOpen;

    static void SetWindowActive(UIInventoryListWindow window, bool active)
    {
        if (window)
            window.gameObject.SetActive(active);
    }

    void Awake()
    {
        EnsureReferences();
        EnsureWindows();
        _primaryWindow?.ListView?.PrewarmRowPool();
        EnsureScrollDragOverlay();
        EnsureDragGhostService();
        EnsureContextMenu();
        WireScrollDragHandler(_primaryWindow);
        WireScrollDragHandler(_lootWindow);

        if (_primaryWindow != null)
            _primaryWindow.gameObject.SetActive(false);
        if (_lootWindow != null)
            _lootWindow.gameObject.SetActive(false);

        SyncLauncherVisuals();
        UIItemListRow.DoubleClicked += OnItemDoubleClicked;
        UIItemListRow.Hovered += OnItemHovered;
        UIItemListRow.HoverEnded += OnItemHoverEnded;
        UIItemListRow.RightClicked += OnItemRightClickedHideDetail;
    }

    void OnEnable() => PlayerInventoryRuntime.ActiveChanged += OnActivePlayerChanged;

    void Start()
    {
        if (InputManager.Instance != null)
            InputManager.Instance.PlayerInventoryTogglePerformed += OnInventoryTogglePerformed;
    }

    void OnDisable()
    {
        PlayerInventoryRuntime.ActiveChanged -= OnActivePlayerChanged;
        ClearMouseActionSuppressions();
        CloseAllWindows();
    }

    void OnDestroy()
    {
        UIItemListRow.DoubleClicked -= OnItemDoubleClicked;
        UIItemListRow.Hovered -= OnItemHovered;
        UIItemListRow.HoverEnded -= OnItemHoverEnded;
        UIItemListRow.RightClicked -= OnItemRightClickedHideDetail;

        if (InputManager.Instance != null)
            InputManager.Instance.PlayerInventoryTogglePerformed -= OnInventoryTogglePerformed;

        CloseAllWindows();
    }

    void OnItemRightClickedHideDetail(ItemStack _, InventoryContainer __, Vector2 ___) =>
        HideItemDetailPanel();

    void OnItemHovered(ItemStack stack, Vector2 screenPosition)
    {
        if (!IsAnyWindowOpen || InventoryDragState.IsDragging)
            return;

        EnsureItemDetailPanel();
        _itemDetailPanel?.Show(stack, screenPosition);
    }

    void OnItemHoverEnded() => HideItemDetailPanel();

    void HideItemDetailPanel() => _itemDetailPanel?.Hide();

    void OnItemDoubleClicked(ItemStack stack, InventoryContainer sourceContainer, UIItemListView sourceListView)
    {
        InventorySession session = _activeRuntime?.Session;
        if (session == null)
            return;

        InventoryDragDrop.TryQuickTransferBetweenWindows(
            session,
            _primaryWindow,
            _lootWindow,
            sourceListView,
            stack,
            sourceContainer);
    }

    void OnInventoryTogglePerformed(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        TogglePrimaryWindow();
    }

    void LateUpdate()
    {
        bool suppressMouseActions =
            _itemDragDepth > 0 || _scrollDragDepth > 0 || IsPointerOverAnyDistWindow();
        if (!_hasCachedSuppressState || suppressMouseActions != _cachedSuppressMouseActions)
            ApplyMouseActionSuppression(suppressMouseActions);

        if (_itemDetailPanel != null && _itemDetailPanel.IsVisible && IsAnyWindowOpen && Mouse.current != null)
            _itemDetailPanel.SetScreenPosition(Mouse.current.position.ReadValue());
    }

    void FinalizeItemDrag()
    {
        if (InventoryDragState.IsDragging)
        {
            TryDropActiveDragToFloorIfOutsideWindows();
            InventoryDragState.End();
        }

        HideDragGhost();
        RefreshVisibleWindowsAfterDrag();
    }

    void HideDragGhost()
    {
        EnsureDragGhostService();
        _dragGhostService?.Hide();
    }

    // 등록된 Dist 창 Rect 밖에서 놓으면 사이드바 floor-loot 탭 드롭과 동일 경로로 바닥 투하.
    void TryDropActiveDragToFloorIfOutsideWindows()
    {
        if (!InventoryDragState.TryGetActive(out InventoryDragPayload payload))
            return;

        if (InventoryDragState.WasConsumed)
            return;

        if (IsPointerOverAnyDistWindow())
            return;

        if (payload.SourceContainer == null || payload.Stacks == null || payload.Stacks.Count == 0)
            return;

        InventorySession session = _activeRuntime?.Session;
        if (session == null || !TryGetFloorLootContainer(session, out InventoryContainer floor))
            return;

        if (payload.SourceContainer == floor)
            return;

        InventoryDragDrop.TryApplyTo(session, floor);
    }

    static bool TryGetFloorLootContainer(InventorySession session, out InventoryContainer floor)
    {
        floor = null;

        IReadOnlyList<InventoryContainer> sidebar = session.GetSidebarContainers();
        for (int i = 0; i < sidebar.Count; i++)
        {
            InventoryContainer container = sidebar[i];
            if (container != null && container.InstanceId == FloorLootHost.DefaultInstanceId)
            {
                floor = container;
                return true;
            }
        }

        return false;
    }

    void RefreshVisibleWindowsAfterDrag()
    {
        if (_primaryWindow && _primaryWindow.IsVisible)
        {
            _primaryWindow.ClearSidebarDropHovers();
            _primaryWindow.SyncDeferredAfterDrag();
        }

        if (_lootWindow && _lootWindow.IsVisible)
        {
            _lootWindow.ClearSidebarDropHovers();
            _lootWindow.SyncDeferredAfterDrag();
        }
    }

    void ApplyMouseActionSuppression(bool suppressMouseActions)
    {
        _cachedSuppressMouseActions = suppressMouseActions;
        _hasCachedSuppressState = true;

        InputManager input = InputManager.Instance;
        if (input == null)
            return;

        input.SuppressPlayerAction(PlayerAction.Zoom, this, suppressMouseActions);
        input.SuppressPlayerAction(PlayerAction.Aim, this, suppressMouseActions);
    }

    void OnValidate() => EnsureReferences();

    public void OnScrollDragStarted()
    {
        if (++_scrollDragDepth == 1 && _scrollDragOverlay != null)
            _scrollDragOverlay.SetActive(true);
    }

    public void OnScrollDragEnded()
    {
        if (_scrollDragDepth <= 0)
            return;

        _scrollDragDepth--;
        if (_scrollDragDepth == 0 && _scrollDragOverlay != null)
            _scrollDragOverlay.SetActive(false);
    }

    public void OnItemDragStarted()
    {
        HideItemDetailPanel();
        _itemDragDepth++;
    }

    public void OnItemDragEnded()
    {
        if (_itemDragDepth > 0)
            _itemDragDepth--;

        FinalizeItemDrag();
    }

    void ConfigureWindow(UIInventoryListWindow window)
    {
        if (window == null || _uiCanvas == null)
            return;

        Vector2 maxSize = InventoryWindowLayout.GetMaxSize(_uiCanvas);
        Vector2 minSize = new(InventoryWindowLayout.MinWidth, InventoryWindowLayout.MinHeight);

        ApplyWindowSizeClamp(window, minSize, maxSize);
        if (window == _primaryWindow)
            window.BindChromeClose(ClosePrimaryWindow);
        else if (window == _lootWindow)
            window.BindChromeClose(CloseLootWindow);
        window.ConfigureWindowChrome(_uiCanvas, minSize, maxSize);
        window.ConfigureDragAndDrop(this, _uiCanvas, EnsureDragGhostService());
    }

    void ApplyWindowSizeClamp(UIInventoryListWindow window, Vector2 minSize, Vector2 maxSize)
    {
        RectTransform rect = window.WindowRect;
        if (rect == null)
            return;

        Vector2 size = rect.sizeDelta;
        size.x = Mathf.Clamp(size.x, minSize.x, maxSize.x);
        size.y = Mathf.Clamp(size.y, minSize.y, maxSize.y);
        rect.sizeDelta = size;
    }

    void EnsureReferences()
    {
        if (!_uiCanvas) _uiCanvas = FindAnyObjectByType<Canvas>();
        if (!_layerHost && _uiCanvas) _layerHost = _uiCanvas.GetComponent<UICanvasLayerHost>();
    }

    UIItemDragGhostService EnsureDragGhostService()
    {
        EnsureReferences();
        if (_dragGhostService != null)
            return _dragGhostService;

        if (_uiCanvas == null)
            return null;

        if (!UIItemDragGhostService.TryGet(_uiCanvas, out _dragGhostService) || _dragGhostService == null)
        {
            Debug.LogError(
                "[UIInventoryController] UIItemDragGhostService missing on UICanvas. Run Dist/MCP/Inventory/Setup Canvas Overlays In Open Scene.",
                this);
            return null;
        }

        _dragGhostService.EnsureReady();
        return _dragGhostService;
    }

    void EnsureWindows()
    {
        if (_uiCanvas == null)
            _uiCanvas = FindAnyObjectByType<Canvas>();

        if (_uiCanvas == null)
        {
            Debug.LogError("[UIInventoryController] Canvas not found.", this);
            return;
        }

        if (_windowPrefab == null)
        {
            Debug.LogError("[UIInventoryController] Window prefab is not assigned.", this);
            return;
        }

        Transform windowRoot = _layerHost != null
            ? _layerHost.GetLayerRoot(UICanvasLayer.Window)
            : _uiCanvas.transform;

        if (_primaryWindow == null)
        {
            _primaryWindow = Instantiate(_windowPrefab, windowRoot);
            _primaryWindow.name = "Grp_InventoryListWindow_Primary";
            _primaryWindow.WindowRect.anchoredPosition = _primaryWindowInitialPosition;
        }

        if (_lootWindow == null)
        {
            _lootWindow = Instantiate(_windowPrefab, windowRoot);
            _lootWindow.name = "Grp_InventoryListWindow_Loot";
            _lootWindow.WindowRect.anchoredPosition = _lootWindowInitialPosition;
        }
    }

    void EnsureScrollDragOverlay()
    {
        if (_scrollDragOverlay != null || _uiCanvas == null)
            return;

        if (_scrollDragOverlayPrefab == null)
        {
            Debug.LogError(
                "[UIInventoryController] Scroll drag overlay prefab is not assigned. Run Dist/MCP/Inventory/Setup Canvas Overlays In Open Scene.",
                this);
            return;
        }

        Transform parent = ResolveLayerRoot(UICanvasLayer.Overlay);
        _scrollDragOverlay = Instantiate(_scrollDragOverlayPrefab, parent);
        _scrollDragOverlay.name = "InventoryScrollDragOverlay";
        _scrollDragOverlay.SetActive(false);
    }

    void EnsureContextMenu()
    {
        if (_contextMenu != null || _uiCanvas == null)
            return;

        if (_contextMenuPrefab == null)
        {
            Debug.LogError(
                "[UIInventoryController] Context menu prefab is not assigned. Run Dist/MCP/Inventory/Setup Canvas Overlays In Open Scene.",
                this);
            return;
        }

        Transform parent = ResolveLayerRoot(UICanvasLayer.ContextMenu);
        _contextMenu = Instantiate(_contextMenuPrefab, parent);
        _contextMenu.name = "ItemContextMenu";
        _contextMenu.Hide();
    }

    void EnsureItemDetailPanel()
    {
        if (_itemDetailPanel != null || _uiCanvas == null)
            return;

        if (_itemDetailPanelPrefab == null)
        {
            Debug.LogError(
                "[UIInventoryController] Item detail panel prefab is not assigned. Run Dist/MCP/Inventory/Setup Canvas Overlays In Open Scene.",
                this);
            return;
        }

        Transform parent = ResolveLayerRoot(UIHoverCanvasLayer.Layer);
        _itemDetailPanel = Instantiate(_itemDetailPanelPrefab, parent);
        _itemDetailPanel.name = "InventoryItemDetailPanel";
        _itemDetailPanel.Initialize(_uiCanvas);
        _itemDetailPanel.Hide();
    }

    Transform ResolveLayerRoot(UICanvasLayer layer)
    {
        if (_layerHost != null)
            return _layerHost.GetLayerRoot(layer);

        return _uiCanvas != null ? _uiCanvas.transform : transform;
    }

    void WireScrollDragHandler(UIInventoryListWindow window)
    {
        if (window == null)
            return;

        InventoryScrollDragHandler[] handlers =
            window.GetComponentsInChildren<InventoryScrollDragHandler>(true);
        if (handlers == null || handlers.Length == 0)
        {
            Debug.LogError("[UIInventoryController] InventoryScrollDragHandler missing on inventory window prefab.", window);
            return;
        }

        for (int i = 0; i < handlers.Length; i++)
            handlers[i].Bind(this);
    }

    public void ToggleInventory() => TogglePrimaryWindow();

    public void TogglePrimaryWindow()
    {
        if (_isPrimaryOpen)
            ClosePrimaryWindow();
        else
            OpenPrimaryWindow();
    }

    public void ToggleLootWindow()
    {
        if (_isLootOpen)
            CloseLootWindow();
        else
            OpenLootWindow(null);
    }

    public void OpenInventory() => OpenPrimaryWindow();

    public void OpenPrimaryWindow()
    {
        if (_isPrimaryOpen)
            return;

        PlayerInventoryRuntime runtime = PlayerInventoryRuntime.Active;
        if (runtime?.Host?.Container == null)
        {
            Debug.LogWarning("[UIInventoryController] No active player inventory runtime.", this);
            return;
        }

        EnsureInventoryContext(runtime);

        _primaryWindow.gameObject.SetActive(true);
        _primaryWindow.SetHeaderTitle(InventoryWindowLabels.PrimaryTitle);
        // Configure first so Initialize.RefreshAll binds rows with drag host in one pass.
        ConfigureWindow(_primaryWindow);
        _primaryWindow.Initialize(runtime.Session, InventoryWindowMode.PlayerOnly, runtime.Host.Container);
        _isPrimaryOpen = true;
        SyncLauncherVisuals();

        UIItemListView listView = _primaryWindow.ListView;
        Debug.Log(
            $"[UIInventoryController] OpenPrimaryWindow stacks={listView?.BoundStackCount ?? 0} visibleRows={listView?.ActiveRowCount ?? 0}",
            this);
    }

    public void CloseInventory() => CloseAllWindows();

    public void ClosePrimaryWindow()
    {
        if (!_isPrimaryOpen)
            return;

        SetWindowActive(_primaryWindow, false);
        _isPrimaryOpen = false;
        SyncLauncherVisuals();
        TryEndInventoryContext();
        CleanupIfNoWindowsOpen();
    }

    public void CloseLootWindow()
    {
        if (!_isLootOpen)
            return;

        _activeRuntime?.LootProximity?.ClearActive();
        SetWindowActive(_lootWindow, false);
        _isLootOpen = false;
        SyncLauncherVisuals();
        TryEndInventoryContext();
        CleanupIfNoWindowsOpen();
    }

    void CloseAllWindows()
    {
        bool wasOpen = IsAnyWindowOpen;

        SetWindowActive(_primaryWindow, false);
        SetWindowActive(_lootWindow, false);
        _activeRuntime?.LootProximity?.ClearActive();
        _isPrimaryOpen = false;
        _isLootOpen = false;
        SyncLauncherVisuals();

        if (wasOpen)
            TryEndInventoryContext();

        CleanupIfNoWindowsOpen();
    }

    void SyncLauncherVisuals()
    {
        if (_primaryLauncher != null)
            _primaryLauncher.SetOpen(_isPrimaryOpen);
        if (_lootLauncher != null)
            _lootLauncher.SetOpen(_isLootOpen);
    }

    void CleanupIfNoWindowsOpen()
    {
        if (IsAnyWindowOpen)
            return;

        _itemDragDepth = 0;
        _scrollDragDepth = 0;
        if (_scrollDragOverlay != null)
            _scrollDragOverlay.SetActive(false);

        if (_contextMenu != null)
            _contextMenu.Hide();

        HideItemDetailPanel();

        InventoryDragState.End();
        HideDragGhost();
        ClearMouseActionSuppressions();
    }

    void ClearMouseActionSuppressions()
    {
        ApplyMouseActionSuppression(false);
        _hasCachedSuppressState = false;
    }

    bool IsPointerOverAnyDistWindow()
    {
        InputManager input = InputManager.Instance;
        if (input == null || !input.TryReadPointerScreenPosition(out Vector2 position))
            return false;

        return UIOverlayWindowHitTest.ContainsScreenPoint(position, GetCanvasCamera());
    }

    Camera GetCanvasCamera() =>
        _uiCanvas != null && _uiCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? _uiCanvas.worldCamera
            : null;

    public void OpenLoot(InventoryContainer focusContainer)
    {
        PlayerInventoryRuntime runtime = PlayerInventoryRuntime.Active;
        if (runtime == null)
        {
            Debug.LogWarning("[UIInventoryController] No active player inventory runtime.", this);
            return;
        }

        EnsureInventoryContext(runtime);
        runtime.RefreshNearbyContainers();

        if (focusContainer != null && !runtime.IsWorldLootContainer(focusContainer.InstanceId))
            runtime.TryIncludeLootContainer(focusContainer);

        if (focusContainer != null)
            runtime.SeedContainerIfEmpty(focusContainer);

        if (!_isLootOpen)
            OpenLootWindow(focusContainer);
        else if (focusContainer != null &&
                 runtime.IsWorldLootContainer(focusContainer.InstanceId))
            _lootWindow.SelectContainer(focusContainer);
        else
            _lootWindow.OnSidebarChanged();
    }

    void OpenLootWindow(InventoryContainer initialFocus)
    {
        PlayerInventoryRuntime runtime = PlayerInventoryRuntime.Active;
        if (runtime == null)
            return;

        EnsureInventoryContext(runtime);
        runtime.RefreshNearbyContainers();

        InventoryContainer focus = initialFocus;
        if (focus != null && !runtime.IsWorldLootContainer(focus.InstanceId))
            runtime.TryIncludeLootContainer(focus);

        if (focus != null && !runtime.IsWorldLootContainer(focus.InstanceId))
            focus = null;

        if (focus == null)
        {
            IReadOnlyList<InventoryContainer> detected = runtime.LootProximity.DetectedContainers;
            for (int i = 0; i < detected.Count; i++)
            {
                focus = detected[i];
                break;
            }
        }

        _lootWindow.gameObject.SetActive(true);
        _lootWindow.SetHeaderTitle(InventoryWindowLabels.LootTitle);
        // Configure first so Initialize.RefreshAll binds rows with drag host in one pass.
        ConfigureWindow(_lootWindow);
        _lootWindow.Initialize(runtime.Session, InventoryWindowMode.NearbyOnly, focus);
        _isLootOpen = true;
        SyncLauncherVisuals();
    }

    void OnActivePlayerChanged(PlayerInventoryRuntime runtime)
    {
        if (IsAnyWindowOpen)
            CloseAllWindows();
        else
            UnbindRuntime();
    }

    void EnsureInventoryContext(PlayerInventoryRuntime runtime)
    {
        if (runtime == null)
            return;

        BindRuntime(runtime);
        runtime.AcquireContext(this);
    }

    void TryEndInventoryContext()
    {
        if (IsAnyWindowOpen)
            return;

        _activeRuntime?.ReleaseContext(this);
        UnbindRuntime();
    }

    void BindRuntime(PlayerInventoryRuntime runtime)
    {
        if (_activeRuntime == runtime)
            return;

        UnbindRuntime();
        _activeRuntime = runtime;

        if (_activeRuntime?.Session != null)
        {
            _activeRuntime.Session.SidebarChanged += OnSessionChanged;
            _activeRuntime.Session.StacksChanged += OnInventoryDataChanged;

            EnsureContextMenu();
            if (_contextMenu != null)
                _contextMenu.Initialize(_activeRuntime.Session, _uiCanvas);
        }

        if (_activeRuntime?.LootProximity != null)
        {
            _activeRuntime.LootProximity.NearbyContainersChanged += OnLootNearbyContainersChanged;
            _activeRuntime.LootProximity.ActiveLootContainerChanged += OnLootActiveContainerChanged;
        }
    }

    void UnbindRuntime()
    {
        if (_activeRuntime?.LootProximity != null)
        {
            _activeRuntime.LootProximity.NearbyContainersChanged -= OnLootNearbyContainersChanged;
            _activeRuntime.LootProximity.ActiveLootContainerChanged -= OnLootActiveContainerChanged;
        }

        if (_activeRuntime?.Session != null)
        {
            _activeRuntime.Session.SidebarChanged -= OnSessionChanged;
            _activeRuntime.Session.StacksChanged -= OnInventoryDataChanged;
        }

        _activeRuntime = null;
    }

    void OnLootNearbyContainersChanged(IReadOnlyList<InventoryContainer> _)
    {
        if (_lootWindow && _lootWindow.IsVisible)
            _lootWindow.OnSidebarChanged();
    }

    void OnLootActiveContainerChanged(InventoryContainer container)
    {
        if (_lootWindow && _lootWindow.IsVisible)
            _lootWindow.ApplyActiveLootContainer(container);
    }

    void OnSessionChanged()
    {
        if (_primaryWindow && _primaryWindow.IsVisible)
            _primaryWindow.OnSidebarChanged();

        if (_lootWindow && _lootWindow.IsVisible)
            _lootWindow.OnSidebarChanged();
    }

    void OnInventoryDataChanged(InventoryStacksChangeSet changeSet)
    {
        if (_primaryWindow && _primaryWindow.IsVisible)
            _primaryWindow.SyncFromChangeSet(changeSet);

        if (_lootWindow && _lootWindow.IsVisible)
            _lootWindow.SyncFromChangeSet(changeSet);
    }

    void RefreshVisibleWindows()
    {
        OnSessionChanged();
        OnInventoryDataChanged(InventoryStacksChangeSet.Full);
    }
}
