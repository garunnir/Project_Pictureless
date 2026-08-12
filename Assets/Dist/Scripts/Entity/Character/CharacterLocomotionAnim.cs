// ============================================================
// CharacterLocomotionAnim — MoveXZ/Speed + L/R/2H overlays + thin clip remap
// ============================================================
using Garunnir.Runtime.Gameplay.Data;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// Drives Move (facing-relative MoveX/MoveZ) + RightArm/LeftArm/TwoHand overlays.
/// WeaponAction selects library clips projected onto thin keys via
/// <see cref="ArmAnimSlotResolver"/> (no Animator Action params).
/// Animation time advances via <see cref="TimeScaleService"/> only.
/// </summary>
[RequireComponent(typeof(CharacterState))]
public class CharacterLocomotionAnim : MonoBehaviour
{
    const string ManualTickHelp =
        "Play 시 Animator 컴포넌트를 끕니다(TimeScale 채널 수동 틱). " +
        "Inspector에서 Animator.enabled가 꺼져 보이는 것은 정상입니다. " +
        "재생은 이 스크립트의 Update → Animator.Update(TimeScaleService.Delta)로만 진행됩니다.";

    const string DefaultRightArmLayer = "RightArm Layer";
    const string DefaultLeftArmLayer = "LeftArm Layer";
    const string DefaultTwoHandLayer = "TwoHand Layer";
    const float DefaultPoseRate = 10f;
    const float DefaultLayerBlendSpeed = 10f;
    const int MaxPoseStepsPerFrame = 8;
    const float MoveDirEpsilonSqr = 1e-6f;
    const string AttackOverlayStateName = "Attack";

    [InfoBox(ManualTickHelp, InfoMessageType.Warning)]
    [SerializeField] TimeScaleChannel _timeChannel = TimeScaleChannel.Player;

    [Header("Animator (TimeScale manual tick)")]
    [Tooltip(ManualTickHelp)]
    [SerializeField] Animator _animator;
    [SerializeField] RuntimeAnimatorController _defaultController;
    [SerializeField] ArmAnimSlotCatalog _armSlotCatalog;
    [SerializeField] string _paramSpeed = "Speed";
    [SerializeField] string _paramMoveX = "MoveX";
    [SerializeField] string _paramMoveZ = "MoveZ";
    [SerializeField] string _paramAiming = "IsAiming";
    [SerializeField] string _paramAttackR = "AttackR";
    [SerializeField] string _paramAttackL = "AttackL";
    [SerializeField] string _paramAttack2H = "Attack2H";
    [SerializeField] string _rightArmLayerName = DefaultRightArmLayer;
    [SerializeField] string _leftArmLayerName = DefaultLeftArmLayer;
    [SerializeField] string _twoHandLayerName = DefaultTwoHandLayer;
    [SerializeField, Min(0f)] float _layerBlendSpeed = DefaultLayerBlendSpeed;
    [SerializeField, Min(0f)] float _poseRate = DefaultPoseRate;

    [ShowInInspector, ReadOnly, PropertyOrder(20)]
    [LabelText("Manual tick active (Animator.enabled forced off)")]
    bool ManualTickActive => _manualControl && _animator != null && !_animator.enabled;

    CharacterState _characterState;
    CharacterAttacker _attacker;
    PlayerGearHost _gearHost;
    CharacterSkillsHost _skillsHost;
    ICharacterLocomotion _locomotion;
    bool _manualControl;
    bool _pendingBind = true;
    float _poseAccum;
    RuntimeAnimatorController _weaponSourceController;
    AnimatorOverrideController _resolvedOverride;

    int _hashSpeed;
    int _hashMoveX;
    int _hashMoveZ;
    int _hashAiming;
    int _hashAttackR;
    int _hashAttackL;
    int _hashAttack2H;
    int _rightArmLayerIndex = -1;
    int _leftArmLayerIndex = -1;
    int _twoHandLayerIndex = -1;
    bool _hasSpeed;
    bool _hasMoveX;
    bool _hasMoveZ;
    bool _hasAiming;
    bool _hasAttackR;
    bool _hasAttackL;
    bool _hasAttack2H;
    int _hashAttackState;

    public bool HasAttackTrigger => _hasAttackR || _hasAttackL || _hasAttack2H;

    WeaponAction _mappedActionL = (WeaponAction)(-1);
    WeaponAction _mappedActionR = (WeaponAction)(-1);
    WeaponAction _mappedAction2H = (WeaponAction)(-1);

    readonly WeaponAction[] _attackActionQueue = new WeaponAction[2];
    readonly WieldHand[] _attackHandQueue = new WieldHand[2];
    int _attackQueueHead;
    int _attackQueueCount;

    public Animator Animator => _animator;

    void Awake()
    {
        _characterState = GetComponentInParent<CharacterState>();
        if (_characterState == null)
            _characterState = GetComponent<CharacterState>();

        _attacker = GetComponentInParent<CharacterAttacker>();
        if (_attacker == null)
            _attacker = GetComponent<CharacterAttacker>();

        _gearHost = GetComponentInParent<PlayerGearHost>();
        if (_gearHost == null)
            _gearHost = GetComponent<PlayerGearHost>();

        _skillsHost = GetComponentInParent<CharacterSkillsHost>();
        if (_skillsHost == null)
            _skillsHost = GetComponent<CharacterSkillsHost>();

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
        {
            _attacker.PresentationChanged += OnPresentationChanged;
            _attacker.AttackResolved += OnAttackResolved;
        }
    }

    void OnDisable()
    {
        if (_attacker != null)
        {
            _attacker.PresentationChanged -= OnPresentationChanged;
            _attacker.AttackResolved -= OnAttackResolved;
        }
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
        if (_layerBlendSpeed < 0f)
            _layerBlendSpeed = 0f;
#if UNITY_EDITOR
        if (_armSlotCatalog == null)
        {
            _armSlotCatalog = UnityEditor.AssetDatabase.LoadAssetAtPath<ArmAnimSlotCatalog>(
                "Assets/Dist/Visual/Anim/CharacterClips/ArmAnimSlotCatalog.asset");
        }
#endif
        if (_animator != null)
            CacheAnimatorParameters();
    }

    void Update()
    {
        if (_animator == null)
            return;

        bool rebound = false;
        if (_pendingBind || !_manualControl)
        {
            TakeManualControl();
            _pendingBind = false;
            rebound = true;
        }

        float speedNorm = ResolveNormalizedSpeed();
        if (_hasSpeed)
            _animator.SetFloat(_hashSpeed, speedNorm);

        ResolveFacingMoveXZ(speedNorm, out float moveX, out float moveZ);
        if (_hasMoveX)
            _animator.SetFloat(_hashMoveX, moveX);
        if (_hasMoveZ)
            _animator.SetFloat(_hashMoveZ, moveZ);

        bool isAiming = _characterState != null && _characterState.IsAiming;
        if (_hasAiming)
            _animator.SetBool(_hashAiming, isAiming);

        ResolveHandActions(out WeaponAction actionL, out WeaponAction actionR, out WeaponAction action2H);
        SyncThinActionRemap(actionL, actionR, action2H);

        if (_attackQueueCount > 0)
        {
            WeaponAction attackAction = _attackActionQueue[_attackQueueHead];
            WieldHand attackHand = _attackHandQueue[_attackQueueHead];
            _attackQueueHead = (_attackQueueHead + 1) % _attackActionQueue.Length;
            _attackQueueCount--;

            if (attackHand == WieldHand.TwoHand)
            {
                SyncThinActionRemap(actionL, actionR, attackAction);
                if (_hasAttack2H)
                    _animator.SetTrigger(_hashAttack2H);
            }
            else if (attackHand == WieldHand.Left)
            {
                SyncThinActionRemap(attackAction, actionR, action2H);
                if (_hasAttackL)
                    _animator.SetTrigger(_hashAttackL);
            }
            else
            {
                SyncThinActionRemap(actionL, attackAction, action2H);
                if (_hasAttackR)
                    _animator.SetTrigger(_hashAttackR);
            }
        }

        float channelDelta = TimeScaleService.Delta(_timeChannel);
        SyncArmLayerWeights(rebound ? 0f : channelDelta);
        AdvanceAnimator(channelDelta);
        TickAttackCues();
    }

    void SyncThinActionRemap(WeaponAction actionL, WeaponAction actionR, WeaponAction action2H)
    {
        if (_resolvedOverride == null || _armSlotCatalog == null)
            return;

        if (actionL == _mappedActionL &&
            actionR == _mappedActionR &&
            action2H == _mappedAction2H)
            return;

        ArmAnimSlotResolver.RemapThinKeys(
            _resolvedOverride, _armSlotCatalog, actionL, actionR, action2H);
        _mappedActionL = actionL;
        _mappedActionR = actionR;
        _mappedAction2H = action2H;
    }

    void OnAttackResolved(AttackOutcome outcome)
    {
        if (WeaponActionUtil.SuppressesAttackTrigger(outcome.Action))
            return;
        if (_attackQueueCount >= _attackActionQueue.Length)
            return;

        int index = (_attackQueueHead + _attackQueueCount) % _attackActionQueue.Length;
        _attackActionQueue[index] = outcome.Action;
        _attackHandQueue[index] = outcome.Hand;
        _attackQueueCount++;
    }

    void OnPresentationChanged() => ApplyWeaponAnimOverride(forceRebind: true);

    void ApplyWeaponAnimOverride(bool forceRebind)
    {
        if (_animator == null)
            return;

        RuntimeAnimatorController source = _defaultController;
        if (_attacker != null &&
            _attacker.Presentation != null &&
            _attacker.Presentation.AnimatorOverride != null)
        {
            source = _attacker.Presentation.AnimatorOverride;
        }

        if (source == null)
            return;

        if (!forceRebind && ReferenceEquals(source, _weaponSourceController) && _resolvedOverride != null)
            return;

        if (_resolvedOverride != null)
        {
            if (ReferenceEquals(_animator.runtimeAnimatorController, _resolvedOverride))
                _animator.runtimeAnimatorController = null;
            Destroy(_resolvedOverride);
            _resolvedOverride = null;
        }

        _weaponSourceController = source;
        _mappedActionL = (WeaponAction)(-1);
        _mappedActionR = (WeaponAction)(-1);
        _mappedAction2H = (WeaponAction)(-1);

        ResolveHandActions(out WeaponAction actionL, out WeaponAction actionR, out WeaponAction action2H);
        if (_armSlotCatalog != null)
        {
            _resolvedOverride = ArmAnimSlotResolver.BuildResolvedOverride(
                source, _armSlotCatalog, actionL, actionR, action2H);
            _mappedActionL = actionL;
            _mappedActionR = actionR;
            _mappedAction2H = action2H;
        }

        RuntimeAnimatorController next = _resolvedOverride != null
            ? (RuntimeAnimatorController)_resolvedOverride
            : source;

        if (!ReferenceEquals(_animator.runtimeAnimatorController, next))
            _animator.runtimeAnimatorController = next;

        CacheAnimatorParameters();

        if (forceRebind || _manualControl)
        {
            _manualControl = false;
            _pendingBind = true;
        }
    }

    void OnDestroy()
    {
        if (_resolvedOverride == null)
            return;
        if (_animator != null && ReferenceEquals(_animator.runtimeAnimatorController, _resolvedOverride))
            _animator.runtimeAnimatorController = _defaultController;
        Destroy(_resolvedOverride);
        _resolvedOverride = null;
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

    void TickAttackCues()
    {
        if (_attacker == null || !_attacker.HasPendingAttackCue || _animator == null)
            return;

        TickAttackCueHand(WieldHand.Right, _rightArmLayerIndex);
        TickAttackCueHand(WieldHand.Left, _leftArmLayerIndex);
        TickAttackCueHand(WieldHand.TwoHand, _twoHandLayerIndex);
    }

    void TickAttackCueHand(WieldHand hand, int layerIndex)
    {
        if (!_attacker.HasPendingFor(hand))
            return;

        if (layerIndex < 0)
        {
            _attacker.NotifyAttackCueForHand(hand);
            return;
        }

        AnimatorStateInfo current = _animator.GetCurrentAnimatorStateInfo(layerIndex);
        bool inAttack = current.shortNameHash == _hashAttackState;
        float normalizedTime = current.normalizedTime;
        if (_animator.IsInTransition(layerIndex))
        {
            AnimatorStateInfo next = _animator.GetNextAnimatorStateInfo(layerIndex);
            if (next.shortNameHash == _hashAttackState)
            {
                inAttack = true;
                normalizedTime = next.normalizedTime;
            }
        }

        _attacker.NotifyAttackOverlayTick(hand, inAttack, normalizedTime);
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
        _hasMoveX = false;
        _hasMoveZ = false;
        _hasAiming = false;
        _hasAttackR = false;
        _hasAttackL = false;
        _hasAttack2H = false;
        _rightArmLayerIndex = -1;
        _leftArmLayerIndex = -1;
        _twoHandLayerIndex = -1;
        _hashAttackState = Hash(AttackOverlayStateName);

        if (_animator == null || _animator.runtimeAnimatorController == null)
            return;

        _hashSpeed = Hash(_paramSpeed);
        _hashMoveX = Hash(_paramMoveX);
        _hashMoveZ = Hash(_paramMoveZ);
        _hashAiming = Hash(_paramAiming);
        _hashAttackR = Hash(_paramAttackR);
        _hashAttackL = Hash(_paramAttackL);
        _hashAttack2H = Hash(_paramAttack2H);

        AnimatorControllerParameter[] parameters = _animator.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            int nameHash = parameters[i].nameHash;
            if (Match(_paramSpeed, _hashSpeed, nameHash)) _hasSpeed = true;
            if (Match(_paramMoveX, _hashMoveX, nameHash)) _hasMoveX = true;
            if (Match(_paramMoveZ, _hashMoveZ, nameHash)) _hasMoveZ = true;
            if (Match(_paramAiming, _hashAiming, nameHash)) _hasAiming = true;
            if (Match(_paramAttackR, _hashAttackR, nameHash)) _hasAttackR = true;
            if (Match(_paramAttackL, _hashAttackL, nameHash)) _hasAttackL = true;
            if (Match(_paramAttack2H, _hashAttack2H, nameHash)) _hasAttack2H = true;
        }

        if (!string.IsNullOrEmpty(_rightArmLayerName))
            _rightArmLayerIndex = _animator.GetLayerIndex(_rightArmLayerName);
        if (!string.IsNullOrEmpty(_leftArmLayerName))
            _leftArmLayerIndex = _animator.GetLayerIndex(_leftArmLayerName);
        if (!string.IsNullOrEmpty(_twoHandLayerName))
            _twoHandLayerIndex = _animator.GetLayerIndex(_twoHandLayerName);
    }

    static int Hash(string name) =>
        string.IsNullOrEmpty(name) ? 0 : Animator.StringToHash(name);

    static bool Match(string name, int hash, int nameHash) =>
        !string.IsNullOrEmpty(name) && nameHash == hash;

    void SyncArmLayerWeights(float channelDelta)
    {
        bool twoHand = false;
        bool leftArmed = false;
        bool rightArmed = false;

        CharacterGearService gear = _gearHost != null ? _gearHost.Service : null;
        if (gear?.Wield != null)
        {
            twoHand = gear.Wield.IsTwoHand;
            leftArmed = !twoHand && gear.Wield.Left != null;
            rightArmed = !twoHand && gear.Wield.Right != null;
        }
        else if (_attacker != null && !string.IsNullOrEmpty(_attacker.ItemId))
        {
            WieldHand hand = _attacker.ActiveWieldHand;
            twoHand = hand == WieldHand.TwoHand;
            leftArmed = hand == WieldHand.Left;
            rightArmed = hand == WieldHand.Right;
        }

        SetLayerWeightToward(_rightArmLayerIndex, twoHand ? 0f : (rightArmed ? 1f : 0f), channelDelta);
        SetLayerWeightToward(_leftArmLayerIndex, twoHand ? 0f : (leftArmed ? 1f : 0f), channelDelta);
        SetLayerWeightToward(_twoHandLayerIndex, twoHand ? 1f : 0f, channelDelta);
    }

    void SetLayerWeightToward(int layerIndex, float target, float channelDelta)
    {
        if (layerIndex < 0)
            return;

        float current = _animator.GetLayerWeight(layerIndex);
        if (_layerBlendSpeed <= 0f || channelDelta <= 0f)
        {
            if (!Mathf.Approximately(current, target))
                _animator.SetLayerWeight(layerIndex, target);
            return;
        }

        float next = Mathf.MoveTowards(current, target, _layerBlendSpeed * channelDelta);
        if (!Mathf.Approximately(current, next))
            _animator.SetLayerWeight(layerIndex, next);
    }

    void ResolveHandActions(out WeaponAction actionL, out WeaponAction actionR, out WeaponAction action2H)
    {
        actionL = WeaponAction.Swing;
        actionR = WeaponAction.Swing;
        action2H = _attacker != null ? _attacker.SelectedAction : WeaponAction.Swing;

        CharacterGearService gear = _gearHost != null ? _gearHost.Service : null;
        if (gear?.Wield == null)
        {
            if (_attacker != null)
            {
                actionL = _attacker.SelectedAction;
                actionR = _attacker.SelectedAction;
            }
            return;
        }

        WeaponPresentationCatalog catalog = gear.PresentationCatalog
            ?? (_attacker != null ? _attacker.Catalog : null);

        if (gear.Wield.IsTwoHand)
        {
            ItemStack stack = gear.Wield.Left ?? gear.Wield.Right;
            action2H = ActionForStack(catalog, stack, action2H);
            actionL = action2H;
            actionR = action2H;
            return;
        }

        actionL = ActionForStack(catalog, gear.Wield.Left, actionL);
        actionR = ActionForStack(catalog, gear.Wield.Right, actionR);
    }

    static WeaponAction ActionForStack(
        WeaponPresentationCatalog catalog,
        ItemStack stack,
        WeaponAction fallback)
    {
        if (stack?.Item == null)
            return fallback;

        WeaponPresentation presentation = WeaponActionRows.Resolve(catalog, stack);
        return WeaponActionRows.ResolveSelected(stack.Instance, presentation);
    }

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

    void ResolveFacingMoveXZ(float speedNorm, out float moveX, out float moveZ)
    {
        moveX = 0f;
        moveZ = 0f;

        if (speedNorm <= 1e-4f || _characterState == null)
            return;

        Vector3 wish = _characterState.MoveDir;
        wish.y = 0f;
        if (wish.sqrMagnitude <= MoveDirEpsilonSqr)
            return;

        Vector3 facing = _characterState.GetFacingDir();
        facing.y = 0f;
        if (facing.sqrMagnitude <= MoveDirEpsilonSqr)
            return;

        Quaternion facingRot = Quaternion.LookRotation(facing.normalized, Vector3.up);
        Vector3 local = Quaternion.Inverse(facingRot) * wish.normalized;
        local.y = 0f;
        if (local.sqrMagnitude <= MoveDirEpsilonSqr)
            return;

        local.Normalize();
        moveX = local.x * speedNorm;
        moveZ = local.z * speedNorm;
    }
}
