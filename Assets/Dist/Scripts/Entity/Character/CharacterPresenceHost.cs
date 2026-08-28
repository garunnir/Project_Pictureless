// ============================================================
// CharacterPresenceHost — 타인 탐지용 가시성·소음 스탯 SSOT (본체 공용)
// ============================================================

using UnityEngine;

[DisallowMultipleComponent]
public sealed class CharacterPresenceHost : MonoBehaviour, ICharacterPresence
{
    [SerializeField] CharacterPresenceSettings _settings = CharacterPresenceSettings.DefaultUnity;

    CharacterState _state;
    CharacterMotor _motor;
    CharacterPresenceResolved _resolved = CharacterPresenceResolved.Identity;

    public float Visibility01 => _resolved.Visibility01;
    public float Noise01 => _resolved.Noise01;
    public CharacterPresenceResolved Resolved => _resolved;

    void Awake()
    {
        TryGetComponent(out _state);
        TryGetComponent(out _motor);
    }

    void OnEnable()
    {
        if (_state != null)
            _state.StealthChanged += OnStealthChanged;
        Refresh();
    }

    void OnDisable()
    {
        if (_state != null)
            _state.StealthChanged -= OnStealthChanged;
    }

    void LateUpdate() => Refresh();

    void OnStealthChanged(bool _) => Refresh();

    void Refresh()
    {
        if (_state == null)
            TryGetComponent(out _state);
        if (_motor == null)
            TryGetComponent(out _motor);

        var ctx = new CharacterPresenceContext
        {
            IsStealthActive = _state != null && _state.IsStealth,
            CurrentSpeed = _motor != null ? _motor.CurrentSpeed : 0f,
            IsSprinting = _motor != null && _motor.IsSprinting,
            NoiseReferenceSpeed = _settings.NoiseReferenceSpeed,
            BodyScale01 = 1f,
            Transparency01 = 1f,
        };
        _resolved = CharacterPresenceResolved.Evaluate(in ctx, in _settings);
    }

    public static bool TryResolve(Component target, out CharacterPresenceResolved resolved)
    {
        if (target != null &&
            target.TryGetComponent(out CharacterPresenceHost host))
        {
            resolved = host.Resolved;
            return true;
        }

        resolved = CharacterPresenceResolved.Identity;
        return false;
    }

    public static float ResolveVisibility01(Component target) =>
        TryResolve(target, out CharacterPresenceResolved resolved)
            ? resolved.Visibility01
            : 1f;

    public static float ResolveNoise01(Component target) =>
        TryResolve(target, out CharacterPresenceResolved resolved)
            ? resolved.Noise01
            : 1f;
}
