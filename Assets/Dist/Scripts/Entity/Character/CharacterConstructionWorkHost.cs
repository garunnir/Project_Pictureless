// ============================================================
// CharacterConstructionWorkHost — 건설 Work 타이머
// ============================================================

using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class CharacterConstructionWorkHost : MonoBehaviour
{
    float _elapsed;
    float _duration;
    Action _onComplete;
    bool _running;

    public bool IsBusy => _running;
    public float Progress01 =>
        _duration <= 0f ? 1f : Mathf.Clamp01(_elapsed / _duration);

    void Update()
    {
        if (!_running || _duration <= 0f)
            return;

        float dt = TimeScaleService.Delta(
            TryGetComponent(out CharacterMotor motor) && motor.IsPossessed
                ? TimeScaleChannel.Player
                : TimeScaleChannel.World);
        CharacterActionHost action = GetComponent<CharacterActionHost>();
        if (action != null)
            dt *= action.ActionTickScale;

        _elapsed += dt;
        if (_elapsed < _duration)
            return;

        Finish();
    }

    public bool TryBegin(float durationSeconds, Action onComplete)
    {
        if (onComplete == null || _running)
            return false;

        _onComplete = onComplete;
        _elapsed = 0f;
        _duration = Mathf.Max(0f, durationSeconds);
        _running = true;

        if (_duration <= 0f)
            Finish();

        return true;
    }

    public void Cancel()
    {
        if (!_running)
            return;

        _running = false;
        _onComplete = null;
        _elapsed = 0f;
        _duration = 0f;
    }

    void Finish()
    {
        Action complete = _onComplete;
        _running = false;
        _onComplete = null;
        _elapsed = 0f;
        _duration = 0f;
        complete?.Invoke();
    }
}
