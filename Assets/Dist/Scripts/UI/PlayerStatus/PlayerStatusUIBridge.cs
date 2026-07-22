// ============================================================
// PlayerStatusUIBridge — PlayerStatusViewModel 씬 수명주기 + GameplayData bind
// ============================================================

using UnityEngine;

[DefaultExecutionOrder(-50)]
public sealed class PlayerStatusUIBridge : MonoBehaviour
{
    PlayerStatusViewModel _viewModel;

    public PlayerStatusViewModel ViewModel
    {
        get
        {
            EnsureInitialized();
            return _viewModel;
        }
    }

    void Awake() => EnsureInitialized();

    void OnDestroy()
    {
        _viewModel?.Unbind();
        _viewModel = null;
    }

    void EnsureInitialized()
    {
        if (_viewModel != null)
            return;

        _viewModel = new PlayerStatusViewModel();
        _viewModel.Bind(GameplayData.Body, GameplayData.Vitals, GameplayData.Stats);
    }

    public static bool TryResolve(out PlayerStatusViewModel viewModel)
    {
        viewModel = null;
        PlayerStatusUIBridge bridge = FindAnyObjectByType<PlayerStatusUIBridge>();
        if (bridge == null)
            return false;

        viewModel = bridge.ViewModel;
        return viewModel != null;
    }
}
