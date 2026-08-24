// ============================================================
// UIHudLifeThreatOverlay — 빙의 본체 생명 위험 HUD 빨간 비니엣
// ============================================================

using Garunnir.Runtime.Gameplay.Data;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(CanvasGroup))]
public sealed class UIHudLifeThreatOverlay : MonoBehaviour
{
    [SerializeField] CanvasGroup _canvasGroup;
    [SerializeField] Image _vignetteImage;
    [SerializeField] Color _tintColor = new Color(0.85f, 0.08f, 0.06f, 1f);
    [SerializeField] float _maxAlpha = 0.45f;
    [SerializeField] float _pulseAmplitude = 0.08f;
    [SerializeField] float _pulseSpeed = 2.5f;

    PlayerStatusViewModel _viewModel;
    float _baseIntensity;
    float _pulsePhase;
    bool _bound;

    void Awake()
    {
        if (_canvasGroup == null)
            TryGetComponent(out _canvasGroup);
        if (_vignetteImage == null)
            TryGetComponent(out _vignetteImage);

        SetVisible(false);
    }

    void OnEnable()
    {
        TryBind();
        RefreshIntensity();
    }

    void OnDisable()
    {
        UnbindViewModel();
    }

    void LateUpdate()
    {
        if (!_bound)
            TryBind();

        if (_baseIntensity <= 0f)
            return;

        float dt = TimeScaleService.Delta(TimeScaleChannel.Realtime);
        _pulsePhase += dt * _pulseSpeed;
        float pulse = Mathf.Sin(_pulsePhase) * _pulseAmplitude * _baseIntensity;
        _canvasGroup.alpha = Mathf.Clamp01(_baseIntensity * _maxAlpha + pulse);
    }

    void TryBind()
    {
        if (_bound)
            return;

        if (!PlayerStatusUIBridge.TryResolve(out _viewModel))
            return;

        _viewModel.Changed += OnViewModelChanged;
        _bound = true;
        RefreshIntensity();
    }

    void UnbindViewModel()
    {
        if (!_bound || _viewModel == null)
            return;

        _viewModel.Changed -= OnViewModelChanged;
        _viewModel = null;
        _bound = false;
    }

    void OnViewModelChanged() => RefreshIntensity();

    void RefreshIntensity()
    {
        if (!_bound)
            TryBind();

        _baseIntensity = BodyCapacity.LifeThreat01(_viewModel != null ? _viewModel.Body : null);
        if (_baseIntensity <= 0f)
        {
            SetVisible(false);
            return;
        }

        if (_vignetteImage != null)
        {
            _vignetteImage.color = _tintColor;
            _vignetteImage.enabled = true;
        }

        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        _canvasGroup.alpha = _baseIntensity * _maxAlpha;
        _canvasGroup.blocksRaycasts = false;
        _canvasGroup.interactable = false;
    }

    void SetVisible(bool visible)
    {
        _baseIntensity = 0f;
        _pulsePhase = 0f;
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;
        }

        if (_vignetteImage != null)
            _vignetteImage.enabled = visible;

        enabled = true;
    }
}
