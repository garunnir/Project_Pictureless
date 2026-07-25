// ============================================================
// CombatActionUIBridge — CombatActionViewModel 씬 수명 + 플레이어 Attacker 바인드
// ============================================================

using UnityEngine;

[DefaultExecutionOrder(-50)]
public sealed class CombatActionUIBridge : MonoBehaviour
{
    CombatActionViewModel _viewModel;

    public CombatActionViewModel ViewModel
    {
        get
        {
            EnsureInitialized();
            return _viewModel;
        }
    }

    void Awake() => EnsureInitialized();

    void Start()
    {
        EnsureInitialized();
        RebindAttackerIfNeeded();
    }

    void OnDestroy()
    {
        _viewModel?.Unbind();
        _viewModel = null;
    }

    void EnsureInitialized()
    {
        if (_viewModel != null)
            return;

        _viewModel = new CombatActionViewModel();
        _viewModel.Bind(ResolvePlayerAttacker());
    }

    void RebindAttackerIfNeeded()
    {
        if (_viewModel == null)
            return;

        CharacterAttacker attacker = ResolvePlayerAttacker();
        if (attacker == null)
            return;

        _viewModel.Bind(attacker);
    }

    static CharacterAttacker ResolvePlayerAttacker()
    {
        PlayerCombatController player = FindAnyObjectByType<PlayerCombatController>();
        return player != null ? player.GetComponent<CharacterAttacker>() : null;
    }

    public static bool TryResolve(out CombatActionViewModel viewModel)
    {
        viewModel = null;
        CombatActionUIBridge bridge = FindAnyObjectByType<CombatActionUIBridge>();
        if (bridge == null)
            return false;

        viewModel = bridge.ViewModel;
        return viewModel != null;
    }
}
