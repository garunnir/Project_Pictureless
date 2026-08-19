// ============================================================
// PossessedTransformFollower — 시스템 리그가 possessed 트랜스폼을 따라감
// ============================================================

using UnityEngine;

[DisallowMultipleComponent]
public sealed class PossessedTransformFollower : MonoBehaviour
{
    [SerializeField] PlayerPossessedInputHost _host;

    void LateUpdate()
    {
        Transform target = _host != null ? _host.BodyTransform : null;
        if (target == null)
            return;

        transform.SetPositionAndRotation(target.position, target.rotation);
    }
}
