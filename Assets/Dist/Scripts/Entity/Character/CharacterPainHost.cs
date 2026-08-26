// ============================================================
// CharacterPainHost — PainTotal·고통 쇼크 플래그 (Defeat/Dead 아님)
// ============================================================

using System;
using System.Collections.Generic;
using Garunnir.Runtime.Gameplay.Data;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterBodyHost))]
public sealed class CharacterPainHost : MonoBehaviour
{
    readonly List<BodyPartEffect> _effectScratch = new(16);

    CharacterBodyHost _bodyHost;
    CharacterMotor _motor;
    CharacterActionHost _actionHost;
    CharacterAttacker _attacker;
    ICharacterBody _subscribed;
    bool _painShocked;
    bool _painLatched;
    float _lastEffective;

    public event Action Changed;

    public bool IsPainShocked => _painShocked;
    public float EffectivePain01 => _lastEffective;

    void Awake()
    {
        _bodyHost = GetComponent<CharacterBodyHost>();
        TryGetComponent(out _motor);
        TryGetComponent(out _actionHost);
        TryGetComponent(out _attacker);
    }

    void OnEnable()
    {
        BindBody();
        Refresh();
    }

    void OnDisable()
    {
        UnbindBody();
        ReleasePossessedInputPolicy();
    }

    /// <summary>
    /// possessed 전환 후 호출. 다운 중이면 <see cref="InputManager"/> Move/Aim 억제를 맞춘다.
    /// </summary>
    public void SyncPossessedInputPolicy()
    {
        InputManager input = InputManager.Instance;
        if (input == null)
            return;

        bool suppress = _painShocked && _motor != null && _motor.IsPossessed;
        input.SuppressPlayerAction(PlayerAction.Move, this, suppress);
        input.SuppressPlayerAction(PlayerAction.Aim, this, suppress);
    }

    void ReleasePossessedInputPolicy()
    {
        InputManager input = InputManager.Instance;
        if (input == null)
            return;

        input.SuppressPlayerAction(PlayerAction.Move, this, false);
        input.SuppressPlayerAction(PlayerAction.Aim, this, false);
    }

    void BindBody()
    {
        UnbindBody();
        _subscribed = _bodyHost != null ? _bodyHost.Body : null;
        if (_subscribed != null)
            _subscribed.Changed += OnBodyChanged;
    }

    void UnbindBody()
    {
        if (_subscribed != null)
            _subscribed.Changed -= OnBodyChanged;
        _subscribed = null;
    }

    void OnBodyChanged() => Refresh();

    public void Refresh()
    {
        ICharacterBody body = _bodyHost != null ? _bodyHost.Body : null;
        if (body == null || body.IsDeadState)
        {
            _painLatched = false;
            SetShocked(false);
            _lastEffective = 0f;
            return;
        }

        _lastEffective = CombatPain.EffectivePain01(body, _effectScratch);
        bool painDown = CombatPain.IsPainDown(_lastEffective, _painLatched);
        _painLatched = painDown;
        bool shocked = painDown || BodyCapacity.IsCapacityDowned(body);
        SetShocked(shocked);
    }

    void SetShocked(bool shocked)
    {
        if (_painShocked == shocked)
            return;

        _painShocked = shocked;
        _motor?.SetMoveLocked(shocked);
        SyncPossessedInputPolicy();
        if (shocked)
        {
            _actionHost?.CancelAll();
            _attacker?.CancelAllPendingCues();
        }

        Changed?.Invoke();
    }
}
