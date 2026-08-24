// ============================================================
// UIPlayerStatusBodyPartGraphic — 인체도 부위 컨디션 색·밴디지·호버 입력
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
    static readonly Color BandageCleanColor = new(0.35f, 0.65f, 1f, 1f);
    static readonly Color BandageDirtyColor = new(0.92f, 0.82f, 0.2f, 1f);
    const float HoverHighlightAmount = 0.2f;

    public const string BandageChildName = "Img_Bandage";

    [SerializeField] Image _partImage;
    [SerializeField] Image _bandageImage;
    [SerializeField] string _partId;

    Color _displayColor = Color.white;
    Action<string, RectTransform> _onHover;
    Action _onExit;
    Action<string> _onClick;
    Action<string, Vector2> _onRightClick;

    public string PartId => _partId;
    public Image PartImage => _partImage;

    public void Bind(
        string partId,
        Action<string, RectTransform> onHover,
        Action onExit,
        Action<string> onClick = null,
        Action<string, Vector2> onRightClick = null)
    {
        _partId = partId;
        _onHover = onHover;
        _onExit = onExit;
        _onClick = onClick;
        _onRightClick = onRightClick;
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
        if (!present)
            SetBandaged(false, 0f);
    }

    public void SetBandaged(bool bandaged, float dirty01 = 0f)
    {
        if (_bandageImage == null)
            return;

        _bandageImage.enabled = bandaged;
        if (!bandaged)
            return;

        _bandageImage.color = Color.Lerp(
            BandageCleanColor,
            BandageDirtyColor,
            Mathf.Clamp01(dirty01));
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
        if (string.IsNullOrEmpty(_partId))
            return;

        if (eventData.button == PointerEventData.InputButton.Right)
        {
            _onRightClick?.Invoke(_partId, eventData.position);
            return;
        }

        if (eventData.button != PointerEventData.InputButton.Left)
            return;
        _onClick?.Invoke(_partId);
    }

    public void Wire(Image partImage, string partId, Image bandageImage = null)
    {
        _partImage = partImage;
        _partId = partId;
        if (bandageImage != null)
            _bandageImage = bandageImage;
    }
}
