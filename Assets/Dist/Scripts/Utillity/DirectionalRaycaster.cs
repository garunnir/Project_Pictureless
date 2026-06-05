using IsoTilemap;
using UnityEngine;

public class DirectionalRaycaster : MonoBehaviour
{
    [SerializeField] private LayerMask _interactableMask = ~0;

    MapTopologyLineCast _topologyLineCast;
    FloorBandResolver _bandResolver;
    CharacterState _characterState;

    private Vector3 _lastOrigin;
    private Vector3 _lastDirection;
    private float _lastRadius;
    private float _lastDistance;

    public void BindMapCollision(MapTopologyLineCast lineCast, FloorBandResolver bandResolver, CharacterState characterState)
    {
        _topologyLineCast = lineCast;
        _bandResolver = bandResolver;
        _characterState = characterState;
    }

    public bool TrySphereCast(
        Vector3 origin,
        Vector3 direction,
        float radius,
        float maxDistance,
        out RaycastHit hit)
    {
        if (direction == Vector3.zero || maxDistance <= 1e-4f)
        {
            hit = default;
            return false;
        }

        _lastOrigin = origin;
        _lastDirection = direction;
        _lastRadius = radius;
        _lastDistance = maxDistance;

        float clippedDistance = maxDistance;
        if (_topologyLineCast != null && _bandResolver != null && _characterState != null)
        {
            int band = _bandResolver.Resolve(_characterState.BodyWorldPoint.y);
            if (_topologyLineCast.TryGetBlockingDistance(origin, direction, maxDistance, band, out float blockDist))
                clippedDistance = Mathf.Min(maxDistance, blockDist);
        }

        return Physics.SphereCast(
            origin,
            radius,
            direction.normalized,
            out hit,
            clippedDistance,
            _interactableMask,
            QueryTriggerInteraction.Ignore);
    }

    private void OnDrawGizmosSelected()
    {
        if (_lastDirection == Vector3.zero || _lastDistance <= 1e-4f) return;

        Gizmos.color = Color.green;
        Gizmos.DrawRay(_lastOrigin, _lastDirection.normalized * _lastDistance);
        Gizmos.DrawWireSphere(_lastOrigin + _lastDirection.normalized * _lastDistance, _lastRadius);
    }
}
