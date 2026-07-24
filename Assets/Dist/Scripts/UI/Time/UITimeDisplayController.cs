// ============================================================
// UITimeDisplayController — HUD 시계 패널 바인드·갱신
// ============================================================

using UnityEngine;

public sealed class UITimeDisplayController : MonoBehaviour
{
    [SerializeField] UITimeDisplayPanel _panel;
    [SerializeField] Canvas _uiCanvas;

    [Header("Window Chrome")]
    [Tooltip("켜면 창 근처 시 Area_Header가 나타나고 드래그로 이동합니다.")]
    [SerializeField] bool _enableDragHeader = true;

    [Tooltip("켜면 가장자리 근접 시 리사이즈 핸들이 나타납니다.")]
    [SerializeField] bool _enableResize = true;

    TimeViewModel _viewModel;

    void Awake()
    {
        if (_panel == null)
        {
            Debug.LogError(
                "[UITimeDisplayController] _panel is not assigned. " +
                "Run Dist/Time/Setup Canvas In Open Scene to place HUD in the scene.",
                this);
            return;
        }

        if (!TimeUIBridge.TryResolve(out _viewModel))
        {
            Debug.LogError(
                "[UITimeDisplayController] TimeUIBridge not found in scene.",
                this);
            return;
        }

        if (_uiCanvas == null)
            _uiCanvas = FindAnyObjectByType<Canvas>();

        _panel.BindViewModel(_viewModel);
        _panel.ConfigureWindowChrome(_uiCanvas, _enableDragHeader, _enableResize);

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
}
