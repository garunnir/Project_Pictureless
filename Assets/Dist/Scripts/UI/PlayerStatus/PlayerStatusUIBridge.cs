// ============================================================
// PlayerStatusUIBridge — PlayerStatusViewModel 씬 수명주기 + GameplayData bind (Possess 시 rebind)
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

    void Awake() => BindFromGameplayData();

    void OnDestroy()
    {
        _viewModel?.Unbind();
        _viewModel = null;
    }

    void EnsureInitialized()
    {
        if (_viewModel != null)
            return;

        BindFromGameplayData();
    }

    void BindFromGameplayData()
    {
        if (_viewModel == null)
            _viewModel = new PlayerStatusViewModel();

        _viewModel.Bind(GameplayData.Body, GameplayData.Vitals, GameplayData.Stats);
    }

    public static void RebindFromGameplayData()
    {
        PlayerStatusUIBridge bridge = FindAnyObjectByType<PlayerStatusUIBridge>();
        if (bridge == null)
            return;

        bridge.BindFromGameplayData();
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
