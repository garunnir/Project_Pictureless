// ============================================================
// UIPlayerStatusSummaryController — HUD 상태 요약 패널 바인드·갱신
// ============================================================

using UnityEngine;

public sealed class UIPlayerStatusSummaryController : MonoBehaviour
{
    [SerializeField] UIPlayerStatusSummaryPanel _panel;

    PlayerStatusViewModel _viewModel;

    void Awake()
    {
        if (_panel == null)
        {
            Debug.LogError(
                "[UIPlayerStatusSummaryController] _panel is not assigned. " +
                "Run Dist/MCP/PlayerStatus/Setup Canvas In Open Scene to place HUD in the scene.",
                this);
            return;
        }

        if (!PlayerStatusUIBridge.TryResolve(out _viewModel))
        {
            Debug.LogError(
                "[UIPlayerStatusSummaryController] PlayerStatusUIBridge not found in scene.",
                this);
            return;
        }

        _panel.BindViewModel(_viewModel);
        UIWindowChromeBar.BindCloseOnWindow(_panel, HidePanel);
        _viewModel.MoodChanged += OnMoodChanged;
        Refresh();
    }

    void OnDestroy()
    {
        if (_viewModel != null)
            _viewModel.MoodChanged -= OnMoodChanged;
    }

    void OnMoodChanged() => Refresh();

    void Refresh()
    {
        if (_panel != null)
            _panel.Refresh();
    }

    void HidePanel()
    {
        if (_panel != null)
            _panel.gameObject.SetActive(false);
    }
}
