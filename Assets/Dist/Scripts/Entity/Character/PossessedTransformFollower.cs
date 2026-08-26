// ============================================================
// PossessedTransformFollower — 시스템 리그가 possessed 위치·시야 yaw를 따라감
// ============================================================

using UnityEngine;

/// <summary>
/// PlayerSight 등 시스템 리그용. 위치는 body, yaw는 <see cref="CharacterState.GetFacingDir"/>.
/// Spot Light GO에는 컴포넌트를 두지 않는다 — 루트에서만 갱신.
/// </summary>
[DisallowMultipleComponent]
public sealed class PossessedTransformFollower : MonoBehaviour
{
    [SerializeField] PlayerPossessedInputHost _host;

    void LateUpdate()
    {
        Transform target = _host != null ? _host.BodyTransform : null;
        if (target == null)
            return;

        transform.position = target.position;

        CharacterState state = _host.BodyState;
        if (state != null)
            PlayerSightVisionBinder.ApplyFacingYaw(state);
        else
            transform.rotation = target.rotation;
    }
}
