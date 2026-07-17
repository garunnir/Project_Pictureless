// ============================================================
// UIPlayerStatusBodyPartRow — 메인 부위 HP 행 + 호버 상세 트리거
// ============================================================

using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class UIPlayerStatusBodyPartRow : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] TMP_Text _nameText;
    [SerializeField] TMP_Text _hpText;
    [SerializeField] Image _fillImage;
    [SerializeField] Image _background;

    string _partId;
    Action<string> _onHover;
    Action _onExit;

    public string PartId => _partId;

    public void Bind(string partId, Action<string> onHover, Action onExit)
    {
        _partId = partId;
        _onHover = onHover;
        _onExit = onExit;
    }

    public void SetDisplay(string name, int cur, int max, bool present)
    {
        if (_nameText != null)
            _nameText.text = name;

        if (!present)
        {
            if (_hpText != null)
                _hpText.text = PlayerStatusLabels.Lost;
            if (_fillImage != null)
                _fillImage.fillAmount = 0f;
            if (_background != null)
                _background.color = new Color(0.2f, 0.12f, 0.12f, 0.9f);
            return;
        }

        if (_hpText != null)
            _hpText.text = PlayerStatusLabels.FormatHp(cur, max);
        if (_fillImage != null)
            _fillImage.fillAmount = max > 0 ? Mathf.Clamp01((float)cur / max) : 0f;
        if (_background != null)
            _background.color = new Color(0.18f, 0.18f, 0.18f, 1f);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!string.IsNullOrEmpty(_partId))
            _onHover?.Invoke(_partId);
    }

    public void OnPointerExit(PointerEventData eventData) => _onExit?.Invoke();

    public void Wire(
        TMP_Text nameText,
        TMP_Text hpText,
        Image fillImage,
        Image background)
    {
        _nameText = nameText;
        _hpText = hpText;
        _fillImage = fillImage;
        _background = background;
    }
}
