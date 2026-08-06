// ============================================================
// CameraZoomController — Cinemachine orthographic 렌즈 줌 (+ HelmetVision)
// ============================================================
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(CinemachineCamera))]
public class CameraZoomController : MonoBehaviour, IMaxOrthographicSizeProvider
{
    [SerializeField, Min(0.01f)] private float _minOrthographicSize = 3f;
    [SerializeField, Min(0.01f)] private float _maxOrthographicSize = 10f;
    [SerializeField, Min(0.01f)] private float _scrollStepSize = 0.5f;
    [SerializeField, Min(0f)] private float _zoomSmoothTime = 0.08f;

    private CinemachineCamera _cinemachineCamera;
    private float _targetOrthographicSize;
    private float _zoomVelocity;
    private float _visionFactor = HelmetVision.FullVisionFactor;

    /// <summary>씬 활성 줌 컨트롤러 (HelmetVision / PlayerGearHost 소비).</summary>
    public static CameraZoomController Active { get; private set; }

    public float MinOrthographicSize =>
        Mathf.Min(_minOrthographicSize, _maxOrthographicSize);

    /// <summary>스트리밍 예산용 — VisionFactor 미적용 상한.</summary>
    public float MaxOrthographicSize =>
        Mathf.Max(_minOrthographicSize, _maxOrthographicSize);

    /// <summary>플레이어 줌 타깃에 곱하는 시야 배율 (1=정상). HelmetVision.</summary>
    public float VisionFactor => _visionFactor;

    private void Awake()
    {
        _cinemachineCamera = GetComponent<CinemachineCamera>();
        _targetOrthographicSize = _cinemachineCamera.Lens.OrthographicSize;
        ClampTarget();
    }

    private void OnEnable()
    {
        Active = this;
        ApplyOrthographicSize(_targetOrthographicSize);
    }

    private void OnDisable()
    {
        if (Active == this)
            Active = null;
        if (_cinemachineCamera != null)
            _cinemachineCamera.Lens.OrthographicSize = _targetOrthographicSize;
    }

    /// <summary>HelmetVision / PlayerGearHost.VisionFactor → ortho FOV 배율.</summary>
    public void SetVisionFactor(float visionFactor)
    {
        float next = Mathf.Clamp(
            visionFactor,
            HelmetVision.HeadCoverVisionFactor,
            HelmetVision.FullVisionFactor);
        if (Mathf.Approximately(next, _visionFactor))
            return;
        _visionFactor = next;
        ApplyCurrentTarget();
    }

    private void Update()
    {
        if (_cinemachineCamera == null)
            return;

        float scrollY = 0f;
        bool scrolled = InputManager.Instance != null
            && InputManager.Instance.TryReadZoomScroll(out scrollY);
        if (scrolled)
        {
            _targetOrthographicSize -= scrollY * _scrollStepSize / 120f;
            ClampTarget();
        }

        ApplyCurrentTarget();
    }

    private void ClampTarget()
    {
        float min = Mathf.Min(_minOrthographicSize, _maxOrthographicSize);
        float max = Mathf.Max(_minOrthographicSize, _maxOrthographicSize);
        _targetOrthographicSize = Mathf.Clamp(_targetOrthographicSize, min, max);
    }

    private void ApplyCurrentTarget()
    {
        if (_cinemachineCamera == null)
            return;

        float currentLogical = _cinemachineCamera.Lens.OrthographicSize
            / Mathf.Max(0.01f, _visionFactor);
        float nextLogical = _zoomSmoothTime <= 0f
            ? _targetOrthographicSize
            : Mathf.SmoothDamp(
                currentLogical,
                _targetOrthographicSize,
                ref _zoomVelocity,
                _zoomSmoothTime);
        ApplyOrthographicSize(nextLogical);
    }

    private void ApplyOrthographicSize(float logicalSize)
    {
        _cinemachineCamera.Lens.OrthographicSize =
            Mathf.Max(0.01f, logicalSize * _visionFactor);
    }
}
