// ============================================================
// UIMessageLogController — 메시지 로그 HUD 바인드·갱신
// ============================================================

using UnityEngine;

public sealed class UIMessageLogController : MonoBehaviour
{
    [SerializeField] UIMessageLogPanel _panel;

    MessageLogViewModel _viewModel;

    void Awake()
    {
        if (_panel == null)
        {
            Debug.LogError("[UIMessageLogController] _panel is not assigned.", this);
            return;
        }

        if (!MessageLogUIBridge.TryResolve(out _viewModel))
        {
            Debug.LogError(
                "[UIMessageLogController] MessageLogUIBridge not found in scene.",
                this);
            return;
        }

        _panel.BindViewModel(_viewModel);
        UIWindowChromeBar.BindCloseOnWindow(_panel, _panel.Hide);
        if (_panel != null)
            _panel.RefreshHeaderTitle();
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
