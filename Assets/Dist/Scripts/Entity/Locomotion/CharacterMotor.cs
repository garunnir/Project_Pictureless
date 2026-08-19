// ============================================================
// CharacterMotor — 캐릭터 공용 캡슐 locomotion (possessed=Player 채널, 아니면 World)
// ============================================================

using IsoTilemap;
using UnityEngine;

public interface ICharacterMotorDrive
{
    float AnimSpeedReference { get; }
    Vector3 CalcDesiredMove(KinematicMover mover, float deltaTime);
    void AfterMove(CharacterMotor motor);
}

[DefaultExecutionOrder(-40)]
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider), typeof(CharacterState))]
public sealed class CharacterMotor : MonoBehaviour, ICharacterLocomotion
{
    [Header("Movement")]
    [SerializeField, Min(0f)] float _moveSpeed = 3f;
    [SerializeField] MovementStyle _activeStyle;

    [Header("Collision")]
    [SerializeField] CharacterLocomotionCollisionSettings _collision =
        CharacterLocomotionCollisionSettings.Default;

    readonly RaycastHit[] _hits =
        new RaycastHit[CharacterLocomotionDefaults.HitBufferSize];

    Rigidbody _rigidbody;
    CapsuleCollider _capsule;
    CharacterState _characterState;
    KinematicMover _mover;
    CharacterLocomotion _locomotion;
    MapCollisionServices _pendingMapCollision;
    ICharacterMotorDrive _drive;
    bool _possessed;
    bool _hasTravelLimit;
    float _remainingTravelDistance;
    float _envSpeedMultiplier = 1f;

    public bool IsPossessed => _possessed;
    public bool IsStuck => _locomotion != null && _locomotion.IsStuck;
    public float CurrentSpeed => _mover != null ? _mover.CurrentSpeed : 0f;
    public float AnimSpeedReference =>
        _possessed && _drive != null ? _drive.AnimSpeedReference : EffectiveMoveSpeed;
    public MovementStyle ActiveStyle => _activeStyle;
    public KinematicMover Mover => _mover;
    public CapsuleCollider Capsule => _capsule;
    public RaycastHit[] Hits => _hits;
    public int LastHitCount => _locomotion != null ? _locomotion.LastHitCount : 0;
    public Vector3 LastP1 =>
        _locomotion != null ? _locomotion.LastCapsulePoint : Vector3.zero;
    public Vector3 LastDesiredMove =>
        _locomotion != null ? _locomotion.LastDesiredMove : Vector3.zero;
    public Vector3 LastAppliedDelta { get; private set; }
    public float BaseSkin => _collision.BaseSkin;
    public int LastNearestIndex => _mover != null ? _mover.LastNearestIndex : -1;
    public Vector3 LastSlide => _mover != null ? _mover.LastSlide : Vector3.zero;
    public bool IsSprinting => _mover != null && _mover.IsSprinting;
    public bool IsInertiaActive => _mover != null && _mover.IsInertiaActive;
    public bool LastPhysicsStuck => _locomotion != null && _locomotion.LastPhysicsStuck;
    public MapTopologyDepenetration.PushOutResult LastTopologyPush =>
        _locomotion != null
            ? _locomotion.LastTopologyPush
            : MapTopologyDepenetration.PushOutResult.None;

    float EffectiveMoveSpeed =>
        _activeStyle != null ? _activeStyle.MoveSpeed : _moveSpeed;

    void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _capsule = GetComponent<CapsuleCollider>();
        _characterState = GetComponent<CharacterState>();

        _rigidbody.freezeRotation = true;
        _rigidbody.useGravity = false;

        _mover = new KinematicMover
        {
            BaseSkin = _collision.BaseSkin,
            CollisionMask = _collision.CollisionMask,
            TriggerInteraction = _collision.TriggerInteraction,
        };

        _locomotion = new CharacterLocomotion(
            _rigidbody,
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
        _possessed = false;
    }

    void FixedUpdate()
    {
        // 할당 없음. _hits는 CapsuleCastNonAlloc 재사용.
        float deltaTime = TimeScaleService.FixedDelta(
            _possessed ? TimeScaleChannel.Player : TimeScaleChannel.World);

        Vector3 desiredMove;
        if (_possessed && _drive != null)
        {
            desiredMove = _drive.CalcDesiredMove(_mover, deltaTime);
        }
        else
        {
            desiredMove = _mover.CalcConstantSpeedMove(
                EffectiveMoveSpeed * _envSpeedMultiplier,
                deltaTime);
            if (_hasTravelLimit &&
                desiredMove.sqrMagnitude >
                _remainingTravelDistance * _remainingTravelDistance)
            {
                desiredMove = desiredMove.normalized * _remainingTravelDistance;
            }
        }

        LastAppliedDelta = _locomotion.Move(desiredMove, deltaTime);

        if (!_possessed && _hasTravelLimit)
        {
            _remainingTravelDistance = Mathf.Max(
                0f,
                _remainingTravelDistance - LastAppliedDelta.magnitude);
            if (_remainingTravelDistance <= Mathf.Epsilon)
                _mover.SetWorldDirection(Vector3.zero);
        }

        if (_possessed)
            _drive?.AfterMove(this);
    }

    public void BindDrive(ICharacterMotorDrive drive) => _drive = drive;

    public void ConfigureDriveMover(float acceleration, float inertia)
    {
        if (_mover == null)
            return;

        _mover.Acceleration = acceleration;
        _mover.Inertia = inertia;
    }

    public void SetPossessed(bool possessed)
    {
        _possessed = possessed;
        if (possessed)
            ClearTravelLimit();
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

    public void SetSpeed(float metersPerSecond) =>
        _moveSpeed = Mathf.Max(0f, metersPerSecond);

    /// <summary>Env 이동 배율 (GearEnv × limp). Possessed는 PlayerMovement.SetEnvMovement가 같은 값을 쓴다.</summary>
    public void SetEnvMovement(float speedMultiplier) =>
        _envSpeedMultiplier = Mathf.Max(0f, speedMultiplier);

    public void SetActiveMovementStyle(MovementStyle style)
    {
        _activeStyle = style;
        if (style != null)
            _moveSpeed = Mathf.Max(0f, style.MoveSpeed);
    }

    public void SetTravelLimit(float maxDistance)
    {
        _hasTravelLimit = true;
        _remainingTravelDistance = Mathf.Max(0f, maxDistance);
    }

    public void ClearTravelLimit()
    {
        _hasTravelLimit = false;
        _remainingTravelDistance = 0f;
    }
}
