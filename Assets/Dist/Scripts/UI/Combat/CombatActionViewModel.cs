// ============================================================
// CombatActionViewModel — CharacterAttacker 구독 + HUD 스냅샷
// ============================================================

using System;

public sealed class CombatActionViewModel
{
    CharacterAttacker _attacker;

    public event Action Changed;

    public WeaponAction SelectedAction { get; private set; }
    public WeaponActionMask AvailableActions { get; private set; }
    public string WeaponName { get; private set; } = string.Empty;

    public string DisplayText =>
        CombatActionDisplayFormat.Format(SelectedAction, AvailableActions, WeaponName);

    public void Bind(CharacterAttacker attacker)
    {
        Unbind();
        _attacker = attacker;
        if (_attacker != null)
        {
            _attacker.AvailableActionsChanged += OnAttackerChanged;
            _attacker.SelectedActionChanged += OnAttackerChanged;
        }

        Snapshot();
        Changed?.Invoke();
    }

    public void Unbind()
    {
        if (_attacker != null)
        {
            _attacker.AvailableActionsChanged -= OnAttackerChanged;
            _attacker.SelectedActionChanged -= OnAttackerChanged;
        }

        _attacker = null;
    }

    void OnAttackerChanged()
    {
        Snapshot();
        Changed?.Invoke();
    }

    void Snapshot()
    {
        if (_attacker == null)
        {
            SelectedAction = WeaponAction.Bashing;
            AvailableActions = WeaponActionMask.None;
            WeaponName = string.Empty;
            return;
        }

        SelectedAction = _attacker.SelectedAction;
        AvailableActions = _attacker.AvailableActions;
        WeaponName = !string.IsNullOrEmpty(_attacker.ItemId)
            ? _attacker.ItemId
            : (_attacker.Presentation != null ? _attacker.Presentation.name : string.Empty);
    }
}
