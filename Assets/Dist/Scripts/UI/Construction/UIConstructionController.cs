// ============================================================
// UIConstructionController — 본편 건설 창 토글 (런처)
// ============================================================

using Garunnir.Runtime.Gameplay.Data;
using UnityEngine;

public sealed class UIConstructionController : MonoBehaviour
{
    [SerializeField] UIConstructionWindow _windowPrefab;
    [SerializeField] UIConstructionWindow _window;
    [SerializeField] Canvas _uiCanvas;
    [SerializeField] UICanvasLayerHost _layerHost;
    [SerializeField] ConstructionWindowLauncher _launcher;

    bool _isOpen;
    PlayerInventoryRuntime _boundRuntime;

    public static bool IsGameplayOpen
    {
        get
        {
            UIConstructionController c =
                FindFirstObjectByType<UIConstructionController>(FindObjectsInactive.Include);
            return c != null && c._isOpen;
        }
    }

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
        CharacterMoodHost.AnyControlYielded += Close;
    }

    void OnDisable()
    {
        PlayerInventoryRuntime.ActiveChanged -= OnActivePlayerChanged;
        CharacterMoodHost.AnyControlYielded -= Close;
        if (_isOpen)
            Close();
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
        if (MoodGameplayGate.IsBlocked)
            return;

        if (FarmCellTargetSession.IsActive ||
            FishCellTargetSession.IsActive ||
            ConstructionCellTargetSession.IsActive ||
            UIConstruction.IsOpen)
            return;

        EnsureWindow();
        if (_window == null)
            return;

        PlayerInventoryRuntime runtime = PlayerInventoryRuntime.Active;
        if (runtime == null)
            return;

        BindRuntime(runtime);
        _window.gameObject.SetActive(true);
        _window.Refresh();
        _isOpen = true;
        SyncLauncher();
    }

    public void Close()
    {
        if (!_isOpen)
            return;

        if (_window != null)
            _window.gameObject.SetActive(false);

        UnbindRuntime();
        _isOpen = false;
        SyncLauncher();
    }

    void EnsureReferences()
    {
        if (_uiCanvas == null)
            _uiCanvas = FindFirstObjectByType<Canvas>();
        if (_layerHost == null && _uiCanvas != null)
            _uiCanvas.TryGetComponent(out _layerHost);
    }

    void EnsureWindow()
    {
        if (_window != null)
            return;

        if (_windowPrefab == null)
        {
            Debug.LogError("[UIConstructionController] Window prefab missing.");
            return;
        }

        Transform parent = _layerHost != null
            ? _layerHost.GetLayerRoot(UICanvasLayer.Window)
            : _uiCanvas != null ? _uiCanvas.transform : transform;

        _window = Instantiate(_windowPrefab, parent);
        _window.name = "Wnd_Construction";
        _window.CloseRequested += Close;
        _window.BuildRequested += OnBuildRequested;
    }

    void OnBuildRequested(ConstructionData data)
    {
        Close();
        ConstructionCellTargetSession.TryBegin(data);
    }

    void BindRuntime(PlayerInventoryRuntime runtime)
    {
        if (_boundRuntime == runtime)
            return;

        UnbindRuntime();
        _boundRuntime = runtime;
        _boundRuntime?.AcquireContext(this);
    }

    void UnbindRuntime()
    {
        if (_boundRuntime == null)
            return;

        _boundRuntime.ReleaseContext(this);
        _boundRuntime = null;
    }

    void OnActivePlayerChanged(PlayerInventoryRuntime _)
    {
        if (_isOpen)
            Close();
    }

    void SyncLauncher() => _launcher?.SetOpen(_isOpen);

    public void BindLauncher(ConstructionWindowLauncher launcher) => _launcher = launcher;
}
