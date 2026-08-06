// ============================================================
// UICombatActionPanel — 전투 액션 HUD 읽기전용 텍스트
// ============================================================

using TMPro;
using UnityEngine;

public sealed class UICombatActionPanel : MonoBehaviour
{
    [SerializeField] TMP_Text _actionText;

    CombatActionViewModel _viewModel;

    public void Wire(TMP_Text actionText) => _actionText = actionText;

    public void BindViewModel(CombatActionViewModel viewModel) => _viewModel = viewModel;

    public void Refresh()
    {
        if (_actionText == null)
            return;

        _actionText.text = _viewModel != null
            ? _viewModel.DisplayText
            : CombatActionDisplayFormat.Format(
                WeaponAction.Bashing,
                WeaponActionMask.None,
                string.Empty);
    }
}
