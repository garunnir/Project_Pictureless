// ============================================================
// UICharacterController — Character 창 토글 (StatusToggle=Tab) + Layer_Window
// ============================================================

using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public sealed class UICharacterController : MonoBehaviour
{
    [FormerlySerializedAs("_windowPrefab")]
    [SerializeField, Required] UICharacterWindow _windowPrefab;
    [FormerlySerializedAs("_window")]
    [SerializeField] UICharacterWindow _window;
    [SerializeField] Canvas _uiCanvas;
    [SerializeField] UICanvasLayerHost _layerHost;
    [SerializeField] PlayerStatusWindowLauncher _launcher;
    [SerializeField] UIPlayerStatusSummaryPanel _summaryPanel;
    [SerializeField] PlayerStatusUIBridge _bridge;
    [SerializeField] Vector2 _windowInitialPosition = new(220f, 40f);

    bool _isOpen;
    PlayerStatusViewModel _viewModel;

    public bool IsOpen => _isOpen;
    public event Action<CharacterWindowTab> TabChanged;

    void Awake()
    {
        EnsureReferences();
        EnsureWindow();
        if (_window != null)
            _window.gameObject.SetActive(false);
        SyncLauncher();

        if (_bridge == null)
            _bridge = PlayerStatusUIBridge.Instance;

        if (_bridge == null)
        {
            Debug.LogError(
                "[UICharacterController] PlayerStatusUIBridge missing — status window disabled. " +
                "Run Dist/MCP/PlayerStatus/Setup Canvas In Open Scene.",
                this);
            enabled = false;
            return;
        }

        _viewModel = _bridge.ViewModel;
        if (_viewModel == null)
        {
            Debug.LogError(
                "[UICharacterController] ViewModel null — status window disabled.",
                this);
            enabled = false;
        }
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
        {
            _window.TabChanged -= OnWindowTabChanged;
            _window.Unbind();
        }
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
        if (_window == null || _viewModel == null)
            return;

        if (_summaryPanel == null)
            _summaryPanel = UnityEngine.Object.FindAnyObjectByType<UIPlayerStatusSummaryPanel>();

        CharacterWindowTab tab = _summaryPanel != null
            ? _summaryPanel.ActiveTab
            : CharacterWindowTab.Status;

        _window.gameObject.SetActive(true);
        _window.BindChromeClose(Close);
        _window.ConfigureChrome(_uiCanvas);
        _window.TabChanged -= OnWindowTabChanged;
        _window.TabChanged += OnWindowTabChanged;
        _window.Initialize(_viewModel, tab);
        _isOpen = true;
        SyncLauncher();
    }

    public void Close()
    {
        if (!_isOpen)
            return;

        if (_window != null)
        {
            _window.TabChanged -= OnWindowTabChanged;
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

        if (_windowPrefab == null)
        {
            Debug.LogError(
                "[UICharacterController] Window prefab is not assigned.",
                this);
            return;
        }

        _window = Instantiate(_windowPrefab, windowRoot);

        if (_window.transform.parent != windowRoot)
            _window.transform.SetParent(windowRoot, false);

        if (_window.WindowRect != null)
            _window.WindowRect.anchoredPosition = _windowInitialPosition;
    }

    public void BindLauncher(PlayerStatusWindowLauncher launcher) => _launcher = launcher;

    public void SetTab(CharacterWindowTab tab)
    {
        if (!_isOpen || _window == null)
            return;

        _window.SetTab(tab);
    }

    void OnWindowTabChanged(CharacterWindowTab tab) => TabChanged?.Invoke(tab);
}
