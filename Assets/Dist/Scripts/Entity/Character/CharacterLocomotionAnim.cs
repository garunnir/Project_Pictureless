// ============================================================
// CharacterLocomotionAnim — Speed/IsAiming/Action + Aim 레이어 weight + 무기 Override, TimeScale 틱
// ============================================================
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// Drives a 3D layered Animator from <see cref="ICharacterLocomotion"/> speed,
/// <see cref="CharacterState.IsAiming"/>, and <see cref="CharacterAttacker.SelectedAction"/>.
/// Aim Layer weight follows IsAiming so non-aim locomotion keeps full-body Move (arm swing).
/// Applies <see cref="WeaponPresentation.AnimatorOverride"/> when presentation changes.
/// Animation time advances via <see cref="TimeScaleService"/> only (Animator auto-tick disabled).
/// Optional pose rate quantizes ticks for a flipbook look without stepped clips.
/// </summary>
[RequireComponent(typeof(CharacterState))]
public class CharacterLocomotionAnim : MonoBehaviour
{
    const string ManualTickHelp =
        "Play 시 Animator 컴포넌트를 끕니다(TimeScale 채널 수동 틱). " +
        "Inspector에서 Animator.enabled가 꺼져 보이는 것은 정상입니다. " +
        "재생은 이 스크립트의 Update → Animator.Update(TimeScaleService.Delta)로만 진행됩니다.";

    const string DefaultAimLayerName = "Aim Layer";
    const float DefaultPoseRate = 10f;
    const float DefaultAimLayerBlendSpeed = 10f;
    const int MaxPoseStepsPerFrame = 8;

    [InfoBox(ManualTickHelp, InfoMessageType.Warning)]
    [Tooltip("애니 진행에 사용할 시간 채널. 플레이어=Player, NPC·환경=World.")]
    [SerializeField] TimeScaleChannel _timeChannel = TimeScaleChannel.Player;

    [Header("Animator (TimeScale manual tick)")]
    [Tooltip(ManualTickHelp)]
    [SerializeField] Animator _animator;
    [Tooltip("무기 Override가 없거나 비무장일 때 쓸 기본 컨트롤러. 비우면 Awake 시점 Animator 할당값을 캡처.")]
    [SerializeField] RuntimeAnimatorController _defaultController;
    [SerializeField] string _paramSpeed = "Speed";
    [SerializeField] string _paramAiming = "IsAiming";
    [SerializeField] string _paramAction = "Action";
    [Tooltip("UpperBody Override Aim 레이어 이름. 비조준 시 weight 0 → Move 전신 유지.")]
    [SerializeField] string _aimLayerName = DefaultAimLayerName;
    [Tooltip("Aim 레이어 weight 초당 변화량(채널 시간). 0이면 즉시 스냅.")]
    [SerializeField, Min(0f)] float _aimLayerBlendSpeed = DefaultAimLayerBlendSpeed;
    [Tooltip("초당 애니 포즈 수(채널 시간 기준). 0이면 매 프레임 연속 틱. BlendTree 유지한 채 플립북 느낌.")]
    [SerializeField, Min(0f)] float _poseRate = DefaultPoseRate;
    [ShowInInspector, ReadOnly, PropertyOrder(20)]
    [LabelText("Manual tick active (Animator.enabled forced off)")]
    bool ManualTickActive => _manualControl && _animator != null && !_animator.enabled;

    CharacterState _characterState;
    CharacterAttacker _attacker;
    ICharacterLocomotion _locomotion;
    bool _manualControl;
    bool _pendingBind = true;
    float _poseAccum;
    RuntimeAnimatorController _appliedController;

    int _hashSpeed;
    int _hashAiming;
    int _hashAction;
    int _aimLayerIndex = -1;
    bool _hasSpeed;
    bool _hasAiming;
    bool _hasAction;

    public Animator Animator => _animator;

    void Awake()
    {
        _characterState = GetComponentInParent<CharacterState>();
        if (_characterState == null)
            _characterState = GetComponent<CharacterState>();

        _attacker = GetComponentInParent<CharacterAttacker>();
        if (_attacker == null)
            _attacker = GetComponent<CharacterAttacker>();

        _locomotion = GetComponentInParent<ICharacterLocomotion>();
        if (_locomotion == null)
            _locomotion = GetComponent<ICharacterLocomotion>();

        if (_animator == null)
            _animator = GetComponentInChildren<Animator>();

        if (_defaultController == null && _animator != null)
            _defaultController = ResolveBaseController(_animator.runtimeAnimatorController);

        ApplyWeaponAnimOverride(forceRebind: false);
        CacheAnimatorParameters();
    }

    void OnEnable()
    {
        if (_attacker != null)
            _attacker.PresentationChanged += OnPresentationChanged;
    }

    void OnDisable()
    {
        if (_attacker != null)
            _attacker.PresentationChanged -= OnPresentationChanged;
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
        if (_aimLayerBlendSpeed < 0f)
            _aimLayerBlendSpeed = 0f;

        if (_animator != null)
            CacheAnimatorParameters();
    }

    void Update()
    {
        if (_animator == null)
            return;

        // Avatar/SkinnedMesh 준비가 끝난 첫 Update에서 bind (Awake/Start Rebind는 종종 no-op).
        bool rebound = false;
        if (_pendingBind || !_manualControl)
        {
            TakeManualControl();
            _pendingBind = false;
            rebound = true;
        }

        if (_hasSpeed)
            _animator.SetFloat(_hashSpeed, ResolveNormalizedSpeed());

        bool isAiming = _characterState != null && _characterState.IsAiming;
        if (_hasAiming)
            _animator.SetBool(_hashAiming, isAiming);

        if (_hasAction)
            _animator.SetInteger(_hashAction, ResolveAction());

        float channelDelta = TimeScaleService.Delta(_timeChannel);
        // Rebind는 레이어 weight를 컨트롤러 기본값(Aim=1)으로 되돌리므로 즉시 스냅.
        SyncAimLayerWeight(isAiming, rebound ? 0f : channelDelta);
        AdvanceAnimator(channelDelta);
    }

    void OnPresentationChanged() => ApplyWeaponAnimOverride(forceRebind: true);

    void ApplyWeaponAnimOverride(bool forceRebind)
    {
        if (_animator == null)
            return;

        RuntimeAnimatorController next = _defaultController;
        if (_attacker != null &&
            _attacker.Presentation != null &&
            _attacker.Presentation.AnimatorOverride != null)
        {
            next = _attacker.Presentation.AnimatorOverride;
        }

        if (next == null)
            return;

        if (!forceRebind && ReferenceEquals(next, _appliedController))
            return;

        if (!ReferenceEquals(_animator.runtimeAnimatorController, next))
            _animator.runtimeAnimatorController = next;

        _appliedController = next;
        CacheAnimatorParameters();

        if (forceRebind || _manualControl)
        {
            _manualControl = false;
            _pendingBind = true;
        }
    }

    static RuntimeAnimatorController ResolveBaseController(RuntimeAnimatorController controller)
    {
        if (controller is AnimatorOverrideController overrideController &&
            overrideController.runtimeAnimatorController != null)
        {
            return overrideController.runtimeAnimatorController;
        }

        return controller;
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
        _hasAction = false;
        _aimLayerIndex = -1;

        if (_animator == null || _animator.runtimeAnimatorController == null)
            return;

        _hashSpeed = string.IsNullOrEmpty(_paramSpeed) ? 0 : Animator.StringToHash(_paramSpeed);
        _hashAiming = string.IsNullOrEmpty(_paramAiming) ? 0 : Animator.StringToHash(_paramAiming);
        _hashAction = string.IsNullOrEmpty(_paramAction) ? 0 : Animator.StringToHash(_paramAction);

        AnimatorControllerParameter[] parameters = _animator.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            int nameHash = parameters[i].nameHash;
            if (!string.IsNullOrEmpty(_paramSpeed) && nameHash == _hashSpeed)
                _hasSpeed = true;
            if (!string.IsNullOrEmpty(_paramAiming) && nameHash == _hashAiming)
                _hasAiming = true;
            if (!string.IsNullOrEmpty(_paramAction) && nameHash == _hashAction)
                _hasAction = true;
        }

        if (!string.IsNullOrEmpty(_aimLayerName))
            _aimLayerIndex = _animator.GetLayerIndex(_aimLayerName);
    }

    void SyncAimLayerWeight(bool isAiming, float channelDelta)
    {
        if (_aimLayerIndex < 0)
            return;

        float target = isAiming ? 1f : 0f;
        float current = _animator.GetLayerWeight(_aimLayerIndex);
        if (_aimLayerBlendSpeed <= 0f || channelDelta <= 0f)
        {
            if (!Mathf.Approximately(current, target))
                _animator.SetLayerWeight(_aimLayerIndex, target);
            return;
        }

        float next = Mathf.MoveTowards(current, target, _aimLayerBlendSpeed * channelDelta);
        if (!Mathf.Approximately(current, next))
            _animator.SetLayerWeight(_aimLayerIndex, next);
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
        if (_locomotion == null)
            return 0f;

        float max = _locomotion.AnimSpeedReference;
        if (max <= 1e-4f)
            return 0f;

        return Mathf.Clamp01(_locomotion.CurrentSpeed / max);
    }

    int ResolveAction()
    {
        if (_attacker == null)
            return (int)WeaponAction.Bashing;

        return (int)_attacker.SelectedAction;
    }
}
