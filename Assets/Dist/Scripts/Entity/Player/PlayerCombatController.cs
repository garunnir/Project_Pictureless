// ============================================================
// PlayerCombatController — 조준(RMB) 중 LMB 시전 + 액션 선택
// ============================================================

using System.Collections.Generic;
using Garunnir.Runtime.Gameplay.Data;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterAttacker))]
[RequireComponent(typeof(CharacterState))]
[RequireComponent(typeof(PlayerAimController))]
[RequireComponent(typeof(DualWieldAttackDriver))]
public sealed class PlayerCombatController : MonoBehaviour
{
    const int PhysicsHitBufferSize = 16;

    [Tooltip("조준점이 적 콜라이더를 빗나갔을 때, AimWorldPoint 주변으로 후보를 찾는 반경.")]
    [SerializeField, Min(0.05f)] float _aimPointSoftRadius = 0.75f;

    CharacterAttacker _attacker;
    CharacterState _characterState;
    PlayerAimController _aimController;
    DualWieldAttackDriver _dualDriver;
    readonly RaycastHit[] _hits = new RaycastHit[PhysicsHitBufferSize];
    readonly List<RaycastResult> _uiRaycastResults = new();
    bool _connected;

    void Awake()
    {
        _attacker = GetComponent<CharacterAttacker>();
        _characterState = GetComponent<CharacterState>();
        _aimController = GetComponent<PlayerAimController>();
        _dualDriver = GetComponent<DualWieldAttackDriver>();
    }

    void OnDisable() => DisconnectInput();

    /// <summary>PlayerController.SetControlEnabled 경로 — 조준/이동과 동일 소유권.</summary>
    public void SetEnabled(bool enabled)
    {
        if (enabled)
            ConnectInput();
        else
            DisconnectInput();
    }

    void ConnectInput()
    {
        InputManager input = InputManager.Instance;
        if (input == null || _connected)
            return;

        input.PlayerCombatCyclePerformed += OnCombatCycle;
        input.PlayerCombatAttackPerformed += OnCombatAttack;
        input.PlayerCombatSelectBashingPerformed += OnSelectBashing;
        input.PlayerCombatSelectCuttingPerformed += OnSelectCutting;
        input.PlayerCombatSelectGunPerformed += OnSelectGun;
        _connected = true;
    }

    void DisconnectInput()
    {
        InputManager input = InputManager.Instance;
        if (input != null && _connected)
        {
            input.PlayerCombatCyclePerformed -= OnCombatCycle;
            input.PlayerCombatAttackPerformed -= OnCombatAttack;
            input.PlayerCombatSelectBashingPerformed -= OnSelectBashing;
            input.PlayerCombatSelectCuttingPerformed -= OnSelectCutting;
            input.PlayerCombatSelectGunPerformed -= OnSelectGun;
        }

        _connected = false;
    }

    void OnCombatCycle(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;
        _attacker.CycleSelectedAction();
    }

    void OnCombatAttack(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        // RMB Hold 조준이 켜진 동안에만 시전.
        if (_characterState == null || !_characterState.IsAiming)
            return;

        InputManager input = InputManager.Instance;
        if (input != null &&
            input.TryReadPointerScreenPosition(out Vector2 screenPos) &&
            IsPointerBlockedByUiAt(screenPos))
        {
            return;
        }

        if (!TryResolveAimedTarget(out CharacterBodyHost target))
            return;

        if (_dualDriver != null && _dualDriver.TryPerformDual(target))
            return;

        _attacker.TryPerformSelected(target);
    }

    void OnSelectBashing(InputAction.CallbackContext context)
    {
        if (context.performed)
            _attacker.TrySelectAction(WeaponAction.Bashing);
    }

    void OnSelectCutting(InputAction.CallbackContext context)
    {
        if (context.performed)
            _attacker.TrySelectAction(WeaponAction.Cutting);
    }

    void OnSelectGun(InputAction.CallbackContext context)
    {
        if (context.performed)
            _attacker.TrySelectAction(WeaponAction.Gun);
    }

    bool TryResolveAimedTarget(out CharacterBodyHost target)
    {
        target = null;

        Vector3 origin = transform.position + Vector3.up * _aimController.CastOriginYOffset;
        Vector3 direction = _characterState.SightDir;
        if (direction.sqrMagnitude < 1e-6f)
            direction = _characterState.InteractionDir;
        if (direction.sqrMagnitude < 1e-6f)
            return false;

        direction.Normalize();

        float maxDistance = Mathf.Max(
            _characterState.InteractionReach,
            _aimController.MaxAimDistance);
        string itemId = _attacker.ItemId;
        ItemData item = string.IsNullOrEmpty(itemId)
            ? null
            : GameplayData.GetItem(itemId);
        float actionRange = CombatMath.RangeMeters(item, _attacker.SelectedAction);
        maxDistance = Mathf.Min(maxDistance, Mathf.Max(actionRange, 0.01f));

        if (TrySphereCastBody(origin, direction, maxDistance, out target))
            return true;

        return TryNearestBodyToAimPoint(maxDistance, out target);
    }

    bool TrySphereCastBody(
        Vector3 origin,
        Vector3 direction,
        float maxDistance,
        out CharacterBodyHost target)
    {
        target = null;
        float radius = _aimController.SphereRadius;
        int count = Physics.SphereCastNonAlloc(
            origin,
            radius,
            direction,
            _hits,
            maxDistance,
            ~0,
            QueryTriggerInteraction.Ignore);

        float bestDistance = float.MaxValue;
        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = _hits[i];
            if (hit.collider == null)
                continue;

            CharacterBodyHost host = hit.collider.GetComponentInParent<CharacterBodyHost>();
            if (!IsValidHostile(host))
                continue;

            if (hit.distance >= bestDistance)
                continue;

            bestDistance = hit.distance;
            target = host;
        }

        return target != null;
    }

    bool TryNearestBodyToAimPoint(float maxWeaponDistance, out CharacterBodyHost target)
    {
        target = null;
        Vector3 aimPoint = _characterState.AimWorldPoint;
        if (aimPoint.sqrMagnitude < 1e-6f)
            return false;

        CharacterBodyHost[] hosts = FindObjectsByType<CharacterBodyHost>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        float bestSqr = _aimPointSoftRadius * _aimPointSoftRadius;
        float maxWeaponSqr = maxWeaponDistance * maxWeaponDistance;
        Vector3 self = transform.position;
        for (int i = 0; i < hosts.Length; i++)
        {
            CharacterBodyHost host = hosts[i];
            if (!IsValidHostile(host))
                continue;

            Collider col = host.GetComponentInChildren<Collider>();
            Vector3 center = col != null ? col.bounds.center : host.transform.position;

            Vector3 fromSelf = center - self;
            fromSelf.y = 0f;
            if (fromSelf.sqrMagnitude > maxWeaponSqr)
                continue;

            float sqr = (center - aimPoint).sqrMagnitude;
            if (sqr >= bestSqr)
                continue;

            bestSqr = sqr;
            target = host;
        }

        return target != null;
    }

    bool IsValidHostile(CharacterBodyHost host)
    {
        if (host == null || host.transform == transform)
            return false;
        if (host.GetComponent<PlayerCombatController>() != null)
            return false;
        if (host.Body == null || host.Body.IsDeadState)
            return false;
        return true;
    }

    /// <summary>
    /// GraphicRaycaster 히트만 UI로 본다. PhysicsRaycaster 월드 히트는 차단하지 않는다.
    /// </summary>
    bool IsPointerBlockedByUiAt(Vector2 screenPosition)
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
            return false;

        var pointerData = new PointerEventData(eventSystem) { position = screenPosition };
        _uiRaycastResults.Clear();
        eventSystem.RaycastAll(pointerData, _uiRaycastResults);

        for (int i = 0; i < _uiRaycastResults.Count; i++)
        {
            if (_uiRaycastResults[i].module is GraphicRaycaster)
                return true;
        }

        return false;
    }
}
