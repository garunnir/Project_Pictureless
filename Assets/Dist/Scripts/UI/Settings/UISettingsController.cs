// ============================================================
// UISettingsController — ESC 세팅 토글 + pause_menu + UiMenu 입력
// ============================================================

using System;
using Sirenix.OdinInspector;
using UnityEngine;

public sealed class UISettingsController : MonoBehaviour, IUiCancelConsumer
{
    [SerializeField, Required] UISettingsWindow _windowPrefab;
    [SerializeField] UISettingsWindow _window;
    [SerializeField] Canvas _uiCanvas;
    [SerializeField] UICanvasLayerHost _layerHost;

    bool _isOpen;
    bool _pauseApplied;
    IDisposable _uiMenuScope;

    public int CancelPriority => UiCancelPriority.Settings;

    public bool IsOpen => _isOpen;

    void Awake()
    {
        EnsureReferences();
        EnsureWindow();
        if (_window != null)
            _window.gameObject.SetActive(false);
    }

    void OnEnable() => UiCancelRouter.Register(this);

    void OnDisable()
    {
        UiCancelRouter.Unregister(this);
        if (_isOpen)
            CloseInternal();
    }

    void OnDestroy()
    {
        if (_window != null)
            Destroy(_window.gameObject);
    }

    public bool TryHandleCancel()
    {
        if (_isOpen)
        {
            Close();
            return true;
        }

        Open();
        return true;
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
        _window.RefreshLabels();
        _window.ConfigureChrome(_uiCanvas);
        _window.BindClose(Close);
        _window.SetHudLayoutToggle(HudLayoutEdit.IsActive, notify: false);
        _window.SyncHudPopupToggles(notify: false);

        PushPause();
        _uiMenuScope = InputManager.Instance?.AcquireUiMenuInput(this);
        _isOpen = true;
    }

    public void Close()
    {
        if (!_isOpen)
            return;

        CloseInternal();
    }

    void CloseInternal()
    {
        HudLayoutEdit.SetActive(false);

        if (_window != null)
        {
            _window.SetHudLayoutToggle(false, notify: false);
            _window.gameObject.SetActive(false);
        }

        PopPause();
        _uiMenuScope?.Dispose();
        _uiMenuScope = null;
        _isOpen = false;
    }

    void PushPause()
    {
        if (_pauseApplied)
            return;

        TimeScaleService svc = TimeScaleService.Instance;
        if (svc == null)
            return;

        svc.Push(GameplayTimeScaleKeys.PauseMenu, TimeScaleChannel.World, 0f);
        svc.Push(GameplayTimeScaleKeys.PauseMenu, TimeScaleChannel.Player, 0f);
        _pauseApplied = true;
    }

    void PopPause()
    {
        if (!_pauseApplied)
            return;

        TimeScaleService svc = TimeScaleService.Instance;
        svc?.Pop(GameplayTimeScaleKeys.PauseMenu);
        _pauseApplied = false;
    }

    void EnsureReferences()
    {
        if (_uiCanvas == null)
            _uiCanvas = FindAnyObjectByType<Canvas>();
        if (_layerHost == null && _uiCanvas != null)
            _layerHost = _uiCanvas.GetComponent<UICanvasLayerHost>();
    }

    void EnsureWindow()
    {
        EnsureReferences();
        if (_window != null || _uiCanvas == null)
            return;

        if (_windowPrefab == null)
        {
            Debug.LogError("[UISettingsController] Window prefab is not assigned.", this);
            return;
        }

        Transform overlayRoot = _layerHost != null
            ? _layerHost.GetLayerRoot(UICanvasLayer.Overlay)
            : _uiCanvas.transform;

        _window = Instantiate(_windowPrefab, overlayRoot);
        _window.name = "Grp_SettingsWindow";
        _window.gameObject.SetActive(false);
    }
}
