// ============================================================
// PlayerCombatController — 조준(RMB) 중 LMB 시전 + 액션 선택
// ============================================================

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class PlayerCombatController : MonoBehaviour
{
    CharacterAttacker _attacker;
    CharacterState _characterState;
    CharacterActionHost _actionHost;
    readonly List<RaycastResult> _uiRaycastResults = new();
    bool _connected;

    public void BindBody(
        CharacterAttacker attacker,
        CharacterState characterState,
        CharacterActionHost actionHost)
    {
        _attacker = attacker;
        _characterState = characterState;
        _actionHost = actionHost;
    }

    void Awake()
    {
        _attacker = GetComponent<CharacterAttacker>();
        _characterState = GetComponent<CharacterState>();
        TryGetComponent(out _actionHost);
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
        _connected = true;
    }

    void DisconnectInput()
    {
        InputManager input = InputManager.Instance;
        if (input != null && _connected)
        {
            input.PlayerCombatCyclePerformed -= OnCombatCycle;
            input.PlayerCombatAttackPerformed -= OnCombatAttack;
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

        if (_actionHost != null)
        {
            _actionHost.TryRunOrEnqueue(CharacterActionKind.Combat, ExecuteAttack);
            return;
        }

        ExecuteAttack();
    }

    bool ExecuteAttack()
    {
        if (_attacker == null)
            return false;
        _attacker.TryPerformSelected(null);
        return _attacker.IsActionBusy;
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
