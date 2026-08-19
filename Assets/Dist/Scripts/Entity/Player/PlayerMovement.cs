// ============================================================
// PlayerMovement — 입력이 켜져 있을 때만 CharacterMotor에 관성·달리기 desired를 씀
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

public class PlayerMovement : MonoBehaviour, IMovable, ICharacterMotorDrive
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

    [SerializeField,ReadOnly] private Vector2 _moveDir;
    CharacterMotor _motor;
    CharacterState _characterState;
    CharacterFacingAnim _facingAnim;
    bool _pendingInitialVelocity;
    [SerializeField] private MonoBehaviour _debugControllerBehaviour;
    IPlayerMovementDebug _debugController;
    bool _controlEnabled;

    float _encumbranceSpeedMultiplier = 1f;
    float _liftStrainSpeedMultiplier = 1f;
    float _envSpeedMultiplier = 1f;
    bool _encumbranceBlocksSprint;
    bool _encumbranceBlocksMovement;

    public static event System.Action AnyImmobileMoveAttempted;

    public CapsuleCollider Capsule => _motor != null ? _motor.Capsule : null;
    public RaycastHit[] Hits => _motor != null ? _motor.Hits : null;
    public int LastHitCount => _motor != null ? _motor.LastHitCount : 0;
    public Vector3 LastP1 => _motor != null ? _motor.LastP1 : Vector3.zero;
    public Vector3 LastDesiredMove =>
        _motor != null ? _motor.LastDesiredMove : Vector3.zero;
    public float BaseSkin => _motor != null ? _motor.BaseSkin : 0f;
    public int LastNearestIndex => _motor != null ? _motor.LastNearestIndex : -1;
    public Vector3 LastSlide =>
        _motor != null ? _motor.LastSlide : Vector3.zero;
    public bool IsSprinting => _motor != null && _motor.IsSprinting;
    public bool IsInertiaActive => _motor != null && _motor.IsInertiaActive;
    public float CurrentSpeed => _motor != null ? _motor.CurrentSpeed : 0f;
    /// <summary>애니 Speed 정규화 분모 (달리기 상한). Inspector <c>_runMaxSpeed</c> SSOT.</summary>
    public float RunMaxSpeed => _runMaxSpeed;
    public float AnimSpeedReference => _runMaxSpeed;
    public bool IsStuck => _motor != null && _motor.IsStuck;
    public float InitialVelocity
    {
        get => _initialVelocity;
        set => _initialVelocity = Mathf.Max(-1f, value);
    }

    public void SetEncumbranceMovement(
        float speedMultiplier,
        bool blocksSprint,
        bool blocksMovement)
    {
        _encumbranceSpeedMultiplier = Mathf.Max(0f, speedMultiplier);
        _encumbranceBlocksSprint = blocksSprint;
        _encumbranceBlocksMovement = blocksMovement;

        KinematicMover mover = ActiveMover;
        if (mover == null)
            return;

        if (blocksSprint || blocksMovement)
            SetSprinting(false);

        if (blocksMovement)
        {
            mover.SetInput(Vector2.zero, _refCam);
            _characterState?.SetMoveDir(Vector3.zero);
        }
    }

    /// <summary>LiftStrain(들기 힘 부담) 이동 배율. 1 = 없음. GearConstants.LiftStrainMoveFactor.</summary>
    public void SetLiftStrainMovement(float speedMultiplier) =>
        _liftStrainSpeedMultiplier = Mathf.Max(0f, speedMultiplier);

    /// <summary>Env 이동 배율 (GearEnvPenalties × BodyLocomotionPenalties). ClimateHost가 motor와 같은 값을 넣는다.</summary>
    public void SetEnvMovement(float speedMultiplier) =>
        _envSpeedMultiplier = Mathf.Max(0f, speedMultiplier);

    public void BindBody(CharacterMotor motor, CharacterState state, CharacterFacingAnim facing)
    {
        if (_motor != null && _motor != motor)
            _motor.BindDrive(null);

        _motor = motor;
        _characterState = state;
        _facingAnim = facing;
        if (_motor != null)
            _motor.BindDrive(this);
        ApplyDriveMoverSettings();
    }

    public void SetControllEnabled(bool enabled)
    {
        _controlEnabled = enabled;
        _motor?.SetPossessed(enabled);
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

        _motor = GetComponent<CharacterMotor>();
        _characterState = GetComponent<CharacterState>();
        TryGetComponent(out _facingAnim);
        if (_debugControllerBehaviour == null) TryGetComponent(out _debugControllerBehaviour);
        _debugController = _debugControllerBehaviour as IPlayerMovementDebug;

        if (_motor != null)
        {
            _motor.BindDrive(this);
            ApplyDriveMoverSettings();
        }
    }

    void Start()
    {
        // Motor Awake가 뒤면 Awake 시점 Mover가 null이다. Start에서 관성 세팅을 다시 넣는다.
        ApplyDriveMoverSettings();
    }

    KinematicMover ActiveMover => _motor != null ? _motor.Mover : null;

    void ApplyDriveMoverSettings()
    {
        if (_motor == null)
            return;
        _motor.ConfigureDriveMover(_acceleration, _inertia);
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
        KinematicMover mover = ActiveMover;
        if (!_controlEnabled || mover == null)
            return;

        Vector2 inputDir = context.ReadValue<Vector2>();
        if (_encumbranceBlocksMovement)
        {
            if (inputDir.sqrMagnitude > Mathf.Epsilon)
                AnyImmobileMoveAttempted?.Invoke();

            mover.SetInput(Vector2.zero, _refCam);
            _characterState.SetMoveDir(Vector3.zero);
            _characterState.UpdateGridPos(_motor != null ? _motor.transform.position : transform.position);
            return;
        }

        mover.SetInput(inputDir, _refCam);

        if (_pendingInitialVelocity && inputDir.sqrMagnitude > Mathf.Epsilon)
        {
            if (mover.IsSprinting)
                mover.SetInitialVelocity(GetEffectiveInitialVelocity());
            _pendingInitialVelocity = false;
        }

        _characterState.SetMoveDir(mover.WorldMoveDir);
        _characterState.UpdateGridPos(_motor != null ? _motor.transform.position : transform.position);
    }

    public void OnRun(InputAction.CallbackContext context)
    {
        KinematicMover mover = ActiveMover;
        if (!_controlEnabled || mover == null)
            return;

        if (_encumbranceBlocksSprint || _encumbranceBlocksMovement)
        {
            SetSprinting(false);
            return;
        }

        bool wasSprinting = mover.IsSprinting;
        bool isRun = context.ReadValue<float>() > 0.5f;
        SetSprinting(isRun);
        if (isRun)
        {
            _pendingInitialVelocity = true;
            if (mover.WorldMoveDir.sqrMagnitude > Mathf.Epsilon)
            {
                mover.SetInitialVelocity(GetEffectiveInitialVelocity());
                if (!wasSprinting)
                    mover.ApplySpeedBoost(_runEnterBoost, _runMaxSpeed);
                _pendingInitialVelocity = false;
            }
        }
        _debugController?.LogPlayerRun(isRun);
    }

    void SetSprinting(bool isRun)
    {
        ActiveMover?.SetSprinting(isRun);
        _facingAnim?.SetRunning(isRun);
    }

    public Vector3 CalcDesiredMove(KinematicMover mover, float dt)
    {
        if (_encumbranceBlocksMovement || _encumbranceSpeedMultiplier <= 0f)
            return Vector3.zero;

        float moveSpeed = _moveSpeed
            * _encumbranceSpeedMultiplier
            * _liftStrainSpeedMultiplier
            * _envSpeedMultiplier;
        float sprintMultiplier = _encumbranceBlocksSprint ? 1f : _sprintMultiplier;
        return mover.CalcDesiredMove(
            moveSpeed,
            sprintMultiplier,
            dt,
            _customBaseSpeed,
            _inertiaEnableThreshold,
            _runMaxSpeed);
    }

    public void AfterMove(CharacterMotor motor)
    {
        if (motor.LastHitCount > 0 &&
            motor.LastAppliedDelta.sqrMagnitude > Mathf.Epsilon)
            _moveDir = motor.LastAppliedDelta.normalized;

        LogMovementDiagnostics(motor);
    }

    void LogMovementDiagnostics(CharacterMotor motor)
    {
        if (motor.LastPhysicsStuck)
        {
            _debugController?.LogPlayerStuck();
            return;
        }

        if (motor.LastHitCount > 0 &&
            motor.LastSlide.sqrMagnitude > 0f)
            _debugController?.LogPlayerSliding(motor.LastSlide.sqrMagnitude);

        MapTopologyDepenetration.PushOutResult topologyPush = motor.LastTopologyPush;
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
