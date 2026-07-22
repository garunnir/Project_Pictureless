// ============================================================

// UIPlayerStatusMoodIconSlot — 이중 레이어 아이콘 + 호버 툴팁 + 주목 흔들림

// ============================================================



using DG.Tweening;

using Garunnir.Runtime.Gameplay.Data;

using UnityEngine;

using UnityEngine.EventSystems;

using UnityEngine.UI;



public sealed class UIPlayerStatusMoodIconSlot :

    MonoBehaviour,

    IPointerEnterHandler,

    IPointerExitHandler

{

    [SerializeField] Image _backImage;

    [SerializeField] Image _frontImage;

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



    public void Apply(MoodEntry entry, Sprite backPlate, Sprite frontGlyph)

    {

        if (_backImage != null)

        {

            _backImage.sprite = backPlate;

            _backImage.color = PlayerStatusMoodVisuals.ResolveBackColor(entry.Polarity, entry.Intensity);

            _backImage.enabled = backPlate != null;

        }



        if (_frontImage != null)

        {

            _frontImage.sprite = frontGlyph;

            _frontImage.color = Color.white;

            _frontImage.enabled = frontGlyph != null;

        }



        _tooltipText = entry.TooltipText;

    }



    public void Wire(Image backImage, Image frontImage, RectTransform shakeRoot = null)

    {

        _backImage = backImage;

        _frontImage = frontImage;

        _shakeRoot = shakeRoot;

    }



    public void OnPointerEnter(PointerEventData eventData)

    {

        if (!string.IsNullOrEmpty(_tooltipText))

            _owner?.ShowTooltip(_tooltipText, transform as RectTransform);

    }



    public void OnPointerExit(PointerEventData eventData) => _owner?.HideTooltip();



    void OnDestroy() => StopAttentionShake();



    void EnsureShakeRoot()

    {

        if (_shakeRoot != null)

            return;



        Transform found = transform.Find("ShakeRoot");

        if (found is RectTransform existing)

        {

            _shakeRoot = existing;

            return;

        }



        if (_backImage == null && _frontImage == null)

            return;



        var shakeGo = new GameObject("ShakeRoot", typeof(RectTransform));

        _shakeRoot = shakeGo.GetComponent<RectTransform>();

        _shakeRoot.SetParent(transform, false);

        _shakeRoot.anchorMin = Vector2.zero;

        _shakeRoot.anchorMax = Vector2.one;

        _shakeRoot.offsetMin = Vector2.zero;

        _shakeRoot.offsetMax = Vector2.zero;



        if (_backImage != null)

            _backImage.transform.SetParent(_shakeRoot, false);

        if (_frontImage != null)

            _frontImage.transform.SetParent(_shakeRoot, false);

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


