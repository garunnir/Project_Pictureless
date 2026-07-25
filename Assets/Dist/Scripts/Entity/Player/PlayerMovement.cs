// ============================================================
// PlayerMovement — KinematicMover를 이용한 캡슐 기반 플레이어 이동 (MonoBehaviour 래퍼)
// ============================================================
using IsoTilemap;
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
public class PlayerMovement : MonoBehaviour, IMovable, ICharacterLocomotion
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
    [SerializeField] private float _climbAllowance =
        CharacterLocomotionDefaults.ClimbAllowance;
    [SerializeField] private float _baseSkin =
        CharacterLocomotionDefaults.BaseSkin;
    [Tooltip("WalkableOnly 소품·경사 등 Physics 충돌 레이어")]
    [SerializeField] private LayerMask _collisionMask =
        CharacterLocomotionDefaults.AllCollisionLayers;
    [SerializeField] private QueryTriggerInteraction _triggerInteraction = QueryTriggerInteraction.Ignore;
    [Tooltip("논리 낙하 중력 (useGravity 대신 사용)")]
    [SerializeField] private float _logicalGravity =
        CharacterLocomotionDefaults.LogicalGravity;
    [Tooltip("topology 벽 셀 끼임 탈출 push 속도")]
    [SerializeField] private float _topologyPushSpeed =
        CharacterLocomotionDefaults.TopologyPushSpeed;
    [Tooltip("같은 FixedUpdate 내 topology 탈출 push 최대 반복")]
    [SerializeField] private int _topologyPushMaxIter =
        CharacterLocomotionDefaults.TopologyPushMaxIterations;

    [SerializeField,ReadOnly] private Vector2 _moveDir;
    Rigidbody _rb;
    CapsuleCollider _capsule;
    CharacterState _characterState;
    KinematicMover _mover;
    CharacterLocomotion _locomotion;
    MapCollisionServices _pendingMapCollision;
    bool _pendingInitialVelocity;
    [SerializeField] private MonoBehaviour _debugControllerBehaviour;
    IPlayerMovementDebug _debugController;

    readonly RaycastHit[] _hits =
        new RaycastHit[CharacterLocomotionDefaults.HitBufferSize];

    public CapsuleCollider Capsule => _capsule;
    public RaycastHit[] Hits => _hits;
    public int LastHitCount => _locomotion != null ? _locomotion.LastHitCount : 0;
    public Vector3 LastP1 =>
        _locomotion != null ? _locomotion.LastCapsulePoint : Vector3.zero;
    public Vector3 LastDesiredMove =>
        _locomotion != null ? _locomotion.LastDesiredMove : Vector3.zero;
    public float BaseSkin => _baseSkin;
    public int LastNearestIndex => _mover != null ? _mover.LastNearestIndex : -1;
    public Vector3 LastSlide => _mover != null ? _mover.LastSlide : Vector3.zero;
    public bool IsSprinting => _mover != null && _mover.IsSprinting;
    public bool IsInertiaActive => _mover != null && _mover.IsInertiaActive;
    public float CurrentSpeed => _mover != null ? _mover.CurrentSpeed : 0f;
    public bool IsStuck => _locomotion != null && _locomotion.IsStuck;
    public float InitialVelocity
    {
        get => _initialVelocity;
        set => _initialVelocity = Mathf.Max(-1f, value);
    }

    public void BindMapCollision(MapCollisionServices services)
    {
        _pendingMapCollision = services;
        _locomotion?.BindMapCollision(services);
    }

    public void SetDesiredWorldDir(Vector3 worldDirXZ)
    {
        if (_mover == null)
            return;

        _mover.SetWorldDirection(worldDirXZ);
        _characterState?.SetMoveDir(_mover.WorldMoveDir);
    }

    public void SetSpeed(float metersPerSecond)
    {
        _moveSpeed = Mathf.Max(0f, metersPerSecond);
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
        _rb.useGravity = false;

        _mover = new KinematicMover
        {
            Acceleration       = _acceleration,
            Inertia            = _inertia,
            BaseSkin           = _baseSkin,
            CollisionMask      = _collisionMask,
            TriggerInteraction = _triggerInteraction,
        };

        _locomotion = new CharacterLocomotion(
            _rb,
            _capsule,
            transform,
            _characterState,
            _mover,
            _hits,
            _climbAllowance,
            _baseSkin,
            _collisionMask,
            _triggerInteraction,
            _logicalGravity,
            _topologyPushSpeed,
            _topologyPushMaxIter);
        _locomotion.BindMapCollision(_pendingMapCollision);
    }

    void ConnectController()
    {
        InputManager input = InputManager.Instance;
        input.PlayerMovePerformed += OnMove;
        input.PlayerMoveCanceled += OnMove;
        input.PlayerRunPerformed += OnRun;
        input.PlayerRunCanceled += OnRun;
    }

    void DisconnectController()
    {
        InputManager input = InputManager.Instance;
        if (input == null)
            return;

        input.PlayerMovePerformed -= OnMove;
        input.PlayerMoveCanceled -= OnMove;
        input.PlayerRunPerformed -= OnRun;
        input.PlayerRunCanceled -= OnRun;
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
        float dt = TimeScaleService.FixedDelta(TimeScaleChannel.Player);
        Vector3 desiredMove = CalcDesiredMove(dt);
        Vector3 horizontalDelta = _locomotion.Move(desiredMove, dt);

        if (_locomotion.LastHitCount > 0 &&
            horizontalDelta.sqrMagnitude > Mathf.Epsilon)
            _moveDir = horizontalDelta.normalized;

        LogMovementDiagnostics();
    }

    Vector3 CalcDesiredMove(float dt)
    {
        Vector3 desiredMove = _mover.CalcDesiredMove(
            _moveSpeed,
            _sprintMultiplier,
            dt,
            _customBaseSpeed,
            _inertiaEnableThreshold,
            _runMaxSpeed);
        return desiredMove;
    }

    void LogMovementDiagnostics()
    {
        if (_locomotion.LastPhysicsStuck)
        {
            _debugController?.LogPlayerStuck();
            return;
        }

        if (_locomotion.LastHitCount > 0 &&
            _mover.LastSlide.sqrMagnitude > 0f)
            _debugController?.LogPlayerSliding(_mover.LastSlide.sqrMagnitude);

        MapTopologyDepenetration.PushOutResult topologyPush =
            _locomotion.LastTopologyPush;
        if (topologyPush.WasBlocking && topologyPush.StillBlocking)
            _debugController?.LogPlayerStuck();
    }

    private float GetEffectiveInitialVelocity()
    {
        if (_initialVelocity > 0f)
            return _initialVelocity;

        return Mathf.Max(_customBaseSpeed, _moveSpeed);
    }

    private void NormalizeSpeedThresholds()
    {
        float minBase = _moveSpeed + 0.01f;
        _customBaseSpeed = Mathf.Max(_customBaseSpeed, minBase);
        _inertiaEnableThreshold = Mathf.Max(_inertiaEnableThreshold, _customBaseSpeed + 0.01f);
        _runMaxSpeed = Mathf.Max(_runMaxSpeed, _inertiaEnableThreshold);
    }
}
