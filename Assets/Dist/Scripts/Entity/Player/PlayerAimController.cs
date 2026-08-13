// ============================================================
// PlayerAimController — 마우스 기준 조준 SphereCast로 시야·상호작용 방향을 CharacterState에 전달
// ============================================================
using IsoTilemap;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterState))]
public class PlayerAimController : MonoBehaviour
{
    [SerializeField] private Camera _refCam;
    [SerializeField] private float _sphereRadius = 0.10f;
    [SerializeField] private float _castOriginYOffset = 0.35f;
    [Tooltip("마우스 거리와 무관하게 조준·상호작용 SphereCast가 닿는 최대 거리.")]
    [SerializeField] private float _maxAimDistance = 15f;
    [Tooltip("켜면 조준 월드점 Y를 플레이어 발높이 + Cast Origin Y Offset으로 고정(오클루전·몸 기준 거리와 맞춤).")]
    [SerializeField] private bool _flattenAimYToPlayerHeight = true;
    [Tooltip("조준 중 원점→조준점 기즈모/DrawLine. Config.PlayerSight가 켜져도 표시.")]
    [SerializeField] private bool _drawAimDebug = true;
    [Tooltip("막힘 검사 레이어(플레이어 본체 레이어는 제외하는 것을 권장)")]
    [SerializeField] private LayerMask _aimObstructionMask = ~0;

    private CharacterState _characterState;
    private MapTopologyLineCast _topologyLineCast;
    private bool _isAiming;
    private bool _connected;

    bool ShouldDrawAimDebug =>
        _drawAimDebug || Config.DebugMode.PlayerSight;

    public float CastOriginYOffset => _castOriginYOffset;
    public float SphereRadius => _sphereRadius;
    public float MaxAimDistance => _maxAimDistance;

    void Awake()
    {
        _characterState = GetComponent<CharacterState>();
    }

    public void BindMapCollision(MapTopologyLineCast lineCast) => _topologyLineCast = lineCast;

    public void SetEnabled(bool enabled)
    {
        if (enabled) ConnectController();
        else DisconnectController();
    }

    void ConnectController()
    {
        InputManager input = InputManager.Instance;
        if (input == null || _connected)
            return;

        input.PlayerLookAtPerformed += OnLookAtHoldPerformed;
        input.PlayerLookAtCanceled += OnLookAtCanceled;
        _connected = true;
    }

    void DisconnectController()
    {
        InputManager input = InputManager.Instance;
        if (input != null && _connected)
        {
            input.PlayerLookAtPerformed -= OnLookAtHoldPerformed;
            input.PlayerLookAtCanceled -= OnLookAtCanceled;
        }

        _connected = false;

        if (_isAiming)
        {
            _isAiming = false;
            _characterState.ClearAim();
        }
    }

    void OnLookAtHoldPerformed(InputAction.CallbackContext context)
    {
        _isAiming = true;
    }

    void OnLookAtCanceled(InputAction.CallbackContext context)
    {
        if (!_isAiming)
            return;

        _isAiming = false;
        _characterState.ClearAim();
    }

    void LateUpdate()
    {
        if (!_isAiming || !InputManager.Instance.IsPlayerActionEnabled(PlayerAction.Aim))
            return;

        Camera cam = _refCam != null ? _refCam : Camera.main;
        Vector3 origin = transform.position + Vector3.up * _castOriginYOffset;

        // 마우스 교차 평면을 조준 높이(origin.y)에 맞춤 — 발 평면 교차 후 올리면 아이소에서 커서와 어긋남.
        if (!ScreenRaycaster.TryGetMouseWorldPosition(cam, origin.y, out Vector3 mousePlanePos)) return;

        Vector3 flatTarget = mousePlanePos;
        flatTarget.y = origin.y;

        Vector3 toTarget = flatTarget - origin;
        toTarget.y = 0f;
        float maxDist = Mathf.Min(toTarget.magnitude, _maxAimDistance);
        if (maxDist < 1e-4f) return;
        Vector3 dir = toTarget.normalized;

        if (_topologyLineCast != null)
        {
            Vector3 feetWorld = CharacterFeetPose.GetFeetWorld(transform);
            if (_topologyLineCast.TryGetBlockingDistance(feetWorld, dir, maxDist, out float blockDist))
                maxDist = Mathf.Min(maxDist, blockDist);
        }

        RaycastHit hit = default;
        bool hasHit = Physics.SphereCast(origin, _sphereRadius, dir, out hit, maxDist,
                _aimObstructionMask, QueryTriggerInteraction.Ignore);
        Vector3 aimPoint;
        if (hasHit)
            aimPoint = hit.point;
        else
            aimPoint = origin + dir * maxDist;

        if (_flattenAimYToPlayerHeight)
            aimPoint.y = transform.position.y + _castOriginYOffset;

        Vector3 sightFlat = aimPoint - origin;
        sightFlat.y = 0f;
        if (sightFlat.sqrMagnitude < 1e-4f) return;
        _characterState.SetAimDir(sightFlat.normalized, aimPoint, sightFlat.magnitude);

        if (ShouldDrawAimDebug)
            Debug.DrawLine(origin, aimPoint, Color.red, 0f, false);
    }

    void OnDrawGizmos()
    {
        if (!ShouldDrawAimDebug) return;
        if (_characterState == null && !TryGetComponent(out _characterState)) return;
        if (!_characterState.IsAiming) return;
        Vector3 origin = transform.position + Vector3.up * _castOriginYOffset;
        Vector3 aim = _characterState.AimWorldPoint;
        Gizmos.color = Color.red;
        Gizmos.DrawLine(origin, aim);
        Gizmos.DrawWireSphere(aim, 0.1f);
    }
}
