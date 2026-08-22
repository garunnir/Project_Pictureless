// ============================================================
// UIPlayerStatusMoodIconSlot — Fill+Icon 슬롯 + 호버 툴팁 + 주목 흔들림
// ============================================================

using DG.Tweening;
using Garunnir.Runtime.Gameplay.Data;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

public sealed class UIPlayerStatusMoodIconSlot :
    MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [SerializeField] Image _fillImage;
    [FormerlySerializedAs("_frontImage")]
    [SerializeField] Image _iconImage;
    [SerializeField] RectTransform _shakeRoot;

    string _tooltipText = string.Empty;
    UIPlayerStatusSummaryPanel _owner;
    Tween _shakeTween;

    public void Initialize(UIPlayerStatusSummaryPanel owner)
    {
        _owner = owner;
        EnsureShakeRoot();
    }

    public void SetVisible(bool visible)
    {
        if (!visible)
        {
            StopAttentionShake();
            _tooltipText = string.Empty;
        }

        gameObject.SetActive(visible);
    }

    public void PlayAttentionShake()
    {
        EnsureShakeRoot();
        if (_shakeRoot == null)
            return;

        if (_shakeTween != null && _shakeTween.IsActive())
            return;

        Vector2 basePos = Vector2.zero;
        float amplitude = PlayerStatusMoodVisuals.AttentionShakeInitialAmplitude;
        float decay = PlayerStatusMoodVisuals.AttentionShakeDecay;
        float step = PlayerStatusMoodVisuals.AttentionShakeStepDuration;
        int oscillations = PlayerStatusMoodVisuals.AttentionShakeOscillations;

        _shakeRoot.anchoredPosition = basePos;

        Sequence sequence = DOTween.Sequence();
        float direction = -1f;
        for (int i = 0; i < oscillations; i++)
        {
            float swing = amplitude * Mathf.Pow(decay, i);
            sequence.Append(
                _shakeRoot
                    .DOAnchorPosX(basePos.x + direction * swing, step)
                    .SetEase(i == 0 ? Ease.OutQuad : Ease.InOutSine));
            direction *= -1f;
        }

        _shakeTween = sequence
            .Append(_shakeRoot.DOAnchorPosX(basePos.x, step).SetEase(Ease.OutSine))
            .SetTarget(_shakeRoot)
            .OnKill(ResetShakeRootPosition)
            .OnComplete(ResetShakeRootPosition);
    }

    public void Apply(MoodEntry entry, Sprite frontGlyph)
    {
        if (_fillImage != null)
        {
            _fillImage.color = PlayerStatusMoodVisuals.ResolveFillTint(entry.Polarity);
            _fillImage.fillAmount = Mathf.Clamp01(entry.Intensity);
            _fillImage.enabled = true;
        }

        if (_iconImage != null)
        {
            _iconImage.sprite = frontGlyph;
            _iconImage.color = Color.white;
            _iconImage.enabled = frontGlyph != null;
        }

        _tooltipText = entry.TooltipText;
    }

    public void Wire(Image fillImage, Image iconImage, RectTransform shakeRoot = null)
    {
        _fillImage = fillImage;
        _iconImage = iconImage;
        _shakeRoot = shakeRoot;
    }

    public string TooltipText => _tooltipText;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!string.IsNullOrEmpty(_tooltipText))
            _owner?.ShowTooltip(this);
    }

    public void OnPointerExit(PointerEventData eventData) => _owner?.HideTooltip(this);

    void OnDestroy() => StopAttentionShake();

    void EnsureShakeRoot()
    {
        if (_shakeRoot != null)
            return;

        Transform found = transform.Find("ShakeRoot");
        if (found is RectTransform existing)
            _shakeRoot = existing;
    }

    void StopAttentionShake()
    {
        if (_shakeTween != null && _shakeTween.IsActive())
            _shakeTween.Kill();
        _shakeTween = null;
        ResetShakeRootPosition();
    }

    void ResetShakeRootPosition()
    {
        if (_shakeRoot != null)
            _shakeRoot.anchoredPosition = Vector2.zero;
    }
}
