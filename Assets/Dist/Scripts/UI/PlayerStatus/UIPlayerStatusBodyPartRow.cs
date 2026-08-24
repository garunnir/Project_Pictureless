// ============================================================
// UIPlayerStatusBodyPartRow — 메인 부위 컨디션 행 + 호버 상세 트리거
// ============================================================

using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

public sealed class UIPlayerStatusBodyPartRow :
    MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerClickHandler
{
    [SerializeField] TMP_Text _nameText;
    [FormerlySerializedAs("_hpText")]
    [SerializeField] TMP_Text _conditionText;
    [SerializeField] Image _fillImage;
    [SerializeField] Image _background;

    string _partId;
    Action<string, RectTransform> _onHover;
    Action _onExit;
    Action<string, Vector2> _onRightClick;

    public string PartId => _partId;

    public void Bind(
        string partId,
        Action<string, RectTransform> onHover,
        Action onExit,
        Action<string, Vector2> onRightClick = null)
    {
        _partId = partId;
        _onHover = onHover;
        _onExit = onExit;
        _onRightClick = onRightClick;
    }

    public void SetDisplay(string name, int cur, int max, bool present)
    {
        if (_nameText != null)
            _nameText.text = name;

        if (!present)
        {
            if (_conditionText != null)
                _conditionText.text = PlayerStatusLabels.Lost;
            if (_fillImage != null)
                _fillImage.fillAmount = 0f;
            if (_background != null)
                _background.color = new Color(0.2f, 0.12f, 0.12f, 0.9f);
            return;
        }

        if (_conditionText != null)
            _conditionText.text = PlayerStatusLabels.FormatCondition(cur, max);
        if (_fillImage != null)
            _fillImage.fillAmount = max > 0 ? Mathf.Clamp01((float)cur / max) : 0f;
        if (_background != null)
            _background.color = new Color(0.18f, 0.18f, 0.18f, 1f);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!string.IsNullOrEmpty(_partId))
            _onHover?.Invoke(_partId, transform as RectTransform);
    }

    public void OnPointerExit(PointerEventData eventData) => _onExit?.Invoke();

    public void OnPointerClick(PointerEventData eventData)
    {
        if (string.IsNullOrEmpty(_partId))
            return;
        if (eventData.button == PointerEventData.InputButton.Right)
            _onRightClick?.Invoke(_partId, eventData.position);
    }

    public void Wire(
        TMP_Text nameText,
        TMP_Text conditionText,
        Image fillImage,
        Image background)
    {
        _nameText = nameText;
        _conditionText = conditionText;
        _fillImage = fillImage;
        _background = background;
    }
}
