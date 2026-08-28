// ============================================================
// PlayerStealthController — C 키 토글 은신 액션
// ============================================================

using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public sealed class PlayerStealthController : MonoBehaviour
{
    CharacterState _characterState;
    PlayerMovement _movement;
    bool _connected;

    public bool IsStealth => _characterState != null && _characterState.IsStealth;

    public void BindBody(CharacterState characterState, PlayerMovement movement)
    {
        _characterState = characterState;
        _movement = movement;
    }

    void OnDisable() => DisconnectInput();

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

        input.PlayerStealthTogglePerformed += OnStealthToggle;
        _connected = true;
    }

    void DisconnectInput()
    {
        InputManager input = InputManager.Instance;
        if (input == null || !_connected)
            return;

        input.PlayerStealthTogglePerformed -= OnStealthToggle;
        _connected = false;
    }

    void OnStealthToggle(InputAction.CallbackContext context)
    {
        if (!context.performed || _characterState == null)
            return;

        bool next = !_characterState.IsStealth;
        _characterState.SetStealth(next);
        _movement?.SetStealthMovement(next);
        if (next)
            _movement?.CancelSprintForStealth();
    }
}
