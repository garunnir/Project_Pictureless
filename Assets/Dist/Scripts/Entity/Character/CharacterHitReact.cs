// ============================================================
// CharacterHitReact — 피격 밀침·Flinch·Δv Stagger·고통 쇼크 애니 큐 (ApplyHit 미구독)
// ============================================================

using Garunnir.Runtime.Gameplay.Data;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterBodyHost))]
[RequireComponent(typeof(CharacterImbalanceHost))]
public sealed class CharacterHitReact : MonoBehaviour
{
    public const string HurtLayerName = "Hurt Layer";
    public const string FlinchLayerName = "Flinch Layer";
    public const string ParamFlinch = "HitFlinch";
    public const string ParamStagger = "HitStagger";
    public const string ParamPainShocked = "IsPainShocked";
    public const string StateEmpty = "Empty";
    public const string StateFlinch = "Flinch";
    public const string StateStagger = "Stagger";
    public const string StatePainDown = "PainDown";
    public const string ClipFlinch = "HitFlinch_Slot";
    public const string ClipStagger = "HitStagger_Slot";
    public const string ClipPainDown = "HitPainDown_Slot";

    CharacterBodyHost _bodyHost;
    CharacterMotor _motor;
    CharacterActionHost _actionHost;
    CharacterAttacker _attacker;
    CharacterAppearanceHost _appearance;
    PlayerGearHost _gear;
    CharacterPainHost _pain;
    CharacterImbalanceHost _imbalance;
    Animator _animator;
    int _hashFlinch;
    int _hashStagger;
    int _hashPainShocked;
    int _hurtLayerIndex = -1;
    int _flinchLayerIndex = -1;
    bool _hasFlinch;
    bool _hasStagger;
    bool _hasPainShocked;

    void Awake()
    {
        _bodyHost = GetComponent<CharacterBodyHost>();
        TryGetComponent(out _motor);
        TryGetComponent(out _actionHost);
        TryGetComponent(out _attacker);
        TryGetComponent(out _appearance);
        TryGetComponent(out _gear);
        TryGetComponent(out _pain);
        TryGetComponent(out _imbalance);
        TryGetComponent(out _animator);
        if (_animator == null)
            _animator = GetComponentInChildren<Animator>();
        CacheHurtParams();
    }

    void OnEnable()
    {
        CharacterAttacker.AnyAttackJudged += OnAnyAttackJudged;
        if (_pain != null)
            _pain.Changed += OnPainChanged;
        SyncPainBool();
    }

    void OnDisable()
    {
        CharacterAttacker.AnyAttackJudged -= OnAnyAttackJudged;
        if (_pain != null)
            _pain.Changed -= OnPainChanged;
    }

    void CacheHurtParams()
    {
        _hasFlinch = false;
        _hasStagger = false;
        _hasPainShocked = false;
        _hurtLayerIndex = -1;
        _flinchLayerIndex = -1;
        if (_animator == null)
            return;

        _hashFlinch = Animator.StringToHash(ParamFlinch);
        _hashStagger = Animator.StringToHash(ParamStagger);
        _hashPainShocked = Animator.StringToHash(ParamPainShocked);
        _hurtLayerIndex = _animator.GetLayerIndex(HurtLayerName);
        _flinchLayerIndex = _animator.GetLayerIndex(FlinchLayerName);

        for (int i = 0; i < _animator.parameterCount; i++)
        {
            AnimatorControllerParameter p = _animator.parameters[i];
            if (p.nameHash == _hashFlinch && p.type == AnimatorControllerParameterType.Trigger)
                _hasFlinch = true;
            else if (p.nameHash == _hashStagger && p.type == AnimatorControllerParameterType.Trigger)
                _hasStagger = true;
            else if (p.nameHash == _hashPainShocked && p.type == AnimatorControllerParameterType.Bool)
                _hasPainShocked = true;
        }
    }

    void OnPainChanged() => SyncPainBool();

    void SyncPainBool()
    {
        if (!_hasPainShocked || _animator == null)
            return;
        bool shocked = _pain != null && _pain.IsPainShocked;
        _animator.SetBool(_hashPainShocked, shocked);
        if (shocked)
            LiftHurtLayer();
    }

    void OnAnyAttackJudged(AttackOutcome outcome)
    {
        if (!outcome.DidHit || outcome.Target != _bodyHost)
            return;

        ICharacterBody body = _bodyHost != null ? _bodyHost.Body : null;
        if (body == null || body.IsDeadState)
            return;

        bool shocked = _pain != null && _pain.IsPainShocked;
        if (!shocked)
            PlayFlinch();

        float mass = CombatImpulse.InertialMassKg(
            _appearance,
            _gear != null ? _gear.Wear : null,
            _gear != null ? _gear.Wield : null);
        float dv = CombatImpulse.VictimDeltaV(outcome.ImpulseJin, mass);
        if (dv > 0.001f && _motor != null)
        {
            Vector3 dir = outcome.Direction;
            dir.y = 0f;
            if (dir.sqrMagnitude > 1e-6f)
                _motor.ApplyKnockback(dir.normalized * dv);
        }

        bool fell = false;
        if (_imbalance != null)
            fell = _imbalance.ApplyHit(dv);
        else if (dv >= CombatImpulse.StaggerDeltaV)
            fell = true;

        if (fell)
        {
            if (_imbalance != null)
                _imbalance.NotifyFallen();
            else
            {
                _actionHost?.CancelAll();
                _attacker?.CancelAllPendingCues();
            }

            if (!shocked)
                PlayStagger();
        }

        _pain?.Refresh();
    }

    void PlayFlinch()
    {
        if (!_hasFlinch || _animator == null)
            return;
        _animator.ResetTrigger(_hashFlinch);
        _animator.SetTrigger(_hashFlinch);
        LiftFlinchLayer();
    }

    void PlayStagger()
    {
        if (!_hasStagger || _animator == null)
            return;
        _animator.ResetTrigger(_hashStagger);
        _animator.SetTrigger(_hashStagger);
        LiftHurtLayer();
    }

    void LiftFlinchLayer()
    {
        if (_flinchLayerIndex < 0 || _animator == null)
            return;
        _animator.SetLayerWeight(_flinchLayerIndex, 1f);
    }

    void LiftHurtLayer()
    {
        if (_hurtLayerIndex < 0 || _animator == null)
            return;
        _animator.SetLayerWeight(_hurtLayerIndex, 1f);
    }
}
