// ============================================================
// UIPlayerStatusBodyPartGraphic — 인체도 부위 컨디션 색상과 호버 입력
// ============================================================

using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class UIPlayerStatusBodyPartGraphic :
    MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerClickHandler
{
    static readonly Color HealthyColor = Color.white;
    static readonly Color CriticalColor = new(0.8f, 0.2f, 0.18f, 1f);
    static readonly Color LostColor = new(0.3f, 0.08f, 0.08f, 0.35f);
    const float HoverHighlightAmount = 0.2f;

    [SerializeField] Image _partImage;
    [SerializeField] string _partId;

    Color _displayColor = Color.white;
    Action<string, RectTransform> _onHover;
    Action _onExit;
    Action<string> _onClick;

    public string PartId => _partId;

    public void Bind(
        string partId,
        Action<string, RectTransform> onHover,
        Action onExit,
        Action<string> onClick = null)
    {
        _partId = partId;
        _onHover = onHover;
        _onExit = onExit;
        _onClick = onClick;
    }

    public void SetDisplay(int currentCondition, int maxCondition, bool present)
    {
        if (_partImage == null)
            return;

        if (!present)
        {
            _displayColor = LostColor;
        }
        else
        {
            float conditionRatio = maxCondition > 0
                ? Mathf.Clamp01((float)currentCondition / maxCondition)
                : 0f;
            _displayColor = Color.Lerp(
                CriticalColor,
                HealthyColor,
                conditionRatio);
        }

        _partImage.color = _displayColor;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_partImage != null)
            _partImage.color = Color.Lerp(
                _displayColor,
                HealthyColor,
                HoverHighlightAmount);

        if (!string.IsNullOrEmpty(_partId))
            _onHover?.Invoke(_partId, transform as RectTransform);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_partImage != null)
            _partImage.color = _displayColor;

        _onExit?.Invoke();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;
        if (string.IsNullOrEmpty(_partId))
            return;
        _onClick?.Invoke(_partId);
    }

    public void Wire(Image partImage, string partId)
    {
        _partImage = partImage;
        _partId = partId;
    }
}
