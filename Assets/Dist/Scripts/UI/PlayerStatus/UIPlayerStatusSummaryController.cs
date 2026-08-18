// ============================================================
// UIPlayerStatusSummaryController — HUD 상태 요약 패널 바인드·갱신
// ============================================================

using UnityEngine;

public sealed class UIPlayerStatusSummaryController : MonoBehaviour
{
    [SerializeField] UIPlayerStatusSummaryPanel _panel;
    [SerializeField] UICharacterController _characterController;

    PlayerStatusViewModel _viewModel;
    bool _syncingTab;

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

        if (_characterController == null)
            _characterController = FindAnyObjectByType<UICharacterController>();

        _panel.BindViewModel(_viewModel);
        UIWindowChromeBar.BindCloseOnWindow(_panel, HidePanel);
        _panel.BodyTabChanged += OnHudTabChanged;
        if (_characterController != null)
            _characterController.TabChanged += OnWindowTabChanged;

        _viewModel.MoodChanged += OnMoodChanged;
        _viewModel.Changed += OnChanged;
        Refresh();
        _panel.RefreshBody();
    }

    void OnDestroy()
    {
        if (_panel != null)
            _panel.BodyTabChanged -= OnHudTabChanged;
        if (_characterController != null)
            _characterController.TabChanged -= OnWindowTabChanged;
        if (_viewModel != null)
        {
            _viewModel.MoodChanged -= OnMoodChanged;
            _viewModel.Changed -= OnChanged;
        }
    }

    void OnMoodChanged() => Refresh();

    void OnChanged()
    {
        if (_panel != null)
            _panel.RefreshBody();
    }

    void OnHudTabChanged(CharacterWindowTab tab)
    {
        if (_syncingTab)
            return;

        _syncingTab = true;
        _characterController?.SetTab(tab);
        _syncingTab = false;
    }

    void OnWindowTabChanged(CharacterWindowTab tab)
    {
        if (_syncingTab)
            return;

        _syncingTab = true;
        _panel?.SetBodyTab(tab);
        _syncingTab = false;
    }

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
