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
    [SerializeField] UIInventoryListWindow _primaryWindow;
    [SerializeField] UIInventoryListWindow _lootWindow;
    [SerializeField] Canvas _uiCanvas;
    [SerializeField] UICanvasLayerHost _layerHost;
    [SerializeField] UIInventoryDragGhost _dragGhost;
    [SerializeField] GameObject _scrollDragOverlay;
    [SerializeField] UIItemContextMenu _contextMenu;
    [SerializeField] Vector2 _primaryWindowInitialPosition = new(-220f, 0f);
    [SerializeField] Vector2 _lootWindowInitialPosition = new(220f, 0f);

    PlayerInventoryRuntime _activeRuntime;
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
        EnsureScrollDragOverlay();
        EnsureDragGhost();
        WireScrollDragHandler(_primaryWindow);
        WireScrollDragHandler(_lootWindow);

        if (_primaryWindow != null)
            _primaryWindow.gameObject.SetActive(false);
        if (_lootWindow != null)
            _lootWindow.gameObject.SetActive(false);
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
        if (InputManager.Instance != null)
            InputManager.Instance.PlayerInventoryTogglePerformed -= OnInventoryTogglePerformed;

        CloseAllWindows();
    }

    void OnInventoryTogglePerformed(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        TogglePrimaryWindow();
    }

    void LateUpdate()
    {
        if (!IsAnyWindowOpen)
        {
            if (_hasCachedSuppressState && _cachedSuppressMouseActions)
                ApplyMouseActionSuppression(false);
            return;
        }

        bool suppressMouseActions =
            _itemDragDepth > 0 || _scrollDragDepth > 0 || IsPointerOverAnyVisibleWindow();
        if (!_hasCachedSuppressState || suppressMouseActions != _cachedSuppressMouseActions)
            ApplyMouseActionSuppression(suppressMouseActions);
    }

    void FinalizeItemDrag()
    {
        if (InventoryDragState.IsDragging)
            InventoryDragState.End();

        HideDragGhost();
        RefreshVisibleWindowsAfterDrag();
    }

    void RefreshVisibleWindowsAfterDrag()
    {
        if (_primaryWindow && _primaryWindow.IsVisible)
        {
            _primaryWindow.ClearSidebarDropHovers();
            _primaryWindow.RefreshListOnly();
        }

        if (_lootWindow && _lootWindow.IsVisible)
        {
            _lootWindow.ClearSidebarDropHovers();
            _lootWindow.RefreshListOnly();
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
        _itemDragDepth++;
    }

    public void OnItemDragEnded()
    {
        if (_itemDragDepth > 0)
            _itemDragDepth--;

        FinalizeItemDrag();
    }

    public void BeginDragGhost(Vector2 screenPosition, int stackCount)
    {
        EnsureDragGhost();
        if (!_dragGhost)
            return;

        _dragGhost.Show(ResolveDragIcon(), stackCount, screenPosition);
    }

    public void UpdateDragGhostPosition(Vector2 screenPosition)
    {
        EnsureDragGhost();
        if (!_dragGhost)
            return;

        _dragGhost.SetScreenPosition(screenPosition);
    }

    Sprite ResolveDragIcon()
    {
        if (InventoryDragState.TryGetActive(out InventoryDragPayload payload) &&
            payload.Stacks != null &&
            payload.Stacks.Count > 0 &&
            payload.Stacks[0]?.Item != null)
        {
            return ItemVisualPresenter.GetDisplayIcon(payload.Stacks[0].Item.id);
        }

        return ItemVisualPresenter.GetDefaultIcon();
    }

    public void HideDragGhost()
    {
        if (!_dragGhost)
        {
            _dragGhost = null;
            return;
        }

        _dragGhost.Hide();
    }

    void EnsureDragGhost()
    {
        EnsureReferences();
        if (_uiCanvas == null)
            return;

        if (!_dragGhost)
            _dragGhost = GetComponentInChildren<UIInventoryDragGhost>(true);

        if (!_dragGhost && _layerHost != null)
        {
            Transform topMost = _layerHost.GetLayerRoot(UICanvasLayer.TopMost);
            if (topMost.Find("InventoryDragGhost") is Transform layerChild &&
                layerChild.TryGetComponent(out UIInventoryDragGhost layerGhost))
                _dragGhost = layerGhost;
        }

        if (!_dragGhost && _uiCanvas.transform.Find("InventoryDragGhost") is Transform existing &&
            existing.TryGetComponent(out UIInventoryDragGhost found))
        {
            _dragGhost = found;
        }

        if (!_dragGhost)
        {
            Debug.LogError(
                "[UIInventoryController] UIInventoryDragGhost is not assigned. Run Dist/Inventory/Setup Canvas Overlays In Open Scene.",
                this);
            return;
        }

        _dragGhost.EnsureReady(_uiCanvas);
    }

    void ConfigureWindow(UIInventoryListWindow window)
    {
        if (window == null || _uiCanvas == null)
            return;

        Vector2 maxSize = InventoryWindowLayout.GetMaxSize(_uiCanvas);
        Vector2 minSize = new(InventoryWindowLayout.MinWidth, InventoryWindowLayout.MinHeight);

        ApplyWindowSizeClamp(window, minSize, maxSize);
        window.ConfigureWindowChrome(_uiCanvas, minSize, maxSize);
        window.ConfigureDragAndDrop(this, _uiCanvas);
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

        if (_layerHost != null)
        {
            Transform overlayRoot = _layerHost.GetLayerRoot(UICanvasLayer.Overlay);
            Transform layerChild = overlayRoot.Find("InventoryScrollDragOverlay");
            if (layerChild != null)
                _scrollDragOverlay = layerChild.gameObject;
        }

        if (_scrollDragOverlay == null)
        {
            Transform existing = _uiCanvas.transform.Find("InventoryScrollDragOverlay");
            if (existing != null)
                _scrollDragOverlay = existing.gameObject;
        }

        if (_scrollDragOverlay == null)
        {
            Debug.LogError(
                "[UIInventoryController] InventoryScrollDragOverlay is not assigned. Run Dist/Inventory/Setup Canvas Overlays In Open Scene.",
                this);
        }
    }

    void WireScrollDragHandler(UIInventoryListWindow window)
    {
        if (window == null)
            return;

        InventoryScrollDragHandler handler = window.GetComponentInChildren<InventoryScrollDragHandler>(true);
        if (handler == null)
            Debug.LogError("[UIInventoryController] InventoryScrollDragHandler missing on inventory window prefab.", window);

        handler?.Bind(this);
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
        _primaryWindow.SetHeaderTitle("Inventory");
        _primaryWindow.Initialize(runtime.Session, InventoryWindowMode.PlayerOnly, runtime.Host.Container);
        ConfigureWindow(_primaryWindow);
        _primaryWindow.RefreshListOnly();
        _isPrimaryOpen = true;

        int stackCount = runtime.Host.Container.Stacks.Count;
        Debug.Log(
            $"[UIInventoryController] OpenPrimaryWindow stacks={stackCount} rows={_primaryWindow.ListView?.ActiveRowCount ?? 0}",
            this);
    }

    public void CloseInventory() => CloseAllWindows();

    public void ClosePrimaryWindow()
    {
        if (!_isPrimaryOpen)
            return;

        SetWindowActive(_primaryWindow, false);
        _isPrimaryOpen = false;
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

        if (wasOpen)
            TryEndInventoryContext();

        CleanupIfNoWindowsOpen();
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

        InventoryDragState.End();
        HideDragGhost();
        _dragGhost = null;
        ClearMouseActionSuppressions();
    }

    void ClearMouseActionSuppressions()
    {
        ApplyMouseActionSuppression(false);
        _hasCachedSuppressState = false;
    }

    bool IsPointerOverAnyVisibleWindow()
    {
        InputManager input = InputManager.Instance;
        if (input == null || !input.TryReadPointerScreenPosition(out Vector2 position))
            return false;

        Camera uiCamera = GetCanvasCamera();

        if (_primaryWindow && _primaryWindow.IsVisible &&
            RectTransformUtility.RectangleContainsScreenPoint(_primaryWindow.WindowRect, position, uiCamera))
            return true;

        if (_lootWindow && _lootWindow.IsVisible &&
            RectTransformUtility.RectangleContainsScreenPoint(_lootWindow.WindowRect, position, uiCamera))
            return true;

        return false;
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
        _lootWindow.SetHeaderTitle("Loot");
        _lootWindow.Initialize(runtime.Session, InventoryWindowMode.NearbyOnly, focus);
        ConfigureWindow(_lootWindow);
        _lootWindow.RefreshListOnly();
        _isLootOpen = true;
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
        runtime.BeginInventoryContext();
    }

    void TryEndInventoryContext()
    {
        if (IsAnyWindowOpen)
            return;

        _activeRuntime?.EndInventoryContext();
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

    void OnInventoryDataChanged()
    {
        if (_primaryWindow && _primaryWindow.IsVisible)
            _primaryWindow.OnStacksChanged();

        if (_lootWindow && _lootWindow.IsVisible)
            _lootWindow.OnStacksChanged();
    }

    void RefreshVisibleWindows()
    {
        OnSessionChanged();
        OnInventoryDataChanged();
    }
}
