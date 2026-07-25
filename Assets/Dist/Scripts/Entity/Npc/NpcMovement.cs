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

    [Header("Collision")]
    [SerializeField, Min(0f)] float _climbAllowance =
        CharacterLocomotionDefaults.ClimbAllowance;
    [SerializeField, Min(0f)] float _baseSkin =
        CharacterLocomotionDefaults.BaseSkin;
    [Tooltip("WalkableOnly 소품·경사 등 Physics 충돌 레이어")]
    [SerializeField] LayerMask _collisionMask =
        CharacterLocomotionDefaults.AllCollisionLayers;
    [SerializeField] QueryTriggerInteraction _triggerInteraction =
        QueryTriggerInteraction.Ignore;
    [Tooltip("논리 낙하 중력 (useGravity 대신 사용)")]
    [SerializeField] float _logicalGravity =
        CharacterLocomotionDefaults.LogicalGravity;
    [Tooltip("topology 벽 셀 끼임 탈출 push 속도")]
    [SerializeField, Min(0f)] float _topologyPushSpeed =
        CharacterLocomotionDefaults.TopologyPushSpeed;
    [Tooltip("같은 FixedUpdate 내 topology 탈출 push 최대 반복")]
    [SerializeField, Min(1)] int _topologyPushMaxIterations =
        CharacterLocomotionDefaults.TopologyPushMaxIterations;

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

    void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _capsule = GetComponent<CapsuleCollider>();
        _characterState = GetComponent<CharacterState>();

        _rigidbody.freezeRotation = true;
        _rigidbody.useGravity = false;

        _mover = new KinematicMover
        {
            BaseSkin = _baseSkin,
            CollisionMask = _collisionMask,
            TriggerInteraction = _triggerInteraction,
        };

        _locomotion = new CharacterLocomotion(
            _rigidbody,
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
            _topologyPushMaxIterations);
        _locomotion.BindMapCollision(_pendingMapCollision);
    }

    void FixedUpdate()
    {
        float deltaTime = TimeScaleService.FixedDelta(TimeScaleChannel.World);
        Vector3 desiredMove = _mover.CalcConstantSpeedMove(_moveSpeed, deltaTime);

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
