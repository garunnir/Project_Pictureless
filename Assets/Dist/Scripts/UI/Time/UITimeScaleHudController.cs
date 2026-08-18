// ============================================================
// UITimeScaleHudController — 배속 HUD 바인드·갱신
// ============================================================

using UnityEngine;

public sealed class UITimeScaleHudController : MonoBehaviour
{
    [SerializeField] UITimeScaleHudPanel _panel;
    [SerializeField] Canvas _uiCanvas;

    GameplayTimeScale _timeScale;

    void Awake()
    {
        if (_panel == null)
        {
            Debug.LogError(
                "[UITimeScaleHudController] _panel is not assigned. " +
                "Run Dist/MCP/Time/Setup Canvas In Open Scene.",
                this);
            return;
        }

        if (_uiCanvas == null)
            _uiCanvas = FindAnyObjectByType<Canvas>();

        _timeScale = FindAnyObjectByType<GameplayTimeScale>();
        if (_timeScale == null)
        {
            Debug.LogError(
                "[UITimeScaleHudController] GameplayTimeScale not found in scene.",
                this);
            return;
        }

        _panel.ConfigureWindowChrome(_uiCanvas);
        _panel.BindGameplayTimeScale(_timeScale);
        _panel.RefreshLabels();
    }
}
