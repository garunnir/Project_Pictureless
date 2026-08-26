// ============================================================
// UIPlayerStatusSummaryController — HUD 상태 요약 패널 바인드·갱신
// ============================================================

using UnityEngine;

public sealed class UIPlayerStatusSummaryController : MonoBehaviour
{
    [SerializeField] PlayerStatusUIBridge _bridge;
    [SerializeField] UIPlayerStatusSummaryPanel _panel;
    [SerializeField] UICharacterController _characterController;

    PlayerStatusViewModel _viewModel;
    bool _syncingTab;

    void Awake()
    {
        if (_bridge == null)
            _bridge = PlayerStatusUIBridge.Instance;

        if (_panel == null || _bridge == null)
        {
            Debug.LogError(
                "[UIPlayerStatusSummaryController] Bridge/panel missing — HUD disabled. " +
                "Run Dist/MCP/PlayerStatus/Setup Canvas In Open Scene.",
                this);
            enabled = false;
            if (_panel != null)
                _panel.gameObject.SetActive(false);
            return;
        }

        _viewModel = _bridge.ViewModel;
        if (_viewModel == null)
        {
            Debug.LogError(
                "[UIPlayerStatusSummaryController] ViewModel null — HUD disabled.",
                this);
            enabled = false;
            _panel.gameObject.SetActive(false);
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
