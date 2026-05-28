// ============================================================
// PlayerMovement — KinematicMover를 이용한 캡슐 기반 플레이어 이동 (MonoBehaviour 래퍼)
// ============================================================
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;

public interface IPlayerMovementDebug
{
    void LogPlayerRun(bool isRun);
    void LogPlayerStuck();
    void LogPlayerSliding(float lastSlideSqrMagnitude);
}

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider), typeof(CharacterState))]
public class PlayerMovement : MonoBehaviour, IMovable
{
    [Header("Movement")]
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _sprintMultiplier = 2f;
    [SerializeField] private float _acceleration = 10f;
    [Tooltip("0 이하일 때는 현재 이동 속도(_moveSpeed, 달리기 포함)를 초기속도로 사용")]
    [SerializeField] private float _initialVelocity = -1f;
    [SerializeField] private Camera _refCam;
    [Tooltip("관성(감쇠) 계수. 0에 가까울수록 미끄러지듯 멈춤, 1에 가까울수록 즉시 멈춤")]
    [Range(0f, 1f)]
    [SerializeField] private float _inertia = 0.9f;
    [Tooltip("관성이 꺼지는 기준 속도. 걷기 속도보다 항상 크게 보정됩니다.")]
    [SerializeField] private float _customBaseSpeed = 6f;
    [Tooltip("이 속도 이상부터 관성 이동을 적용합니다. customBaseSpeed보다 크게 보정됩니다.")]
    [SerializeField] private float _inertiaEnableThreshold = 8f;
    [Tooltip("달리기 누적 가속의 최대 속도 상한")]
    [SerializeField] private float _runMaxSpeed = 12f;
    [Tooltip("달리기 버튼을 눌렀을 때 즉시 추가되는 1회 속도 부스트")]
    [SerializeField] private float _runEnterBoost = 1.5f;

    [Header("Collision")]
    [SerializeField] private float _climbAllowance = 0.3f;
    [SerializeField] private float _baseSkin = 0.02f;
    [SerializeField] private LayerMask _collisionMask = ~0;
    [SerializeField] private QueryTriggerInteraction _triggerInteraction = QueryTriggerInteraction.Ignore;

    [SerializeField,ReadOnly] private Vector2 _moveDir;
    Rigidbody _rb;
    CapsuleCollider _capsule;
    CharacterState _characterState;
    KinematicMover _mover;
    bool _pendingInitialVelocity;
    [SerializeField] private MonoBehaviour _debugControllerBehaviour;
    IPlayerMovementDebug _debugController;

    RaycastHit[] _hits = new RaycastHit[8];

    // Gizmo 전용 캐시
    int _lastHitCount;
    Vector3 _lastP1, _lastDesiredMove;

    public CapsuleCollider Capsule => _capsule;
    public RaycastHit[] Hits => _hits;
    public int LastHitCount => _lastHitCount;
    public Vector3 LastP1 => _lastP1;
    public Vector3 LastDesiredMove => _lastDesiredMove;
    public float BaseSkin => _baseSkin;
    public int LastNearestIndex => _mover != null ? _mover.LastNearestIndex : -1;
    public Vector3 LastSlide => _mover != null ? _mover.LastSlide : Vector3.zero;
    public bool IsSprinting => _mover != null && _mover.IsSprinting;
    public bool IsInertiaActive => _mover != null && _mover.IsInertiaActive;
    public float CurrentSpeed => _mover != null ? _mover.CurrentSpeed : 0f;
    public float InitialVelocity
    {
        get => _initialVelocity;
        set => _initialVelocity = Mathf.Max(-1f, value);
    }

    public void SetControllEnabled(bool enabled)
    {
        if (enabled)
        {
            _pendingInitialVelocity = true;
            ConnectController();
        }
        else
        {
            DisconnectController();
        }
    }

    void Awake()
    {
        NormalizeSpeedThresholds();

        _rb = GetComponent<Rigidbody>();
        _capsule = GetComponent<CapsuleCollider>();
        _characterState = GetComponent<CharacterState>();
        if (_debugControllerBehaviour == null) TryGetComponent(out _debugControllerBehaviour);
        _debugController = _debugControllerBehaviour as IPlayerMovementDebug;
        _rb.freezeRotation = true;

        _mover = new KinematicMover
        {
            Acceleration       = _acceleration,
            Inertia            = _inertia,
            BaseSkin           = _baseSkin,
            CollisionMask      = _collisionMask,
            TriggerInteraction = _triggerInteraction,
        };
    }

    void ConnectController()
    {
        InputManager.Instance.Actions.Player.Move.performed += OnMove;
        InputManager.Instance.Actions.Player.Move.canceled  += OnMove;
        InputManager.Instance.Actions.Player.Run.performed  += OnRun;
        InputManager.Instance.Actions.Player.Run.canceled   += OnRun;
    }

    void DisconnectController()
    {
        InputManager.Instance.Actions.Player.Move.performed -= OnMove;
        InputManager.Instance.Actions.Player.Move.canceled  -= OnMove;
        InputManager.Instance.Actions.Player.Run.performed  -= OnRun;
        InputManager.Instance.Actions.Player.Run.canceled   -= OnRun;
    }

    public UnityEngine.Vector2 GetDirection(){
        return _moveDir;
    }
    public void OnMove(InputAction.CallbackContext context)
    {
        Vector2 inputDir=context.ReadValue<Vector2>();
        _mover.SetInput(inputDir, _refCam);

        if (_pendingInitialVelocity && inputDir.sqrMagnitude > Mathf.Epsilon)
        {
            if (_mover.IsSprinting)
                _mover.SetInitialVelocity(GetEffectiveInitialVelocity());
            _pendingInitialVelocity = false;
        }

        _characterState.SetMoveDir(_mover.WorldMoveDir);
        _characterState.UpdateGridPos(transform.position);
    }

    public void OnRun(InputAction.CallbackContext context)
    {
        bool wasSprinting = _mover.IsSprinting;
        bool isRun = context.ReadValue<float>() > 0.5f;
        _mover.SetSprinting(isRun);
        if (isRun)
        {
            _pendingInitialVelocity = true;
            if (_mover.WorldMoveDir.sqrMagnitude > Mathf.Epsilon)
            {
                _mover.SetInitialVelocity(GetEffectiveInitialVelocity());
                if (!wasSprinting)
                    _mover.ApplySpeedBoost(_runEnterBoost, _runMaxSpeed);
                _pendingInitialVelocity = false;
            }
        }
        _debugController?.LogPlayerRun(isRun);
    }

    void FixedUpdate()
    {
        Vector3 desiredMove = _mover.CalcDesiredMove(
            _moveSpeed,
            _sprintMultiplier,
            Time.fixedDeltaTime,
            _customBaseSpeed,
            _inertiaEnableThreshold,
            _runMaxSpeed);
        _lastDesiredMove = desiredMove;

        if (desiredMove.sqrMagnitude <= Mathf.Epsilon)
        {
            _lastHitCount = 0;
            _characterState.UpdateGridPos(transform.position);
            return;
        }

        Vector3 worldCenter = transform.TransformPoint(_capsule.center);
        Vector3 up          = transform.up;
        float halfHeight    = Mathf.Max(0f, (_capsule.height * 0.5f) - _capsule.radius);
        Vector3 p1 = worldCenter + up * halfHeight;
        Vector3 p2 = worldCenter - up * (halfHeight - _climbAllowance);
        float radius = _capsule.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y);

        float distance = desiredMove.magnitude;
        int hitCount = Physics.CapsuleCastNonAlloc(
            p1, p2, radius, desiredMove.normalized,
            _hits, distance + _baseSkin, _collisionMask, _triggerInteraction);

        _lastP1          = p1;
        _lastHitCount    = hitCount;

        if (hitCount == 0)
            _rb.MovePosition(_rb.position + desiredMove);
        else
        {
            Vector3 delta = _mover.ResolveMove(desiredMove, p1, p2, radius, _hits, hitCount, _capsule);

            if (delta.sqrMagnitude <= Mathf.Epsilon)
            {
                _rb.MovePosition(_rb.position);
                _debugController?.LogPlayerStuck();
            }
            else
            {
                if (_mover.LastSlide.sqrMagnitude > 0f)
                {
                    _debugController?.LogPlayerSliding(_mover.LastSlide.sqrMagnitude);
                }

                _rb.MovePosition(_rb.position + delta);
                _moveDir = delta.normalized;
            }
        }

        // 이동이 없던 프레임과 관계 없이 피직스 적용 후 항상 갱신(오클루전 거리 블렌드 끊김 방지)
        _characterState.UpdateGridPos(transform.position);
    }

    private float GetEffectiveInitialVelocity()
    {
        if (_initialVelocity > 0f)
            return _initialVelocity;

        return _customBaseSpeed;
    }

    private void NormalizeSpeedThresholds()
    {
        float minBase = _moveSpeed + 0.01f;
        _customBaseSpeed = Mathf.Max(_customBaseSpeed, minBase);
        _inertiaEnableThreshold = Mathf.Max(_inertiaEnableThreshold, _customBaseSpeed + 0.01f);
        _runMaxSpeed = Mathf.Max(_runMaxSpeed, _inertiaEnableThreshold);
    }
}
