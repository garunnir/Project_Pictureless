// ============================================================
// UITimeDisplayController — HUD 시계 패널 바인드·갱신
// ============================================================

using UnityEngine;

public sealed class UITimeDisplayController : MonoBehaviour
{
    [SerializeField] UITimeDisplayPanel _panel;
    [SerializeField] UICanvasLayerHost _layerHost;
    [SerializeField] UITimeDisplayPanel _panelPrefab;
    [SerializeField] Canvas _uiCanvas;

    [Header("Window Chrome")]
    [Tooltip("켜면 창 근처 시 Area_Header가 나타나고 드래그로 이동합니다.")]
    [SerializeField] bool _enableDragHeader = true;

    [Tooltip("켜면 가장자리 근접 시 리사이즈 핸들이 나타납니다.")]
    [SerializeField] bool _enableResize = true;

    TimeViewModel _viewModel;

    void Awake()
    {
        EnsurePanel();
        if (!TimeUIBridge.TryResolve(out _viewModel))
        {
            Debug.LogError(
                "[UITimeDisplayController] TimeUIBridge not found in scene.",
                this);
            return;
        }

        if (_panel != null)
        {
            _panel.BindViewModel(_viewModel);
            EnsureReferences();
            _panel.ConfigureWindowChrome(_uiCanvas, _enableDragHeader, _enableResize);
        }

        _viewModel.Changed += OnChanged;
        Refresh();
    }

    void OnDestroy()
    {
        if (_viewModel != null)
            _viewModel.Changed -= OnChanged;
    }

    void OnChanged() => Refresh();

    void Refresh()
    {
        if (_panel != null)
            _panel.Refresh();
    }

    void EnsurePanel()
    {
        if (_panel != null)
            return;

        EnsureReferences();
        if (_panelPrefab == null || _uiCanvas == null)
            return;

        Transform hudRoot = _layerHost != null
            ? _layerHost.GetLayerRoot(UICanvasLayer.HUD)
            : _uiCanvas.transform;

        _panel = Instantiate(_panelPrefab, hudRoot);
        _panel.name = "Grp_TimeDisplay";
    }

    void EnsureReferences()
    {
        if (_uiCanvas == null)
            _uiCanvas = FindAnyObjectByType<Canvas>();
        if (_layerHost == null && _uiCanvas != null)
            _layerHost = _uiCanvas.GetComponent<UICanvasLayerHost>();
    }

    public void SetPanelPrefab(UITimeDisplayPanel prefab) => _panelPrefab = prefab;
}
