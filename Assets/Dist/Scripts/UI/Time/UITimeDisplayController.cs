// ============================================================
// UITimeDisplayController — HUD 시계 패널 바인드·갱신
// ============================================================

using UnityEngine;

public sealed class UITimeDisplayController : MonoBehaviour
{
    [SerializeField] UITimeDisplayPanel _panel;
    [SerializeField] Canvas _uiCanvas;

    [Header("Window Chrome")]
    [Tooltip("HUD 조정 ON일 때만 HudLayoutParticipant가 드래그·리사이즈를 켭니다. 평소 헤더는 표시하지 않습니다.")]
    [SerializeField] bool _enableDragHeader;

    [Tooltip("HUD 조정 ON일 때만 리사이즈 핸들을 켭니다.")]
    [SerializeField] bool _enableResize;

    TimeViewModel _viewModel;

    void Awake()
    {
        if (_panel == null)
        {
            Debug.LogError(
                "[UITimeDisplayController] _panel is not assigned. " +
                "Run Dist/MCP/Time/Setup Canvas In Open Scene to place HUD in the scene.",
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
