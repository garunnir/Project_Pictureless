// ============================================================
// CharacterAttacker — Action 시그널 시전 + 클립 큐에서 IActionHandler 실행
// ============================================================

using System;
using Garunnir.Runtime.Gameplay.Data;
using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterAimIntent))]
[RequireComponent(typeof(CharacterSkillsHost))]
public sealed class CharacterAttacker : MonoBehaviour
{
    const float AimHeight = 0.15f;
    public const float MinRayDistance = 0.001f;
    const float SurfaceProbeMargin = 1f;
    const float FallbackImpactRadius = 0.4f;
    const int PendingCueSlotCount = 2;
    const int HandCooldownSlotCount = (int)WieldHand.TwoHand + 1;

    [FormerlySerializedAs("_weapon")]
    [SerializeField] WeaponPresentation _presentation;
    [Tooltip("GameplayData ItemData id. 비우면 비무장.")]
    [SerializeField] string _itemId;
    [SerializeField] WeaponPresentationCatalog _catalog;
    [SerializeField] LayerMask _rangedObstructionMask = ~0;
    [SerializeField] TimeScaleChannel _timeChannel = TimeScaleChannel.World;
    [SerializeField] WeaponAction _selectedAction = WeaponAction.Swing;
    [SerializeField] WieldHand _activeWieldHand = WieldHand.Right;
    ItemInstance _wieldedInstance;
    ItemStack _wieldedStack;

    CharacterAimIntent _aimIntent;
    CharacterSkillsHost _skillsHost;
    PlayerGearHost _gearHost;
    CharacterState _characterState;
    PlayerAimController _aimController;
    CharacterLocomotionAnim _locAnim;
    Collider _selfCollider;
    readonly float[] _cooldownRemaining = new float[HandCooldownSlotCount];
    readonly PendingAttack[] _pendingCues = new PendingAttack[PendingCueSlotCount];

    public event Action AvailableActionsChanged;
    public event Action SelectedActionChanged;
    public event Action PresentationChanged;
    public event Action ActiveWieldHandChanged;

    /// <summary>시전된 액션 판정(Performed/Miss 및 Cooling/NoAmmo/NoTarget/OutOfRange). 연출 계층이 구독한다.</summary>
    public event Action<AttackOutcome> AttackResolved;

    /// <summary>모든 CharacterAttacker Resolve 공통 훅 (메시지 로그 등).</summary>
    public static event Action<AttackOutcome> AnyAttackResolved;

    /// <summary>클립 큐에서 Attack 로직이 판정한 결과 (피해·히트 VFX).</summary>
    public event Action<AttackOutcome> AttackJudged;

    public static event Action<AttackOutcome> AnyAttackJudged;

    public bool HasPendingAttackCue
    {
        get
        {
            for (int i = 0; i < _pendingCues.Length; i++)
            {
                if (_pendingCues[i].Armed && !_pendingCues[i].CueFired)
                    return true;
            }

            return false;
        }
    }

    public bool HasPendingFor(WieldHand hand)
    {
        int index = FindPending(hand);
        return index >= 0 && _pendingCues[index].Armed && !_pendingCues[index].CueFired;
    }

    public LayerMask RangedObstructionMask => _rangedObstructionMask;

    public WeaponPresentation Presentation => _presentation;
    public WeaponPresentationCatalog Catalog => _catalog;
    public string ItemId => _itemId;
    public ItemInstance WieldedInstance => _wieldedInstance;
    public ItemStack WieldedStack => _wieldedStack;
    public WeaponActionMask AvailableActions { get; private set; }
    public WeaponAction SelectedAction => _selectedAction;
    public WieldHand ActiveWieldHand => _activeWieldHand;

    /// <summary>플레이어는 CharacterState, NPC는 AimIntent.AimHeld.</summary>
    public bool IsAiming =>
        _characterState != null
            ? _characterState.IsAiming
            : _aimIntent != null && _aimIntent.AimHeld;

    /// <summary>raise_guard 핸들러가 판정한 가드 유지.</summary>
    public bool IsRaiseActive { get; private set; }

    /// <summary>애니·시전 손. TwoHand / Left / Right.</summary>
    public void SetActiveWieldHand(WieldHand hand)
    {
        if (_activeWieldHand == hand)
            return;
        _activeWieldHand = hand;
        ActiveWieldHandChanged?.Invoke();
    }

    /// <summary>슬롯 → 애니 손. 양손 모드면 TwoHand.</summary>
    public static WieldHand AnimHandFrom(WieldSlots slots, WieldSlotId slot)
    {
        if (slots != null && slots.IsTwoHand)
            return WieldHand.TwoHand;
        return slot == WieldSlotId.Left ? WieldHand.Left : WieldHand.Right;
    }

    ItemData CurrentItem =>
        string.IsNullOrEmpty(_itemId) ? null : GameplayData.GetItem(_itemId);

    void Awake()
    {
        _aimIntent = GetComponent<CharacterAimIntent>();
        _skillsHost = GetComponent<CharacterSkillsHost>();
        TryGetComponent(out _gearHost);
        TryGetComponent(out _characterState);
        TryGetComponent(out _aimController);
        TryGetComponent(out _locAnim);
        if (_locAnim == null)
            _locAnim = GetComponentInChildren<CharacterLocomotionAnim>();
        _selfCollider = GetComponentInChildren<Collider>();
        if (_presentation != null)
            _presentation.RebuildSupportedActions();
        RefreshPresentationFromCatalog();
        RebuildAvailableActions();
        ApplySelectedFromInstance();
    }

    void OnEnable()
    {
        ICharacterSkills skills = _skillsHost != null ? _skillsHost.Skills : null;
        if (skills != null)
            skills.Refreshed += OnSkillsRefreshed;
    }

    void OnDisable()
    {
        ICharacterSkills skills = _skillsHost != null ? _skillsHost.Skills : null;
        if (skills != null)
            skills.Refreshed -= OnSkillsRefreshed;
        CancelAllPendingCues();
        ApplyRaiseFromHandler(false);
    }

    void Update()
    {
        float dt = TimeScaleService.Delta(_timeChannel);
        if (dt <= 0f)
            return;

        for (int i = 0; i < _cooldownRemaining.Length; i++)
        {
            if (_cooldownRemaining[i] <= 0f)
                continue;
            _cooldownRemaining[i] = Mathf.Max(0f, _cooldownRemaining[i] - dt);
        }

        TickRaiseGuard();
    }

    /// <summary>들기(Wield) 훅. 카탈로그로 Presentation resolve. 선택은 ItemInstance. 약실은 인스턴스.</summary>
    public void SetWieldedItem(ItemStack stack) =>
        SetWieldedItemCore(stack?.ItemId ?? string.Empty, stack);

    /// <summary>인스턴스 없는 경로(비무장·인스펙터). 선택은 SO default / 로컬만.</summary>
    public void SetWieldedItem(string itemId) =>
        SetWieldedItemCore(itemId, null);

    void SetWieldedItemCore(string itemId, ItemStack stack)
    {
        ItemInstance instance = stack?.Instance;
        if (ReferenceEquals(_wieldedStack, stack) &&
            ReferenceEquals(_wieldedInstance, instance) &&
            string.Equals(_itemId, itemId, StringComparison.Ordinal))
            return;

        _wieldedStack = stack;
        _wieldedInstance = instance;
        _itemId = itemId ?? string.Empty;
        RefreshPresentationFromCatalog();
        RebuildAvailableActions();
        ApplySelectedFromInstance();
    }

    [Obsolete("Use SetWieldedItem")]
    public void SetEquippedItem(string itemId) =>
        SetWieldedItem(itemId);

    public void SetPresentation(WeaponPresentation presentation)
    {
        if (_presentation == presentation)
            return;

        _presentation = presentation;
        if (_presentation != null)
            _presentation.RebuildSupportedActions();
        RebuildAvailableActions();
        ApplySelectedFromInstance();
        PresentationChanged?.Invoke();
    }

    public void SetCatalog(WeaponPresentationCatalog catalog)
    {
        _catalog = catalog;
        RefreshPresentationFromCatalog();
    }

    public bool CanPerform(WeaponAction action) =>
        (AvailableActions & WeaponActionUtil.ToMask(action)) != 0;

    public void CycleSelectedAction()
    {
        if (!WeaponActionUtil.TryNextAvailable(
                AvailableActions,
                _selectedAction,
                out WeaponAction next))
            return;

        if (next == _selectedAction)
            return;

        _selectedAction = next;
        WriteSelectedToInstance(next);
        SelectedActionChanged?.Invoke();
    }

    public bool TrySelectAction(WeaponAction action)
    {
        if (!CanPerform(action))
            return false;
        WriteSelectedToInstance(action);
        if (_selectedAction == action)
            return true;
        _selectedAction = action;
        SelectedActionChanged?.Invoke();
        return true;
    }

    [Obsolete("Use SelectedAction / TryPerformSelected. Distance no longer picks an action.")]
    public bool TryGetBestAction(float distance, out WeaponAction action)
    {
        action = _selectedAction;
        if (!CanPerform(_selectedAction))
            return false;
        if (GetCooldown(_activeWieldHand) > 0f)
            return false;

        float range = CombatMath.RangeMeters(CurrentItem, _selectedAction);
        return distance <= range;
    }

    public bool TryReload() =>
        WeaponChamber.TryReload(_wieldedInstance, _wieldedStack, CurrentItem);

    public void ApplyRaiseFromHandler(bool active) =>
        IsRaiseActive = active;

    public AttackPerformResult TryPerform(
        WeaponAction action,
        CharacterBodyHost targetHost,
        float offenseFactor = 1f)
    {
        if (!CanPerform(action))
        {
            Debug.LogWarning(
                $"[CharacterAttacker] Action {action} not available on {name}",
                this);
            return AttackPerformResult.Unsupported;
        }

        ItemData item = CurrentItem;
        if (action == WeaponAction.Raise)
            return PerformRaise(action, targetHost, offenseFactor, item);

        WeaponResolveMode resolveMode = WeaponActionUtil.ResolveMode(action);
        Vector3 origin = ResolveBodyCenter(transform, _selfCollider);
        AttackPerformResult gate = GateAction(action, item, targetHost);

        ArmPendingCue(action, targetHost, offenseFactor);

        AttackPerformResult signal = ResolveActionSignal(
            action, resolveMode, gate, targetHost, origin, item);

        if (!HasAttackOverlayWatch)
            NotifyAttackCue();

        return signal;
    }

    AttackPerformResult GateAction(
        WeaponAction action,
        ItemData item,
        CharacterBodyHost targetHost)
    {
        if (GetCooldown(_activeWieldHand) > 0f)
            return AttackPerformResult.Cooling;

        if (action == WeaponAction.Raise)
            return AttackPerformResult.Performed;

        if (WeaponActionUtil.Normalize(action) == WeaponAction.Trigger &&
            !WeaponChamber.CanCommitFire(item, _wieldedInstance, _wieldedStack, AttackFor(action)))
            return AttackPerformResult.NoAmmo;

        if (targetHost == null || targetHost.Body == null)
            return AttackPerformResult.NoTarget;

        Vector3 toTarget = targetHost.transform.position - transform.position;
        toTarget.y = 0f;
        float range = CombatMath.RangeMeters(item, action);
        if (toTarget.magnitude > range)
            return AttackPerformResult.OutOfRange;

        return AttackPerformResult.Performed;
    }

    float ResolveAttackerWearEncAccuracyFactor()
    {
        EquipmentWearState wear = _gearHost != null ? _gearHost.Wear : null;
        if (wear == null)
            return 1f;
        return WearCombatDefense.WearEncAccuracyFactor(
            WearStatsAggregator.Aggregate(wear).TotalEncumbrance);
    }

    float ResolveAttackerEnvAccuracyFactor()
    {
        if (_gearHost == null)
            return 1f;
        BodyTemp bodyTemp = _gearHost.BodyTemperature;
        WearEnvExposure env = _gearHost.EnvExposure;
        if (bodyTemp == null || env == null)
            return 1f;
        return GearEnvPenalties.HitAccuracyFactor(bodyTemp.Feeling, env.Wetness01);
    }

    static EquipmentWearState ResolveTargetWear(CharacterBodyHost targetHost)
    {
        if (targetHost == null)
            return null;
        if (targetHost.TryGetComponent(out PlayerGearHost gear))
            return gear.Wear;
        return null;
    }

    const int StrengthBaselineFallback = 8;

    AttackPerformResult ResolveActionSignal(
        WeaponAction action,
        WeaponResolveMode resolveMode,
        AttackPerformResult result,
        CharacterBodyHost targetHost,
        Vector3 origin,
        ItemData item)
    {
        Vector3 impact = ResolveOutcomeImpact(targetHost, origin, item, action);
        return Resolve(
            action,
            resolveMode,
            result,
            targetHost,
            string.Empty,
            0,
            origin,
            impact);
    }

    Vector3 ResolveOutcomeImpact(
        CharacterBodyHost targetHost,
        Vector3 origin,
        ItemData item,
        WeaponAction action)
    {
        if (targetHost == null || targetHost.Body == null)
            return ResolveAimImpact(origin, item, action);

        Collider targetCollider = targetHost.GetComponentInChildren<Collider>();
        Vector3 targetCenter = ResolveBodyCenter(targetHost.transform, targetCollider);
        return ResolveImpactPoint(targetCollider, targetCenter, origin);
    }

    Vector3 ResolveAimImpact(Vector3 origin, ItemData item, WeaponAction action)
    {
        if (_characterState != null)
        {
            Vector3 aim = _characterState.AimWorldPoint;
            if (aim.sqrMagnitude > 1e-6f)
                return aim;

            Vector3 dir = _characterState.SightDir;
            if (dir.sqrMagnitude < 1e-6f)
                dir = _characterState.InteractionDir;
            if (dir.sqrMagnitude > 1e-6f)
            {
                float dist = _characterState.InteractionReach;
                if (dist <= 0f && _aimController != null)
                    dist = _aimController.MaxAimDistance;
                if (dist > 0f)
                    return origin + dir.normalized * dist;
            }
        }

        Vector3 fallbackDir = transform.forward;
        fallbackDir.y = 0f;
        if (fallbackDir.sqrMagnitude < 1e-6f)
            fallbackDir = Vector3.forward;

        float fallbackDist = CombatMath.RangeMeters(item, action);
        if (fallbackDist <= 0f)
            fallbackDist = FallbackImpactRadius;
        return origin + fallbackDir.normalized * fallbackDist;
    }

    AttackPerformResult Resolve(
        WeaponAction action,
        WeaponResolveMode resolveMode,
        AttackPerformResult result,
        CharacterBodyHost target,
        string aimedPartId,
        int damage,
        Vector3 origin,
        Vector3 impact)
    {
        var outcome = new AttackOutcome(
            action,
            _activeWieldHand,
            resolveMode,
            result,
            target,
            aimedPartId,
            damage,
            origin,
            impact);
        AttackResolved?.Invoke(outcome);
        AnyAttackResolved?.Invoke(outcome);
        return result;
    }

    public static Vector3 ResolveBodyCenter(Transform owner, Collider collider) =>
        collider != null
            ? collider.bounds.center
            : owner.position + Vector3.up * AimHeight;

    static Vector3 ResolveImpactPoint(
        Collider targetCollider,
        Vector3 targetCenter,
        Vector3 origin)
    {
        Vector3 offset = targetCenter - origin;
        float distance = offset.magnitude;
        if (distance <= MinRayDistance)
            return targetCenter;

        Vector3 direction = offset / distance;
        if (targetCollider != null &&
            targetCollider.Raycast(
                new Ray(origin, direction),
                out RaycastHit surface,
                distance + SurfaceProbeMargin))
        {
            return surface.point;
        }

        float radius = targetCollider != null
            ? Mathf.Min(targetCollider.bounds.extents.x, targetCollider.bounds.extents.z)
            : FallbackImpactRadius;
        return targetCenter - direction * radius;
    }

    public AttackPerformResult TryPerformSelected(CharacterBodyHost targetHost) =>
        TryPerform(_selectedAction, targetHost);

    AttackPerformResult PerformRaise(
        WeaponAction action,
        CharacterBodyHost targetHost,
        float offenseFactor,
        ItemData item)
    {
        AttackPerformResult gate = GateAction(action, item, targetHost);
        var context = new ActionHandlerContext(
            action,
            _activeWieldHand,
            AttackFor(action),
            targetHost,
            Mathf.Max(0f, offenseFactor),
            _itemId ?? string.Empty,
            _wieldedInstance,
            _wieldedStack);
        IActionHandler handler = ActionHandlerRegistry.Resolve(context.Attack, action);
        handler?.Execute(this, context);

        Vector3 origin = ResolveOrigin();
        Vector3 impact = ResolveOutcomeImpact(targetHost, origin, item, action);
        return Resolve(
            action,
            WeaponResolveMode.MeleeReach,
            gate,
            targetHost,
            string.Empty,
            0,
            origin,
            impact);
    }

    void TickRaiseGuard()
    {
        bool shouldRaise = CanPerform(WeaponAction.Raise)
            && _selectedAction == WeaponAction.Raise
            && IsAiming;
        if (shouldRaise == IsRaiseActive)
            return;

        if (!ActionHandlerRegistry.TryGet(ActionHandlerIds.RaiseGuard, out IActionHandler handler))
        {
            ApplyRaiseFromHandler(false);
            return;
        }

        var context = new ActionHandlerContext(
            WeaponAction.Raise,
            _activeWieldHand,
            AttackFor(WeaponAction.Raise),
            null,
            1f,
            _itemId ?? string.Empty,
            _wieldedInstance,
            _wieldedStack);
        handler.Execute(this, context);
    }

    public float GetCooldown(WieldHand hand) =>
        _cooldownRemaining[CooldownIndex(hand)];

    public ItemData ItemFor(string itemId) =>
        string.IsNullOrEmpty(itemId) ? null : GameplayData.GetItem(itemId);

    public Vector3 ResolveOrigin() =>
        ResolveBodyCenter(transform, _selfCollider);

    /// <summary>Animator Animation Event. 클립 이벤트가 있으면 정규화 시각보다 우선.</summary>
    public void NotifyAttackCue()
    {
        for (int i = 0; i < _pendingCues.Length; i++)
        {
            if (_pendingCues[i].Armed && !_pendingCues[i].CueFired)
                ExecutePendingCue(i);
        }
    }

    public void NotifyAttackCueForHand(WieldHand hand)
    {
        int index = FindPending(hand);
        if (index < 0)
            return;
        ExecutePendingCue(index);
    }

    public void NotifyAttackOverlayTick(
        WieldHand hand,
        bool inAttack,
        float normalizedTime)
    {
        int index = FindPending(hand);
        if (index < 0)
            return;

        PendingAttack pending = _pendingCues[index];
        if (!pending.Armed || pending.CueFired)
            return;

        if (inAttack)
        {
            pending.SawAttackState = true;
            _pendingCues[index] = pending;
            if (normalizedTime >= pending.CueNormalizedTime)
                ExecutePendingCue(index);
            return;
        }

        if (!pending.SawAttackState)
            return;

        CancelPendingAt(index);
    }

    bool HasAttackOverlayWatch =>
        _locAnim != null && _locAnim.HasAttackTrigger;

    void ArmPendingCue(
        WeaponAction action,
        CharacterBodyHost targetHost,
        float offenseFactor)
    {
        WeaponAttack attack = AttackFor(action);

        float cueTime = attack != null
            ? attack.CueNormalizedTime
            : WeaponAttack.DefaultCueNormalizedTime;

        int index = FindPending(_activeWieldHand);
        if (index < 0)
            index = FindEmptyPending();
        if (index < 0)
            index = 0;

        _pendingCues[index] = new PendingAttack
        {
            Armed = true,
            CueFired = false,
            SawAttackState = false,
            Action = action,
            Hand = _activeWieldHand,
            Target = targetHost,
            OffenseFactor = Mathf.Max(0f, offenseFactor),
            Attack = attack,
            CueNormalizedTime = cueTime,
            ItemId = _itemId ?? string.Empty,
            Instance = _wieldedInstance,
            Stack = _wieldedStack
        };
    }

    WeaponAttack AttackFor(WeaponAction action)
    {
        if (_presentation != null &&
            _presentation.TryGetEntry(action, out WeaponPresentation.Entry entry))
            return entry.attack;
        return null;
    }

    int FindPending(WieldHand hand)
    {
        for (int i = 0; i < _pendingCues.Length; i++)
        {
            if (_pendingCues[i].Armed && _pendingCues[i].Hand == hand)
                return i;
        }

        return -1;
    }

    int FindEmptyPending()
    {
        for (int i = 0; i < _pendingCues.Length; i++)
        {
            if (!_pendingCues[i].Armed)
                return i;
        }

        return -1;
    }

    void ExecutePendingCue(int index)
    {
        PendingAttack pending = _pendingCues[index];
        if (!pending.Armed || pending.CueFired)
            return;

        pending.CueFired = true;
        pending.Armed = false;
        _pendingCues[index] = pending;

        var context = new ActionHandlerContext(
            pending.Action,
            pending.Hand,
            pending.Attack,
            pending.Target,
            pending.OffenseFactor,
            pending.ItemId,
            pending.Instance,
            pending.Stack);

        IActionHandler handler = ActionHandlerRegistry.Resolve(pending.Attack, pending.Action);
        if (handler == null)
        {
            ItemData item = ItemFor(pending.ItemId);
            Vector3 origin = ResolveOrigin();
            EmitJudgedGate(
                context,
                WeaponActionUtil.ResolveMode(pending.Action),
                AttackPerformResult.Unsupported,
                item,
                origin);
            return;
        }

        handler.Execute(this, context);
    }

    void CancelPendingAt(int index)
    {
        _pendingCues[index] = default;
    }

    void CancelAllPendingCues()
    {
        for (int i = 0; i < _pendingCues.Length; i++)
            _pendingCues[i] = default;
    }

    public void EmitJudgedGate(
        in ActionHandlerContext context,
        WeaponResolveMode resolveMode,
        AttackPerformResult result,
        ItemData item,
        Vector3 origin)
    {
        Vector3 impact = ResolveOutcomeImpact(context.Target, origin, item, context.Action);
        EmitJudged(
            context,
            resolveMode,
            result,
            context.Target,
            string.Empty,
            0,
            origin,
            impact);
    }

    public void ResolveCommittedHit(
        in ActionHandlerContext context,
        WeaponResolveMode resolveMode,
        ItemData item,
        Vector3 origin,
        bool consumeAmmo)
    {
        CharacterBodyHost targetHost = context.Target;
        Collider targetCollider = targetHost.GetComponentInChildren<Collider>();
        Vector3 targetCenter = ResolveBodyCenter(targetHost.transform, targetCollider);

        if (!AimPartResolver.TryResolve(
                targetHost.Body,
                _aimIntent != null ? _aimIntent.PreferredPartId : BodyPartIds.Torso,
                out string aimedPart))
        {
            EmitJudgedGate(context, resolveMode, AttackPerformResult.NoTarget, item, origin);
            return;
        }

        Vector3 impact = ResolveImpactPoint(targetCollider, targetCenter, origin);
        CommitAttempt(context, item, consumeAmmo);

        ICharacterSkills skills = _skillsHost != null ? _skillsHost.Skills : null;
        string damageTag = context.Attack != null
            ? context.Attack.DamageTag
            : AttackDamageTags.DefaultFor(context.Action);
        string skillId = CombatMath.SkillIdForTag(item, damageTag);
        int skillLevel = skills != null && !string.IsNullOrEmpty(skillId)
            ? skills.Level(skillId)
            : 0;
        int strength = skills != null ? skills.Level(AttributeIds.Str) : StrengthBaselineFallback;
        float factor = context.OffenseFactor;
        float hitChance = CombatMath.HitChance(item, context.Action, skillLevel, aimedPart)
            * factor
            * ResolveAttackerWearEncAccuracyFactor()
            * ResolveAttackerEnvAccuracyFactor();

        if (UnityEngine.Random.value > hitChance)
        {
            EmitJudged(
                context,
                resolveMode,
                AttackPerformResult.Miss,
                targetHost,
                aimedPart,
                0,
                origin,
                impact);
            return;
        }

        int damage = Mathf.Max(
            0,
            Mathf.RoundToInt(CombatMath.DamageForTag(item, damageTag, strength, skillLevel) * factor));
        damage = WearCombatDefense.MitigateDamage(
            ResolveTargetWear(targetHost),
            aimedPart,
            damage,
            damageTag);
        BodyPartEffect[] seeds = BuildSeeds(context.Attack);
        BodyDamageService.ApplyHit(targetHost.Body, aimedPart, damage, seeds);
        EmitJudged(
            context,
            resolveMode,
            AttackPerformResult.Performed,
            targetHost,
            aimedPart,
            damage,
            origin,
            impact);
    }

    public void CommitAttempt(
        in ActionHandlerContext context,
        ItemData item,
        bool consumeAmmo)
    {
        float cooldown = CombatMath.AttackIntervalSeconds(item, context.Action);
        BeginCooldown(context.Hand, cooldown);
        if (consumeAmmo)
            WeaponChamber.TryConsume(context.Instance);
        Practice(item, context.Attack, context.Action);
    }

    public void EmitJudged(
        in ActionHandlerContext context,
        WeaponResolveMode resolveMode,
        AttackPerformResult result,
        CharacterBodyHost target,
        string aimedPartId,
        int damage,
        Vector3 origin,
        Vector3 impact)
    {
        string impactTag = ResolveImpactTag(context.Attack, null);
        var outcome = new AttackOutcome(
            context.Action,
            context.Hand,
            resolveMode,
            result,
            target,
            aimedPartId,
            damage,
            origin,
            impact,
            impactTag,
            context.Attack);
        AttackJudged?.Invoke(outcome);
        AnyAttackJudged?.Invoke(outcome);
    }

    static string ResolveImpactTag(WeaponAttack attack, string emitted)
    {
        if (!string.IsNullOrEmpty(emitted))
            return emitted;
        if (attack != null)
        {
            if (!string.IsNullOrEmpty(attack.ImpactTag))
                return attack.ImpactTag;
            if (!string.IsNullOrEmpty(attack.FallbackImpactTag))
                return attack.FallbackImpactTag;
        }

        return AttackImpactTags.Fallback;
    }

    void BeginCooldown(WieldHand hand, float seconds) =>
        _cooldownRemaining[CooldownIndex(hand)] = Mathf.Max(0f, seconds);

    static int CooldownIndex(WieldHand hand)
    {
        int index = (int)hand;
        if ((uint)index >= HandCooldownSlotCount)
            return (int)WieldHand.Right;
        return index;
    }

    void Practice(ItemData item, WeaponAttack attack, WeaponAction action)
    {
        if (_skillsHost == null)
            return;
        string damageTag = attack != null
            ? attack.DamageTag
            : AttackDamageTags.DefaultFor(action);
        string skillId = CombatMath.SkillIdForTag(item, damageTag);
        int xp = CombatMath.PracticeXp(action);
        if (string.IsNullOrEmpty(skillId) || xp <= 0)
            return;
        _skillsHost.Skills?.AddPractice(skillId, xp);
    }

    static BodyPartEffect[] BuildSeeds(WeaponAttack attack)
    {
        if (attack == null ||
            attack.EffectSeeds == null ||
            attack.EffectSeeds.Length == 0)
            return null;

        var seeds = new BodyPartEffect[attack.EffectSeeds.Length];
        for (int i = 0; i < attack.EffectSeeds.Length; i++)
        {
            WeaponAttack.EffectSeed seed = attack.EffectSeeds[i];
            if (seed == null)
            {
                seeds[i] = default;
                continue;
            }

            seeds[i] = new BodyPartEffect(
                seed.effectId,
                seed.intensity,
                seed.remainingSeconds);
        }

        return seeds;
    }

    struct PendingAttack
    {
        public bool Armed;
        public bool CueFired;
        public bool SawAttackState;
        public WeaponAction Action;
        public WieldHand Hand;
        public CharacterBodyHost Target;
        public float OffenseFactor;
        public WeaponAttack Attack;
        public float CueNormalizedTime;
        public string ItemId;
        public ItemInstance Instance;
        public ItemStack Stack;
    }

    void OnSkillsRefreshed() => RebuildAvailableActions();

    void RefreshPresentationFromCatalog()
    {
        if (_catalog == null)
            return;

        WeaponPresentation resolved = _catalog.Resolve(_itemId, CurrentItem);
        if (resolved == _presentation)
            return;

        _presentation = resolved;
        if (_presentation != null)
            _presentation.RebuildSupportedActions();
        PresentationChanged?.Invoke();
    }

    void RebuildAvailableActions()
    {
        WeaponActionMask previous = AvailableActions;
        AvailableActions = WeaponActionRows.Available(_presentation);
        if (previous != AvailableActions)
            AvailableActionsChanged?.Invoke();
        ApplySelectedFromInstance();
    }

    void ApplySelectedFromInstance()
    {
        WeaponAction next = WeaponActionRows.ResolveSelected(_wieldedInstance, _presentation);
        if (next == _selectedAction)
            return;

        _selectedAction = next;
        SelectedActionChanged?.Invoke();
    }

    void WriteSelectedToInstance(WeaponAction action)
    {
        if (_wieldedInstance == null)
            return;
        _wieldedInstance.SelectedAction = action;
    }
}
