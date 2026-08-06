// ============================================================
// NpcMovement — World 시간 채널로 NPC 공용 locomotion을 구동
// ============================================================

using IsoTilemap;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider), typeof(CharacterState))]
public sealed class NpcMovement : MonoBehaviour, ICharacterLocomotion
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
    bool _hasTravelLimit;
    float _remainingTravelDistance;

    public bool IsStuck => _locomotion != null && _locomotion.IsStuck;
    public float CurrentSpeed => _mover != null ? _mover.CurrentSpeed : 0f;
    public float AnimSpeedReference => EffectiveMoveSpeed;
    public MovementStyle ActiveStyle => _activeStyle;

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
    }

    void FixedUpdate()
    {
        float deltaTime = TimeScaleService.FixedDelta(TimeScaleChannel.World);
        Vector3 desiredMove = _mover.CalcConstantSpeedMove(EffectiveMoveSpeed, deltaTime);

        if (_hasTravelLimit &&
            desiredMove.sqrMagnitude >
            _remainingTravelDistance * _remainingTravelDistance)
        {
            desiredMove = desiredMove.normalized * _remainingTravelDistance;
        }

        Vector3 horizontalDelta = _locomotion.Move(desiredMove, deltaTime);
        if (!_hasTravelLimit)
            return;

        _remainingTravelDistance = Mathf.Max(
            0f,
            _remainingTravelDistance - horizontalDelta.magnitude);
        if (_remainingTravelDistance <= Mathf.Epsilon)
            _mover.SetWorldDirection(Vector3.zero);
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
