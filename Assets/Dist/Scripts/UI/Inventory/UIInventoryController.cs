// ============================================================
// UIInventoryController — 인벤 UI + [B] PlayerAction 억제 + 스크롤 드래그 overlay
// ============================================================
// [A] UiMenu 입력은 사용하지 않음 (건설 UI 전용).
// 창 위 마우스 구간: Zoom/Aim만 SuppressPlayerAction. Move(WASD)는 유지.

using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public sealed class UIInventoryController : MonoBehaviour, IInventoryScrollDragHost
{
    [Required, SerializeField] UIInventoryListWindow _windowPrefab;
    [SerializeField] UIInventoryListWindow _primaryWindow;
    [SerializeField] UIInventoryListWindow _lootWindow;
    [SerializeField] Canvas _uiCanvas;
    [SerializeField] Vector2 _lootWindowOffset = new(380f, 0f);

    PlayerInventoryRuntime _activeRuntime;
    GameObject _scrollDragOverlay;
    int _scrollDragDepth;
    bool _isOpen;

    void Awake()
    {
        EnsureReferences();
        EnsureWindows();
        EnsureScrollDragOverlay();
        WireScrollDragHandler(_primaryWindow);
        WireScrollDragHandler(_lootWindow);

        if (_primaryWindow != null)
            _primaryWindow.gameObject.SetActive(false);
        if (_lootWindow != null)
            _lootWindow.gameObject.SetActive(false);
    }

    void OnEnable() => PlayerInventoryRuntime.ActiveChanged += OnActivePlayerChanged;

    void OnDisable()
    {
        PlayerInventoryRuntime.ActiveChanged -= OnActivePlayerChanged;
        ClearMouseActionSuppressions();
        CloseInventory();
    }

    void OnDestroy() => CloseInventory();

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.iKey.wasPressedThisFrame)
            ToggleInventory();
    }

    void LateUpdate()
    {
        if (!_isOpen)
            return;

        bool suppressMouseActions = _scrollDragDepth > 0 || IsPointerOverAnyVisibleWindow();
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

    void EnsureReferences()
    {
        if (!_uiCanvas) _uiCanvas = FindAnyObjectByType<Canvas>();
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

        if (_primaryWindow == null)
        {
            _primaryWindow = Instantiate(_windowPrefab, _uiCanvas.transform);
            _primaryWindow.name = "Grp_InventoryListWindow_Primary";
        }

        if (_lootWindow == null)
        {
            _lootWindow = Instantiate(_windowPrefab, _uiCanvas.transform);
            _lootWindow.name = "Grp_InventoryListWindow_Loot";
            _lootWindow.GetComponent<RectTransform>().anchoredPosition = _lootWindowOffset;
        }
    }

    void EnsureScrollDragOverlay()
    {
        if (_scrollDragOverlay != null || _uiCanvas == null)
            return;

        _scrollDragOverlay = new GameObject(
            "InventoryScrollDragOverlay",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));

        _scrollDragOverlay.transform.SetParent(_uiCanvas.transform, false);
        _scrollDragOverlay.transform.SetAsLastSibling();

        var rect = _scrollDragOverlay.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var image = _scrollDragOverlay.GetComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0f);
        image.raycastTarget = true;
        _scrollDragOverlay.SetActive(false);
    }

    void WireScrollDragHandler(UIInventoryListWindow window)
    {
        if (window == null)
            return;

        InventoryScrollDragHandler handler = window.GetComponentInChildren<InventoryScrollDragHandler>(true);
        if (handler == null)
        {
            ScrollRect scroll = window.GetComponentInChildren<ScrollRect>();
            if (scroll?.viewport != null)
                handler = scroll.viewport.gameObject.AddComponent<InventoryScrollDragHandler>();
        }

        handler?.Bind(this);
    }

    public void ToggleInventory()
    {
        if (_isOpen)
            CloseInventory();
        else
            OpenInventory();
    }

    public void OpenInventory()
    {
        if (_isOpen)
            return;

        PlayerInventoryRuntime runtime = PlayerInventoryRuntime.Active;
        if (runtime == null)
        {
            Debug.LogWarning("[UIInventoryController] No active player inventory runtime.", this);
            return;
        }

        BindRuntime(runtime);
        runtime.BeginInventoryContext();

        _primaryWindow.gameObject.SetActive(true);
        _primaryWindow.Initialize(runtime.Session, runtime.Host.Container);
        _isOpen = true;
    }

    public void CloseInventory()
    {
        if (!_isOpen)
        {
            UnbindRuntime();
            return;
        }

        _scrollDragDepth = 0;
        if (_scrollDragOverlay != null)
            _scrollDragOverlay.SetActive(false);

        _activeRuntime?.EndInventoryContext();

        _primaryWindow.gameObject.SetActive(false);
        if (_lootWindow != null)
            _lootWindow.gameObject.SetActive(false);

        _isOpen = false;
        ClearMouseActionSuppressions();
        UnbindRuntime();
    }

    void ClearMouseActionSuppressions()
    {
        InputManager input = InputManager.Instance;
        if (input == null)
            return;

        input.SuppressPlayerAction(PlayerAction.Zoom, this, false);
        input.SuppressPlayerAction(PlayerAction.Aim, this, false);
    }

    bool IsPointerOverAnyVisibleWindow()
    {
        if (Mouse.current == null)
            return false;

        Vector2 position = Mouse.current.position.ReadValue();
        Camera uiCamera = GetCanvasCamera();

        if (_primaryWindow != null && _primaryWindow.IsVisible &&
            RectTransformUtility.RectangleContainsScreenPoint(_primaryWindow.WindowRect, position, uiCamera))
            return true;

        if (_lootWindow != null && _lootWindow.IsVisible &&
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
        if (focusContainer == null)
            return;

        PlayerInventoryRuntime runtime = PlayerInventoryRuntime.Active;
        if (runtime == null)
        {
            Debug.LogWarning("[UIInventoryController] No active player inventory runtime.", this);
            return;
        }

        if (!_isOpen)
            OpenInventory();

        runtime.TryAddSidebarContainer(focusContainer);
        runtime.SeedContainerIfEmpty(focusContainer);

        if (_lootWindow != null)
        {
            _lootWindow.gameObject.SetActive(true);
            _lootWindow.Initialize(runtime.Session, focusContainer);
        }
        else
        {
            _primaryWindow.SelectContainer(focusContainer);
        }
    }

    void OnActivePlayerChanged(PlayerInventoryRuntime runtime)
    {
        if (_isOpen)
            CloseInventory();
        else
            UnbindRuntime();
    }

    void BindRuntime(PlayerInventoryRuntime runtime)
    {
        if (_activeRuntime == runtime)
            return;

        UnbindRuntime();
        _activeRuntime = runtime;

        if (_activeRuntime?.Session != null)
            _activeRuntime.Session.SidebarChanged += OnSessionChanged;
    }

    void UnbindRuntime()
    {
        if (_activeRuntime?.Session != null)
            _activeRuntime.Session.SidebarChanged -= OnSessionChanged;

        _activeRuntime = null;
    }

    void OnSessionChanged()
    {
        if (_primaryWindow != null && _primaryWindow.IsVisible)
            _primaryWindow.OnSessionChanged();

        if (_lootWindow != null && _lootWindow.IsVisible)
            _lootWindow.OnSessionChanged();
    }
}
