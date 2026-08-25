// ============================================================
// CharacterArriveHost — NpcSteer 목표 도착 (possessed·NPC 공용)
// ============================================================

using System;
using UnityEngine;

[DefaultExecutionOrder(-45)]
[DisallowMultipleComponent]
public sealed class CharacterArriveHost : MonoBehaviour
{
    CharacterMotor _motor;
    CharacterActionHost _actionHost;
    bool _active;
    bool _suppressInput;
    Vector3 _destination;
    float _stoppingDistance;
    Action _onArrived;
    Action _onCancelled;

    public bool IsBusy => _active;

    void Awake()
    {
        TryGetComponent(out _motor);
        TryGetComponent(out _actionHost);
    }

    void FixedUpdate()
    {
        if (!_active || _motor == null)
            return;

        if (NpcSteer.TryArriveOrSteer(
                _motor,
                _motor.transform.position,
                _destination,
                _stoppingDistance))
        {
            CompleteArrive();
        }
    }

    void OnDisable() => CancelInternal(invokeCancelled: false);

    public bool TryRunOrEnqueue(
        Vector3 destination,
        float stoppingDistance,
        Action onArrived,
        Action onCancelled = null,
        bool suppressInput = true)
    {
        if (_motor == null || onArrived == null)
            return false;

        if (_actionHost == null)
            return TryBegin(destination, stoppingDistance, onArrived, onCancelled, suppressInput);

        return _actionHost.TryRunOrEnqueue(
            CharacterActionKind.Map,
            () => TryBegin(destination, stoppingDistance, onArrived, onCancelled, suppressInput));
    }

    public bool TryBegin(
        Vector3 destination,
        float stoppingDistance,
        Action onArrived,
        Action onCancelled = null,
        bool suppressInput = true)
    {
        if (_active || _motor == null || onArrived == null)
            return false;

        _destination = destination;
        _stoppingDistance = Mathf.Max(0f, stoppingDistance);
        _onArrived = onArrived;
        _onCancelled = onCancelled;
        _suppressInput = suppressInput;

        _motor.BeginScriptedLocomotion();
        if (_suppressInput && _motor.IsPossessed)
            SetScriptedInput(false);

        _active = true;
        return true;
    }

    public void Cancel()
    {
        if (!_active)
            return;

        CancelInternal(invokeCancelled: true);
    }

    void CompleteArrive()
    {
        if (!_active)
            return;

        Action arrived = _onArrived;
        EndSession();
        arrived?.Invoke();
    }

    void CancelInternal(bool invokeCancelled)
    {
        if (!_active)
            return;

        Action cancelled = invokeCancelled ? _onCancelled : null;
        EndSession();
        cancelled?.Invoke();
    }

    void EndSession()
    {
        _active = false;
        _onArrived = null;
        _onCancelled = null;

        if (_motor != null)
        {
            NpcSteer.Stop(_motor);
            _motor.EndScriptedLocomotion();
        }

        if (_suppressInput)
            SetScriptedInput(true);

        _suppressInput = false;
    }

    static void SetScriptedInput(bool allowPlayerInput)
    {
        PlayerPossessedInputHost input = FindFirstObjectByType<PlayerPossessedInputHost>();
        input?.SetScriptedLocomotionInput(allowPlayerInput);
    }
}
