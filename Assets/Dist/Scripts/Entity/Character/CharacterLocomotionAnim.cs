// ============================================================
// CharacterLocomotionAnim — 3D Mecanim에 Speed/IsAiming을 넣고 TimeScale 채널로 진행
// ============================================================
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// Drives a 3D layered Animator from <see cref="PlayerMovement"/> speed and
/// <see cref="CharacterState.IsAiming"/>. Animation time advances via
/// <see cref="TimeScaleService"/> only (Animator auto-tick disabled).
/// Optional pose rate quantizes ticks for a flipbook look without stepped clips.
/// </summary>
[RequireComponent(typeof(CharacterState))]
public class CharacterLocomotionAnim : MonoBehaviour
{
    const string ManualTickHelp =
        "Play 시 Animator 컴포넌트를 끕니다(TimeScale 채널 수동 틱). " +
        "Inspector에서 Animator.enabled가 꺼져 보이는 것은 정상입니다. " +
        "재생은 이 스크립트의 Update → Animator.Update(TimeScaleService.Delta)로만 진행됩니다.";

    const float DefaultPoseRate = 10f;
    const int MaxPoseStepsPerFrame = 8;

    [InfoBox(ManualTickHelp, InfoMessageType.Warning)]
    [Tooltip("애니 진행에 사용할 시간 채널. 플레이어=Player, NPC·환경=World.")]
    [SerializeField] TimeScaleChannel _timeChannel = TimeScaleChannel.Player;

    [Header("Animator (TimeScale manual tick)")]
    [Tooltip(ManualTickHelp)]
    [SerializeField] Animator _animator;
    [SerializeField] string _paramSpeed = "Speed";
    [SerializeField] string _paramAiming = "IsAiming";
    [Tooltip("초당 애니 포즈 수(채널 시간 기준). 0이면 매 프레임 연속 틱. BlendTree 유지한 채 플립북 느낌.")]
    [SerializeField, Min(0f)] float _poseRate = DefaultPoseRate;
    [ShowInInspector, ReadOnly, PropertyOrder(20)]
    [LabelText("Manual tick active (Animator.enabled forced off)")]
    bool ManualTickActive => _manualControl && _animator != null && !_animator.enabled;

    CharacterState _characterState;
    PlayerMovement _playerMovement;
    bool _manualControl;
    bool _pendingBind = true;
    float _poseAccum;

    int _hashSpeed;
    int _hashAiming;
    bool _hasSpeed;
    bool _hasAiming;

    public Animator Animator => _animator;

    void Awake()
    {
        _characterState = GetComponentInParent<CharacterState>();
        if (_characterState == null)
            _characterState = GetComponent<CharacterState>();

        _playerMovement = GetComponentInParent<PlayerMovement>();
        if (_playerMovement == null)
            _playerMovement = GetComponent<PlayerMovement>();

        if (_animator == null)
            _animator = GetComponentInChildren<Animator>();

        CacheAnimatorParameters();
    }

    void Reset()
    {
        if (_animator == null)
            _animator = GetComponentInChildren<Animator>();
    }

    void OnValidate()
    {
        if (_poseRate < 0f)
            _poseRate = 0f;

        if (_animator != null)
            CacheAnimatorParameters();
    }

    void Update()
    {
        if (_animator == null)
            return;

        // Avatar/SkinnedMesh 준비가 끝난 첫 Update에서 bind (Awake/Start Rebind는 종종 no-op).
        if (_pendingBind || !_manualControl)
        {
            TakeManualControl();
            _pendingBind = false;
        }

        if (_hasSpeed)
            _animator.SetFloat(_hashSpeed, ResolveNormalizedSpeed());

        if (_hasAiming)
            _animator.SetBool(_hashAiming, _characterState != null && _characterState.IsAiming);

        AdvanceAnimator(TimeScaleService.Delta(_timeChannel));
    }

    void AdvanceAnimator(float channelDelta)
    {
        if (channelDelta <= 0f)
            return;

        if (_poseRate <= 0f)
        {
            _animator.Update(channelDelta);
            return;
        }

        float step = 1f / _poseRate;
        _poseAccum += channelDelta;
        int steps = 0;
        while (_poseAccum >= step && steps < MaxPoseStepsPerFrame)
        {
            _poseAccum -= step;
            _animator.Update(step);
            steps++;
        }

        if (steps >= MaxPoseStepsPerFrame && _poseAccum >= step)
            _poseAccum %= step;
    }

    void CacheAnimatorParameters()
    {
        _hasSpeed = false;
        _hasAiming = false;

        if (_animator == null || _animator.runtimeAnimatorController == null)
            return;

        _hashSpeed = string.IsNullOrEmpty(_paramSpeed) ? 0 : Animator.StringToHash(_paramSpeed);
        _hashAiming = string.IsNullOrEmpty(_paramAiming) ? 0 : Animator.StringToHash(_paramAiming);

        AnimatorControllerParameter[] parameters = _animator.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            int nameHash = parameters[i].nameHash;
            if (!string.IsNullOrEmpty(_paramSpeed) && nameHash == _hashSpeed)
                _hasSpeed = true;
            if (!string.IsNullOrEmpty(_paramAiming) && nameHash == _hashAiming)
                _hasAiming = true;
        }
    }

    // Unity 자동 틱을 끄고 TimeScale 채널로만 진행한다.
    // 한 번도 켠 적 없이 disabled+Rebind만 하면 본 write가 안 붙는 경우가 있어,
    // 짧게 enable→Update(0)로 bind한 뒤 disabled 수동 틱으로 전환한다.
    void TakeManualControl()
    {
        if (_animator == null)
            return;

        _animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        _animator.enabled = true;
        _animator.Update(0f);
        _animator.enabled = false;
        _animator.Rebind();
        _animator.Update(0f);
        _manualControl = true;
    }

    float ResolveNormalizedSpeed()
    {
        if (_playerMovement == null)
            return 0f;

        float max = _playerMovement.RunMaxSpeed;
        if (max <= 1e-4f)
            return 0f;

        return Mathf.Clamp01(_playerMovement.CurrentSpeed / max);
    }
}
