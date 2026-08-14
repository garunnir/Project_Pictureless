// ============================================================
// UICraftingController — 제작 창 토글 (런처만, 핫키 없음)
// ============================================================

using Sirenix.OdinInspector;
using UnityEngine;

public sealed class UICraftingController : MonoBehaviour
{
    [SerializeField, Required] UICraftingWindow _windowPrefab;
    [SerializeField] UICraftingWindow _window;
    [SerializeField] Canvas _uiCanvas;
    [SerializeField] UICanvasLayerHost _layerHost;
    [SerializeField] CraftingWindowLauncher _launcher;
    [SerializeField] Vector2 _windowInitialPosition = new(40f, 40f);

    bool _isOpen;
    PlayerInventoryRuntime _boundRuntime;

    public bool IsOpen => _isOpen;

    void Awake()
    {
        EnsureReferences();
        EnsureWindow();
        if (_window != null)
            _window.gameObject.SetActive(false);
        SyncLauncher();
    }

    void OnEnable()
    {
        PlayerInventoryRuntime.ActiveChanged += OnActivePlayerChanged;
    }

    void OnDisable()
    {
        PlayerInventoryRuntime.ActiveChanged -= OnActivePlayerChanged;
        if (_isOpen)
            Close();
    }

    void OnDestroy()
    {
        PlayerInventoryRuntime.ActiveChanged -= OnActivePlayerChanged;
        if (_window != null)
            _window.Unbind();
    }

    public void Toggle()
    {
        if (_isOpen)
            Close();
        else
            Open();
    }

    public void Open()
    {
        if (_isOpen)
            return;

        EnsureWindow();
        if (_window == null)
            return;

        PlayerInventoryRuntime runtime = PlayerInventoryRuntime.Active;
        if (runtime == null)
        {
            Debug.LogError("[UICraftingController] PlayerInventoryRuntime.Active missing.", this);
            return;
        }

        BindRuntime(runtime);
        runtime.AcquireContext(this);

        _window.gameObject.SetActive(true);
        _window.ConfigureChrome(_uiCanvas);
        _window.Initialize(runtime, Close);
        _isOpen = true;
        SyncLauncher();
    }

    public void Close()
    {
        if (!_isOpen)
            return;

        if (_window != null)
        {
            _window.Unbind();
            _window.gameObject.SetActive(false);
        }

        _boundRuntime?.ReleaseContext(this);
        UnbindRuntime();
        _isOpen = false;
        SyncLauncher();
    }

    public void BindLauncher(CraftingWindowLauncher launcher) => _launcher = launcher;

    void OnActivePlayerChanged(PlayerInventoryRuntime runtime)
    {
        if (!_isOpen)
            return;

        if (runtime == null || runtime != _boundRuntime)
            Close();
    }

    void BindRuntime(PlayerInventoryRuntime runtime)
    {
        if (_boundRuntime == runtime)
            return;

        UnbindRuntime();
        _boundRuntime = runtime;

        if (_boundRuntime?.Session != null)
        {
            _boundRuntime.Session.SidebarChanged += OnSessionChanged;
            _boundRuntime.Session.StacksChanged += OnSessionChanged;
        }
    }

    void UnbindRuntime()
    {
        if (_boundRuntime?.Session != null)
        {
            _boundRuntime.Session.SidebarChanged -= OnSessionChanged;
            _boundRuntime.Session.StacksChanged -= OnSessionChanged;
        }

        _boundRuntime = null;
    }

    void OnSessionChanged()
    {
        if (_isOpen && _window != null)
            _window.Refresh();
    }

    void OnSessionChanged(InventoryStacksChangeSet _)
    {
        OnSessionChanged();
    }

    void SyncLauncher()
    {
        if (_launcher != null)
            _launcher.SetOpen(_isOpen);
    }

    void EnsureReferences()
    {
        if (!_uiCanvas)
            _uiCanvas = FindAnyObjectByType<Canvas>();
        if (!_layerHost && _uiCanvas)
            _layerHost = _uiCanvas.GetComponent<UICanvasLayerHost>();
    }

    void EnsureWindow()
    {
        EnsureReferences();
        if (_window != null || _uiCanvas == null)
            return;

        Transform windowRoot = _layerHost != null
            ? _layerHost.GetLayerRoot(UICanvasLayer.Window)
            : _uiCanvas.transform;

        if (_windowPrefab == null)
        {
            Debug.LogError("[UICraftingController] Window prefab is not assigned.", this);
            return;
        }

        _window = Instantiate(_windowPrefab, windowRoot);

        if (_window.transform.parent != windowRoot)
            _window.transform.SetParent(windowRoot, false);

        _window.name = "Grp_CraftingWindow";
        if (_window.WindowRect != null)
            _window.WindowRect.anchoredPosition = _windowInitialPosition;
    }
}
