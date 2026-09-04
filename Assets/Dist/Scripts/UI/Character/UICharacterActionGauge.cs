// ============================================================
// UICharacterActionGauge — 행위자 Host 진행 fill / 자동이동 아이콘
// ============================================================

using UnityEngine;
using UnityEngine.UI;

public sealed class UICharacterActionGauge : MonoBehaviour
{
    [SerializeField] CharacterActionHost _host;
    [SerializeField] Image _fill;
    [SerializeField] Canvas _canvas;
    [SerializeField] GameObject _autoProgressIcon;

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

        if (_autoProgressIcon == null)
            _autoProgressIcon = ResolveAutoProgressIcon();

        if (_canvas == null)
            TryGetComponent(out _canvas);
        if (_canvas == null && transform.parent != null)
            transform.parent.TryGetComponent(out _canvas);
    }

    void OnEnable()
    {
        if (_canvas != null && _canvas.worldCamera == null)
            _canvas.worldCamera = Camera.main;
        ApplyVisible(false);
        SetAutoProgressIcon(false);
    }

    void LateUpdate()
    {
        // Rule 6: fillAmount·SetActive만. 할당 없음.
        if (_host == null)
        {
            ApplyVisible(false);
            SetAutoProgressIcon(false);
            return;
        }

        bool show = _host.CurrentKind != CharacterActionKind.None;
        ApplyVisible(show);
        if (!show)
        {
            SetAutoProgressIcon(false);
            return;
        }

        bool autoMove = _host.IsCellArriving;
        SetAutoProgressIcon(autoMove);

        if (_fill != null)
        {
            if (_fill.enabled == autoMove)
                _fill.enabled = !autoMove;
            if (!autoMove)
                _fill.fillAmount = _host.Progress01;
        }
    }

    GameObject ResolveAutoProgressIcon()
    {
        Transform icon = transform.Find(CharacterActionGaugeLayout.AutoProgressIconName);
        if (icon == null && transform.parent != null)
            icon = transform.parent.Find(CharacterActionGaugeLayout.AutoProgressIconName);
        return icon != null ? icon.gameObject : null;
    }

    void SetAutoProgressIcon(bool show)
    {
        if (_autoProgressIcon == null)
            return;
        if (_autoProgressIcon.activeSelf != show)
            _autoProgressIcon.SetActive(show);
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
