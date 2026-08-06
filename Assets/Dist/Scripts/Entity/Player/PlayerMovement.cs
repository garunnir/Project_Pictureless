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
    [SerializeField] private CharacterLocomotionCollisionSettings _collision =
        CharacterLocomotionCollisionSettings.Default;

    [SerializeField,ReadOnly] private Vector2 _moveDir;
    Rigidbody _rb;
    CapsuleCollider _capsule;
    CharacterState _characterState;
    CharacterFacingAnim _facingAnim;
    KinematicMover _mover;
    CharacterLocomotion _locomotion;
    MapCollisionServices _pendingMapCollision;
    bool _pendingInitialVelocity;
    [SerializeField] private MonoBehaviour _debugControllerBehaviour;
    IPlayerMovementDebug _debugController;

    readonly RaycastHit[] _hits =
        new RaycastHit[CharacterLocomotionDefaults.HitBufferSize];

    float _encumbranceSpeedMultiplier = 1f;
    bool _encumbranceBlocksSprint;
    bool _encumbranceBlocksMovement;

    public static event System.Action AnyImmobileMoveAttempted;

    public CapsuleCollider Capsule => _capsule;
    public RaycastHit[] Hits => _hits;
    public int LastHitCount => _locomotion != null ? _locomotion.LastHitCount : 0;
    public Vector3 LastP1 =>
        _locomotion != null ? _locomotion.LastCapsulePoint : Vector3.zero;
    public Vector3 LastDesiredMove =>
        _locomotion != null ? _locomotion.LastDesiredMove : Vector3.zero;
    public float BaseSkin => _collision.BaseSkin;
    public int LastNearestIndex => _mover != null ? _mover.LastNearestIndex : -1;
    public Vector3 LastSlide => _mover != null ? _mover.LastSlide : Vector3.zero;
    public bool IsSprinting => _mover != null && _mover.IsSprinting;
    public bool IsInertiaActive => _mover != null && _mover.IsInertiaActive;
    public float CurrentSpeed => _mover != null ? _mover.CurrentSpeed : 0f;
    /// <summary>애니 Speed 정규화 분모 (달리기 상한). Inspector <c>_runMaxSpeed</c> SSOT.</summary>
    public float RunMaxSpeed => _runMaxSpeed;
    public float AnimSpeedReference => _runMaxSpeed;
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

        if (_encumbranceBlocksMovement)
        {
            if (worldDirXZ.sqrMagnitude > Mathf.Epsilon)
                AnyImmobileMoveAttempted?.Invoke();

            _mover.SetWorldDirection(Vector3.zero);
            _characterState?.SetMoveDir(Vector3.zero);
            return;
        }

        _mover.SetWorldDirection(worldDirXZ);
        _characterState?.SetMoveDir(_mover.WorldMoveDir);
    }

    public void SetSpeed(float metersPerSecond)
    {
        _moveSpeed = Mathf.Max(0f, metersPerSecond);
    }

    public void SetEncumbranceMovement(
        float speedMultiplier,
        bool blocksSprint,
        bool blocksMovement)
    {
        _encumbranceSpeedMultiplier = Mathf.Max(0f, speedMultiplier);
        _encumbranceBlocksSprint = blocksSprint;
        _encumbranceBlocksMovement = blocksMovement;

        if (_mover == null)
            return;

        if (blocksSprint || blocksMovement)
            SetSprinting(false);

        if (blocksMovement)
        {
            _mover.SetInput(Vector2.zero, _refCam);
            _characterState?.SetMoveDir(Vector3.zero);
        }
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
        TryGetComponent(out _facingAnim);
        if (_debugControllerBehaviour == null) TryGetComponent(out _debugControllerBehaviour);
        _debugController = _debugControllerBehaviour as IPlayerMovementDebug;
        _rb.freezeRotation = true;
        _rb.useGravity = false;

        _mover = new KinematicMover
        {
            Acceleration       = _acceleration,
            Inertia            = _inertia,
            BaseSkin           = _collision.BaseSkin,
            CollisionMask      = _collision.CollisionMask,
            TriggerInteraction = _collision.TriggerInteraction,
        };

        _locomotion = new CharacterLocomotion(
            _rb,
            _capsule,
            transform,
            _characterState,
            _mover,
            _hits,
            _collision.ClimbAllowance,
            _collision.BaseSkin,
            _collision.CollisionMask,
            _collision.TriggerInteraction,
            _collision.LogicalGravity,
            _collision.TopologyPushSpeed,
            _collision.TopologyPushMaxIterations);
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
        Vector2 inputDir = context.ReadValue<Vector2>();
        if (_encumbranceBlocksMovement)
        {
            if (inputDir.sqrMagnitude > Mathf.Epsilon)
                AnyImmobileMoveAttempted?.Invoke();

            _mover.SetInput(Vector2.zero, _refCam);
            _characterState.SetMoveDir(Vector3.zero);
            _characterState.UpdateGridPos(transform.position);
            return;
        }

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
        if (_encumbranceBlocksSprint || _encumbranceBlocksMovement)
        {
            SetSprinting(false);
            return;
        }

        bool wasSprinting = _mover.IsSprinting;
        bool isRun = context.ReadValue<float>() > 0.5f;
        SetSprinting(isRun);
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

    void SetSprinting(bool isRun)
    {
        _mover.SetSprinting(isRun);
        _facingAnim?.SetRunning(isRun);
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
        if (_encumbranceBlocksMovement || _encumbranceSpeedMultiplier <= 0f)
            return Vector3.zero;

        float moveSpeed = _moveSpeed * _encumbranceSpeedMultiplier;
        float sprintMultiplier = _encumbranceBlocksSprint ? 1f : _sprintMultiplier;
        Vector3 desiredMove = _mover.CalcDesiredMove(
            moveSpeed,
            sprintMultiplier,
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
