// ============================================================
// NpcSteerToPoint — NPC를 Transform 또는 월드 목표점 방향으로 조향
// ============================================================

using UnityEngine;

[DefaultExecutionOrder(-1)]
[DisallowMultipleComponent]
[RequireComponent(typeof(NpcMovement))]
public sealed class NpcSteerToPoint : MonoBehaviour
{
    [SerializeField] bool _hasDestination;
    [SerializeField] Vector3 _destination;
    [SerializeField] Transform _target;
    [SerializeField, Min(0f)] float _stoppingDistance = 0.1f;

    NpcMovement _movement;

    public bool HasDestination => _hasDestination;
    public bool IsArrived { get; private set; }
    public Vector3 Destination =>
        _target != null ? _target.position : _destination;

    void Awake()
    {
        _movement = GetComponent<NpcMovement>();
    }

    void FixedUpdate()
    {
        if (!_hasDestination)
        {
            StopMovement();
            return;
        }

        Vector3 offset = Destination - transform.position;
        offset.y = 0f;

        float stoppingDistanceSqr = _stoppingDistance * _stoppingDistance;
        if (offset.sqrMagnitude <= stoppingDistanceSqr)
        {
            IsArrived = true;
            StopMovement();
            return;
        }

        IsArrived = false;
        _movement.SetDesiredWorldDir(offset.normalized);
        _movement.SetTravelLimit(offset.magnitude - _stoppingDistance);
    }

    void OnDisable()
    {
        StopMovement();
    }

    public void SetTarget(Transform target)
    {
        if (target == null)
        {
            ClearDestination();
            return;
        }

        _target = target;
        _hasDestination = true;
        IsArrived = false;
    }

    public void SetDestination(Vector3 worldPosition)
    {
        _target = null;
        _destination = worldPosition;
        _hasDestination = true;
        IsArrived = false;
    }

    public void ClearDestination()
    {
        _target = null;
        _hasDestination = false;
        IsArrived = false;
        StopMovement();
    }

    void StopMovement()
    {
        if (_movement == null)
            return;

        _movement.SetDesiredWorldDir(Vector3.zero);
        _movement.ClearTravelLimit();
    }
}
