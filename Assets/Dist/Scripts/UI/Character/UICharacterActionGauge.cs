// ============================================================
// UICharacterActionGauge — 행위자 Host 진행을 월드 fill로 표시
// ============================================================

using UnityEngine;
using UnityEngine.UI;

public sealed class UICharacterActionGauge : MonoBehaviour
{
    [SerializeField] CharacterActionHost _host;
    [SerializeField] Image _fill;
    [SerializeField] Canvas _canvas;

    void Awake()
    {
        if (_host == null)
            _host = GetComponentInParent<CharacterActionHost>();
        if (_fill == null)
        {
            Transform fill = transform.Find(CharacterActionGaugeLayout.FillName);
            if (fill != null)
                fill.TryGetComponent(out _fill);
        }

        if (_canvas == null)
            TryGetComponent(out _canvas);
    }

    void OnEnable()
    {
        if (_canvas != null && _canvas.worldCamera == null)
            _canvas.worldCamera = Camera.main;
        ApplyVisible(false);
    }

    void LateUpdate()
    {
        // Rule 6: fillAmount만. 할당 없음.
        if (_host == null || _fill == null)
        {
            ApplyVisible(false);
            return;
        }

        bool show = _host.CurrentKind != CharacterActionKind.None;
        ApplyVisible(show);
        if (!show)
            return;

        _fill.fillAmount = _host.Progress01;
    }

    void ApplyVisible(bool show)
    {
        if (_canvas != null)
        {
            if (_canvas.enabled != show)
                _canvas.enabled = show;
            return;
        }

        if (gameObject.activeSelf != show)
            gameObject.SetActive(show);
    }
}
