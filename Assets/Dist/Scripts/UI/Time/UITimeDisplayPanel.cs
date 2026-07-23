// ============================================================
// UITimeDisplayPanel — 시계 HUD 텍스트 패널
// ============================================================

using TMPro;
using UnityEngine;

public sealed class UITimeDisplayPanel : MonoBehaviour
{
    [SerializeField] TMP_Text _timeText;

    TimeViewModel _viewModel;

    public void Wire(TMP_Text timeText) => _timeText = timeText;

    public void BindViewModel(TimeViewModel viewModel) => _viewModel = viewModel;

    public void Refresh()
    {
        if (_timeText == null)
            return;

        _timeText.text = _viewModel != null
            ? _viewModel.DisplayText
            : TimeDisplayFormat.Format(0, 0, 0);
    }
}
