// ============================================================
// CharacterLocomotion — 캐릭터 공용 캡슐·맵 토폴로지 이동을 해결
// ============================================================

using IsoTilemap;
using UnityEngine;

public interface ICharacterLocomotion
{
    void BindMapCollision(MapCollisionServices services);
    void SetDesiredWorldDir(Vector3 worldDirXZ);
    void SetSpeed(float metersPerSecond);
    bool IsStuck { get; }
}

public static class CharacterLocomotionDefaults
{
    public const int HitBufferSize = 8;
    public const float ClimbAllowance = 0.3f;
    public const float BaseSkin = 0.02f;
    public const int AllCollisionLayers = ~0;
    public const float LogicalGravity = -9.81f;
    public const float TopologyPushSpeed = 4f;
    public const int TopologyPushMaxIterations = 4;
}

public sealed class CharacterLocomotion
{
    readonly Rigidbody _rigidbody;
    readonly CapsuleCollider _capsule;
    readonly Transform _transform;
    readonly CharacterState _characterState;
    readonly KinematicMover _mover;
    readonly RaycastHit[] _hits;
    readonly float _climbAllowance;
    readonly float _baseSkin;
    readonly LayerMask _collisionMask;
    readonly QueryTriggerInteraction _triggerInteraction;
    readonly float _logicalGravity;
    readonly float _topologyPushSpeed;
    readonly int _topologyPushMaxIterations;

    MapCollisionServices _mapCollision;
    MapTopologyDepenetration.Tracker _gridStuckTracker;
    float _verticalVelocity;

    public int LastHitCount { get; private set; }
    public Vector3 LastCapsulePoint { get; private set; }
    public Vector3 LastDesiredMove { get; private set; }
    public bool LastPhysicsStuck { get; private set; }
    public MapTopologyDepenetration.PushOutResult LastTopologyPush { get; private set; }
    public bool IsStuck =>
        LastPhysicsStuck ||
        (LastTopologyPush.WasBlocking && LastTopologyPush.StillBlocking);

    public CharacterLocomotion(
        Rigidbody rigidbody,
        CapsuleCollider capsule,
        Transform transform,
        CharacterState characterState,
        KinematicMover mover,
        RaycastHit[] hits,
        float climbAllowance,
        float baseSkin,
        LayerMask collisionMask,
        QueryTriggerInteraction triggerInteraction,
        float logicalGravity,
        float topologyPushSpeed,
        int topologyPushMaxIterations)
    {
        _rigidbody = rigidbody;
        _capsule = capsule;
        _transform = transform;
        _characterState = characterState;
        _mover = mover;
        _hits = hits;
        _climbAllowance = climbAllowance;
        _baseSkin = baseSkin;
        _collisionMask = collisionMask;
        _triggerInteraction = triggerInteraction;
        _logicalGravity = logicalGravity;
        _topologyPushSpeed = topologyPushSpeed;
        _topologyPushMaxIterations = topologyPushMaxIterations;
    }

    public void BindMapCollision(MapCollisionServices services) => _mapCollision = services;

    public Vector3 Move(Vector3 desiredMove, float deltaTime)
    {
        LastDesiredMove = desiredMove;

        Vector3 oldPosition = _rigidbody.position;
        float feetOffset = CharacterFeetPose.GetFeetOffset(_transform);

        LastPhysicsStuck = ResolvePhysicsHorizontal(desiredMove, out Vector3 horizontalDelta);
        Vector3 newPosition = ApplyTopologyHorizontal(oldPosition, feetOffset, horizontalDelta);

        float cellSize = _mapCollision != null ? _mapCollision.Query.CellSize : 1f;
        MapCollisionGrid.FeetCell feetCell =
            MapCollisionGrid.ResolveFeetCell(newPosition, feetOffset, cellSize);
        ApplyLogicalVertical(ref newPosition, feetOffset, deltaTime, ref feetCell);

        LastTopologyPush = ResolveGridStuck(
            ref newPosition,
            feetOffset,
            ref feetCell,
            deltaTime);

        _rigidbody.MovePosition(newPosition);
        _rigidbody.linearVelocity = Vector3.zero;
        _characterState.UpdateGridPos(_transform.position);
        return horizontalDelta;
    }

    bool ResolvePhysicsHorizontal(Vector3 desiredMove, out Vector3 horizontalDelta)
    {
        horizontalDelta = Vector3.zero;
        if (desiredMove.sqrMagnitude <= Mathf.Epsilon)
        {
            LastHitCount = 0;
            return false;
        }

        Vector3 worldCenter = _transform.TransformPoint(_capsule.center);
        Vector3 up = _transform.up;
        float halfHeight = Mathf.Max(0f, (_capsule.height * 0.5f) - _capsule.radius);
        Vector3 p1 = worldCenter + up * halfHeight;
        Vector3 p2 = worldCenter - up * (halfHeight - _climbAllowance);
        float radius = _capsule.radius *
            Mathf.Max(_transform.lossyScale.x, _transform.lossyScale.y);

        int hitCount = Physics.CapsuleCastNonAlloc(
            p1,
            p2,
            radius,
            desiredMove.normalized,
            _hits,
            desiredMove.magnitude + _baseSkin,
            _collisionMask,
            _triggerInteraction);

        LastCapsulePoint = p1;
        LastHitCount = hitCount;

        if (hitCount == 0)
        {
            horizontalDelta = desiredMove;
            return false;
        }

        horizontalDelta = _mover.ResolveMove(
            desiredMove,
            p1,
            p2,
            radius,
            _hits,
            hitCount,
            _capsule);
        return horizontalDelta.sqrMagnitude <= Mathf.Epsilon;
    }

    Vector3 ApplyTopologyHorizontal(
        Vector3 oldPosition,
        float feetOffset,
        Vector3 horizontalDelta)
    {
        if (_mapCollision == null || horizontalDelta.sqrMagnitude <= Mathf.Epsilon)
            return oldPosition + horizontalDelta;

        Vector3 feetWorld = CharacterFeetPose.GetFeetWorld(oldPosition, feetOffset);
        Vector3 topologyDelta =
            _mapCollision.CollisionResolver.ClampHorizontal(feetWorld, horizontalDelta);
        return oldPosition + topologyDelta;
    }

    MapTopologyDepenetration.PushOutResult ResolveGridStuck(
        ref Vector3 bodyPosition,
        float feetOffset,
        ref MapCollisionGrid.FeetCell feetCell,
        float deltaTime)
    {
        if (_mapCollision == null)
            return MapTopologyDepenetration.PushOutResult.None;

        return _mapCollision.Depenetration.TryResolveGridStuck(
            ref bodyPosition,
            feetOffset,
            ref feetCell,
            ref _gridStuckTracker,
            _topologyPushSpeed,
            _topologyPushMaxIterations,
            deltaTime);
    }

    void ApplyLogicalVertical(
        ref Vector3 worldPosition,
        float feetOffset,
        float deltaTime,
        ref MapCollisionGrid.FeetCell feetCell)
    {
        if (_mapCollision == null)
            return;

        _mapCollision.FloorSupport.ApplyVertical(
            ref worldPosition,
            ref _verticalVelocity,
            deltaTime,
            feetOffset,
            ref feetCell,
            ref _gridStuckTracker,
            _logicalGravity);
    }
}
