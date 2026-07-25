// ============================================================
// UICombatActionController — 전투 액션 HUD 바인드·갱신
// ============================================================

using UnityEngine;

public sealed class UICombatActionController : MonoBehaviour
{
    [SerializeField] UICombatActionPanel _panel;

    CombatActionViewModel _viewModel;

    void Awake()
    {
        if (_panel == null)
        {
            Debug.LogError(
                "[UICombatActionController] _panel is not assigned.",
                this);
            return;
        }

        if (!CombatActionUIBridge.TryResolve(out _viewModel))
        {
            Debug.LogError(
                "[UICombatActionController] CombatActionUIBridge not found in scene.",
                this);
            return;
        }

        _panel.BindViewModel(_viewModel);
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
