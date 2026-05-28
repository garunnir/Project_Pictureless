// ============================================================
// PlayerMovementDebugController — PlayerMovement 디버그 로그/기즈모 출력을 전담하는 컴포넌트
// ============================================================
using UnityEngine;

[RequireComponent(typeof(PlayerMovement))]
public class PlayerMovementDebugController : MonoBehaviour, IPlayerMovementDebug
{
    [SerializeField] private PlayerMovement _movement;

    private static readonly Color WalkCapsuleColor = new Color(0f, 0.6f, 1f, 0.25f);
    private static readonly Color SprintCapsuleColor = new Color(1f, 0.65f, 0f, 0.25f);
    private static readonly Color InertiaCapsuleColor = new Color(0.8f, 0.3f, 1f, 0.35f);
    private static readonly Color CastColor = Color.yellow;
    private static readonly Color HitColor = Color.red;
    private static readonly Color HitAltColor = new Color(1f, 0.5f, 0.2f, 1f);
    private static readonly Color SlideColor = Color.green;

#if UNITY_EDITOR
    private static readonly System.Type DebugLogControllerType =
        System.Type.GetType("DebugLogController, Assembly-CSharp-Editor");
#endif

    private void Awake()
    {
        if (_movement == null) TryGetComponent(out _movement);
    }

    public void LogPlayerRun(bool isRun)
    {
#if UNITY_EDITOR
        InvokeDebugLogController("LogPlayerRun", isRun);
#endif
    }

    public void LogPlayerStuck()
    {
#if UNITY_EDITOR
        InvokeDebugLogController("LogPlayerStuck");
#endif
    }

    public void LogPlayerSliding(float lastSlideSqrMagnitude)
    {
#if UNITY_EDITOR
        InvokeDebugLogController("LogPlayerSliding", lastSlideSqrMagnitude);
#endif
    }

    private void OnDrawGizmos()
    {
        if (!Config.DebugMode.PlayerMovement) return;
        if (_movement == null && !TryGetComponent(out _movement)) return;

        CapsuleCollider capsule = _movement.Capsule;
        if (capsule == null) return;

        Transform cachedTransform = _movement.transform;
        float scale = Mathf.Max(cachedTransform.lossyScale.x, cachedTransform.lossyScale.y);
        float radius = capsule.radius * scale;
        Vector3 worldCenter = cachedTransform.TransformPoint(capsule.center);
        Vector3 up = cachedTransform.up;
        float halfHeight = Mathf.Max(0f, (capsule.height * 0.5f) - capsule.radius);
        Vector3 cp1 = worldCenter + up * halfHeight;
        Vector3 cp2 = worldCenter - up * halfHeight;

        Gizmos.color = _movement.IsInertiaActive
            ? InertiaCapsuleColor
            : (_movement.IsSprinting ? SprintCapsuleColor : WalkCapsuleColor);
        Gizmos.DrawWireSphere(cp1, radius);
        Gizmos.DrawWireSphere(cp2, radius);
        Gizmos.DrawLine(cp1 + cachedTransform.right * capsule.radius, cp2 + cachedTransform.right * capsule.radius);
        Gizmos.DrawLine(cp1 - cachedTransform.right * capsule.radius, cp2 - cachedTransform.right * capsule.radius);
        Gizmos.color = Color.skyBlue;
        Gizmos.DrawWireSphere(cachedTransform.position + _movement.LastDesiredMove, 0.01f);
        Gizmos.color = _movement.IsInertiaActive ? InertiaCapsuleColor : Color.gray;
        Gizmos.DrawWireSphere(cachedTransform.position, Mathf.Clamp(_movement.CurrentSpeed * 0.05f, 0.05f, 0.4f));

        if (_movement.LastHitCount <= 0) return;

        Gizmos.color = CastColor;
        Gizmos.DrawLine(
            _movement.LastP1,
            _movement.LastP1 + _movement.LastDesiredMove.normalized * (_movement.LastDesiredMove.magnitude + _movement.BaseSkin));

        RaycastHit[] hits = _movement.Hits;
        int hitCount = _movement.LastHitCount;
        int lastNearestIndex = _movement.LastNearestIndex;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = hits[i];
            if (hit.collider == null) continue;
            Gizmos.color = (i == lastNearestIndex) ? HitColor : HitAltColor;
            Gizmos.DrawSphere(hit.point, 0.05f);
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(hit.point, hit.point + hit.normal * 0.5f);
        }

        if (_movement.LastSlide.sqrMagnitude <= Mathf.Epsilon) return;
        Gizmos.color = SlideColor;
        Gizmos.DrawLine(cachedTransform.position, cachedTransform.position + _movement.LastSlide);
    }

#if UNITY_EDITOR
    private static void InvokeDebugLogController(string methodName, object parameter = null)
    {
        if (DebugLogControllerType == null) return;

        var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static;
        var method = parameter == null
            ? DebugLogControllerType.GetMethod(methodName, flags, null, System.Type.EmptyTypes, null)
            : DebugLogControllerType.GetMethod(methodName, flags, null, new[] { parameter.GetType() }, null);

        if (method == null) return;
        if (parameter == null) method.Invoke(null, null);
        else method.Invoke(null, new[] { parameter });
    }
#endif
}
