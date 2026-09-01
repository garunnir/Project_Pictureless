// ============================================================
// CameraFollowTargetDriver — 스무딩 proxy + Cinemachine Brain ManualUpdate
// ============================================================
// Cinemachine Brain manual: Follow/LookAt을 코드로 움직이면 Update Method = Manual Update,
// proxy 이동이 끝난 뒤 brain.ManualUpdate()를 렌더 프레임당 정확히 1회 호출한다.
// https://docs.unity3d.com/Packages/com.unity.cinemachine@3.1/manual/CinemachineBrain.html
using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// 타겟을 <see cref="SmoothDamp"/>한 proxy를 Follow/LookAt으로 쓴다.
/// Main Camera는 proxy 갱신 직후 <see cref="CinemachineBrain.ManualUpdate"/>로만 위치가 정해진다
/// (Position Composer damping 포함).
/// </summary>
[DefaultExecutionOrder(-100)]
[DisallowMultipleComponent]
[RequireComponent(typeof(CinemachineCamera))]
public class CameraFollowTargetDriver : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform _followTarget;
    [SerializeField] private CharacterState _characterState;
    [SerializeField] private Transform _proxyTarget;

    [Header("Offsets")]
    [SerializeField] private Vector3 _followOffset = new Vector3(0f, 8f, -8f);
    [SerializeField, Range(0f, 1f)] private float _aimLeadWeight = 0.32f;

    [Header("Damping")]
    [SerializeField, Min(0.01f)] private float _positionSmoothTime = 0.12f;
    [SerializeField] private float _maxSpeed = 100f;

    private CinemachineCamera _cinemachineCamera;
    private CinemachineBrain _cinemachineBrain;
    private Vector3 _smoothedVelocity;
    private Vector3 _latestAimPoint;
    private bool _hasAimPoint;

    private void Awake()
    {
        _cinemachineCamera = GetComponent<CinemachineCamera>();
        EnsureMainCameraBrain();
        EnsureProxyTarget();
        BindToCinemachine();
    }

    private void OnEnable()
    {
        BindCharacterState(_characterState);
        if (_followTarget != null && _proxyTarget != null)
            _proxyTarget.position = GetDesiredPosition();

        SyncBrainAfterFollowMoved();
    }

    private void OnDisable()
    {
        BindCharacterState(null);
    }

    private void LateUpdate()
    {
        if (_cinemachineBrain == null)
            EnsureMainCameraBrain();

        if (_followTarget == null || _proxyTarget == null)
            return;

        Vector3 desired = GetDesiredPosition();
        _proxyTarget.position = Vector3.SmoothDamp(
            _proxyTarget.position,
            desired,
            ref _smoothedVelocity,
            _positionSmoothTime,
            _maxSpeed,
            TimeScaleService.Delta(TimeScaleChannel.Player));

        SyncBrainAfterFollowMoved();
    }

    public void SetTarget(Transform target, CharacterState state)
    {
        _followTarget = target;
        BindCharacterState(state);
        if (_proxyTarget != null && _followTarget != null)
            _proxyTarget.position = GetDesiredPosition();

        SyncBrainAfterFollowMoved();
    }

    public void SetAimLeadWeight(float weight)
    {
        _aimLeadWeight = Mathf.Clamp01(weight);
    }

    private void BindCharacterState(CharacterState state)
    {
        if (_characterState != null)
            _characterState.AimWorldPointChanged -= OnAimWorldPointChanged;

        _characterState = state;
        _hasAimPoint = false;

        if (_characterState != null)
        {
            _latestAimPoint = _characterState.AimWorldPoint;
            _hasAimPoint = _characterState.IsAiming;
            _characterState.AimWorldPointChanged += OnAimWorldPointChanged;
        }
    }

    private void OnAimWorldPointChanged(Vector3 worldPoint)
    {
        _latestAimPoint = worldPoint;
        _hasAimPoint = _characterState != null && _characterState.IsAiming;
    }

    private Vector3 GetDesiredPosition()
    {
        Vector3 followBase = _followTarget.position + _followOffset;
        if (_characterState == null || !_characterState.IsAiming || !_hasAimPoint)
            return followBase;

        Vector3 aimBase = _latestAimPoint + _followOffset;
        return Vector3.Lerp(followBase, aimBase, _aimLeadWeight);
    }

    private void BindToCinemachine()
    {
        if (_cinemachineCamera == null || _proxyTarget == null)
            return;

        _cinemachineCamera.Follow = _proxyTarget;
        _cinemachineCamera.LookAt = _proxyTarget;
    }

    private void EnsureProxyTarget()
    {
        if (_proxyTarget != null)
            return;

        Transform found = transform.Find("CameraProxyTarget");
        if (found == null)
        {
            GameObject go = new GameObject("CameraProxyTarget");
            found = go.transform;
            found.SetParent(transform, false);
        }

        _proxyTarget = found;
    }

    private void EnsureMainCameraBrain()
    {
        Camera cam = Camera.main;
        if (cam == null)
            return;

        if (!cam.TryGetComponent(out _cinemachineBrain))
            _cinemachineBrain = cam.gameObject.AddComponent<CinemachineBrain>();

        _cinemachineBrain.UpdateMethod = CinemachineBrain.UpdateMethods.ManualUpdate;
        _cinemachineBrain.BlendUpdateMethod = CinemachineBrain.BrainUpdateMethods.LateUpdate;
    }

    /// <summary>Follow/LookAt(proxy) 이동 직후, 렌더 프레임당 1회.</summary>
    private void SyncBrainAfterFollowMoved()
    {
        if (_cinemachineBrain == null)
            return;

        float deltaTime = TimeScaleService.Delta(TimeScaleChannel.Player);
        _cinemachineBrain.ManualUpdate(Time.frameCount, deltaTime);
    }
}
