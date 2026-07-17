// ============================================================
// UIPlayerStatusController — 상태창 토글 + Layer_Window 스폰
// ============================================================

using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class UIPlayerStatusController : MonoBehaviour
{
    [SerializeField] UIPlayerStatusWindow _windowPrefab;
    [SerializeField] UIPlayerStatusWindow _window;
    [SerializeField] Canvas _uiCanvas;
    [SerializeField] UICanvasLayerHost _layerHost;
    [SerializeField] PlayerStatusWindowLauncher _launcher;
    [SerializeField] Vector2 _windowInitialPosition = new(220f, 40f);

    bool _isOpen;

    void Awake()
    {
        EnsureReferences();
        EnsureWindow();
        if (_window != null)
            _window.gameObject.SetActive(false);
        SyncLauncher();
    }

    void Start()
    {
        if (InputManager.Instance != null)
            InputManager.Instance.PlayerStatusTogglePerformed += OnStatusTogglePerformed;
    }

    void OnDestroy()
    {
        if (InputManager.Instance != null)
            InputManager.Instance.PlayerStatusTogglePerformed -= OnStatusTogglePerformed;

        if (_window != null)
            _window.Unbind();
    }

    void OnStatusTogglePerformed(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        Toggle();
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

        _window.gameObject.SetActive(true);
        _window.ConfigureChrome(_uiCanvas);
        _window.Initialize(GameplayData.Body, GameplayData.Vitals, GameplayData.Stats);
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

        _isOpen = false;
        SyncLauncher();
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

        if (_windowPrefab != null)
            _window = Instantiate(_windowPrefab, windowRoot);
        else
            _window = PlayerStatusUIFactory.CreateWindowRoot();

        if (_window.transform.parent != windowRoot)
            _window.transform.SetParent(windowRoot, false);

        _window.name = "Grp_PlayerStatusWindow";
        if (_window.WindowRect != null)
            _window.WindowRect.anchoredPosition = _windowInitialPosition;
    }

    public void BindLauncher(PlayerStatusWindowLauncher launcher) => _launcher = launcher;
}
