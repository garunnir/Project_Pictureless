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
    [SerializeField] WieldHand _activeWieldHand = WieldHand.TwoHand;
    ItemInstance _wieldedInstance;
    ItemStack _wieldedStack;

    CharacterAimIntent _aimIntent;
    CharacterSkillsHost _skillsHost;
    PlayerGearHost _gearHost;
    CharacterClimateHost _climateHost;
    CharacterActionHost _actionHost;
    CharacterState _characterState;
    PlayerAimController _aimController;
    CharacterLocomotionAnim _locAnim;
    Collider _selfCollider;
    readonly Collider[] _meleeColliders = new Collider[MeleeHitbox.BufferSize];
    readonly Vector3[] _debugContacts = new Vector3[MeleeHitbox.BufferSize];
    MeleeHitboxPose _debugCuePose;
    int _debugCueHitCount;
    int _debugContactCount;
    float _debugCueUntilUnscaled;
    readonly float[] _actionCooldownRemaining = new float[HandCooldownSlotCount];
    readonly float[] _actionCooldownDuration = new float[HandCooldownSlotCount];
    readonly float[] _weaponCooldownRemaining = new float[HandCooldownSlotCount];
    readonly float[] _weaponCooldownDuration = new float[HandCooldownSlotCount];
    readonly float[] _recoilRemaining = new float[HandCooldownSlotCount];
    float _aim01;
    readonly PendingAttack[] _pendingCues = new PendingAttack[PendingCueSlotCount];
    readonly string[] _hitChannelScratch = new string[AttackDamageTags.MaxChannels];

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

    /// <summary>공격 클립 cue 도달. Impact Recoil 연출용.</summary>
    public event Action<WieldHand, WeaponAction> AttackCueFired;

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

    public bool IsActionBusy
    {
        get
        {
            if (HasPendingAttackCue)
                return true;
            return HasAnyRemaining(_actionCooldownRemaining) ||
                   HasAnyRemaining(_weaponCooldownRemaining);
        }
    }

    public float CooldownProgress01
    {
        get
        {
            float bestRemaining = 0f;
            float bestDuration = 0f;
            ConsiderProgress(
                _actionCooldownRemaining,
                _actionCooldownDuration,
                ref bestRemaining,
                ref bestDuration);
            ConsiderProgress(
                _weaponCooldownRemaining,
                _weaponCooldownDuration,
                ref bestRemaining,
                ref bestDuration);

            if (bestDuration <= 0f)
                return HasPendingAttackCue ? 0f : 1f;
            return 1f - Mathf.Clamp01(bestRemaining / bestDuration);
        }
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

    /// <summary>CharacterState.IsAiming. NPC Attack은 SetAimDir. CharacterState 없으면 AimIntent.AimHeld.</summary>
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
        TryGetComponent(out _climateHost);
        TryGetComponent(out _actionHost);
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
        Camera.onPostRender += OnCameraPostRender;
    }

    void OnDisable()
    {
        Camera.onPostRender -= OnCameraPostRender;
        ICharacterSkills skills = _skillsHost != null ? _skillsHost.Skills : null;
        if (skills != null)
            skills.Refreshed -= OnSkillsRefreshed;
        CancelAllPendingCues();
        ApplyRaiseFromHandler(false);
    }

    void Update()
    {
        DrawMeleeHitboxDebugLines();

        float dt = TimeScaleService.Delta(_timeChannel);
        if (_actionHost != null)
            dt *= _actionHost.ActionTickScale;
        if (dt <= 0f)
            return;

        TickCooldownSlots(_actionCooldownRemaining, dt);
        TickCooldownSlots(_weaponCooldownRemaining, dt);
        TickRecoilSlots(dt);
        TickAimProgress(dt);

        TickRaiseGuard();
        DrawMeleeHitboxDebugLines();
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

        float range = CombatMath.RangeMeters(CurrentItem, _selectedAction, WeaponChamber.ResolveAmmo(_wieldedStack, _wieldedInstance));
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
        AttackPerformResult gate = GateAction(action, item);
        // Cooling/pending/원거리 NoAmmo만 시전을 막는다.
        // 타깃·사거리는 시전 게이트가 아님 (근접=cue 히트박스, 원거리=조준축 발사).
        if (gate != AttackPerformResult.Performed)
            return gate;

        BeginActionCooldown(_activeWieldHand, ResolveActionCooldown(action));
        ArmPendingCue(action, targetHost, offenseFactor);

        AttackPerformResult signal = ResolveActionSignal(
            action, resolveMode, gate, targetHost, origin, item);

        if (!HasAttackOverlayWatch)
            NotifyAttackCue();

        return signal;
    }

    AttackPerformResult GateAction(WeaponAction action, ItemData item)
    {
        if (GetCooldown(_activeWieldHand) > 0f)
            return AttackPerformResult.Cooling;

        // cue 대기 중 재시전 → pending 리셋·시전 VFX 연타 방지
        if (HasPendingFor(_activeWieldHand))
            return AttackPerformResult.Cooling;

        if (action == WeaponAction.Raise)
            return AttackPerformResult.Performed;

        if (WeaponActionUtil.IsRanged(action) &&
            !WeaponChamber.CanCommitFire(item, _wieldedInstance, _wieldedStack, AttackFor(action)))
            return AttackPerformResult.NoAmmo;

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
        if (_climateHost == null)
            return 1f;
        BodyTemp bodyTemp = _climateHost.BodyTemperature;
        WearEnvExposure env = _climateHost.EnvExposure;
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

        float fallbackDist = CombatMath.RangeMeters(
            item,
            action,
            WeaponChamber.ResolveAmmo(_wieldedStack, _wieldedInstance));
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
        AttackPerformResult gate = GateAction(action, item);
        if (gate == AttackPerformResult.Performed)
            BeginActionCooldown(_activeWieldHand, ResolveActionCooldown(action));
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

    public float GetWeaponCooldown(WieldHand hand) =>
        _weaponCooldownRemaining[CooldownIndex(hand)];

    public float GetActionCooldown(WieldHand hand) =>
        _actionCooldownRemaining[CooldownIndex(hand)];

    /// <summary>동작 쿨·무기 쿨 중 큰 쪽. cue 핸들러는 <see cref="GetWeaponCooldown"/>만 본다.</summary>
    public float GetCooldown(WieldHand hand) =>
        Mathf.Max(GetActionCooldown(hand), GetWeaponCooldown(hand));

    public ItemData ItemFor(string itemId) =>
        string.IsNullOrEmpty(itemId) ? null : GameplayData.GetItem(itemId);

    public Vector3 ResolveOrigin() =>
        ResolveBodyCenter(transform, _selfCollider);

    /// <summary>원거리 발사 방향. 엔티티 락온 없음 — 조준축과 동일.</summary>
    public Vector3 ResolveFireDirection() => ResolveSwingAxis();

    public float GetRecoil(WieldHand hand) =>
        _recoilRemaining[CooldownIndex(hand)];

    public float Aim01 => _aim01;

    public float RangedEffectiveDispersion(WieldHand hand, ItemData item, ItemData ammo) =>
        CombatMath.EffectiveDispersion(item, ammo, GetRecoil(hand), ResolveAim01(item));

    float ResolveAim01(ItemData item)
    {
        if (_aimIntent != null && _aimIntent.AimHeld)
            return 1f;
        if (!IsAiming)
            return 0f;
        if (CombatMath.AimSpeedOf(item) <= 0)
            return 1f;
        return _aim01;
    }

    /// <summary>effective dispersion yaw. 킥은 발사 후 <see cref="AddRecoilKick"/>.</summary>
    public Vector3 ResolveSpreadFireDirection(WieldHand hand, ItemData item, ItemData ammo)
    {
        return CombatMath.SpreadFireDirection(
            ResolveFireDirection(),
            RangedEffectiveDispersion(hand, item, ammo));
    }

    public void AddRecoilKick(WieldHand hand, ItemData item, ItemData ammo)
    {
        int index = CooldownIndex(hand);
        _recoilRemaining[index] += CombatMath.RecoilKickUnits(item, ammo);
    }

    public Vector3 ResolveSwingAxis()
    {
        Vector3 dir = Vector3.zero;
        if (_characterState != null)
        {
            dir = _characterState.SightDir;
            if (dir.sqrMagnitude < 1e-6f)
                dir = _characterState.InteractionDir;
            if (dir.sqrMagnitude < 1e-6f)
                dir = _characterState.GetFacingDir();
        }

        if (dir.sqrMagnitude < 1e-6f)
            dir = transform.forward;

        dir.y = 0f;
        if (dir.sqrMagnitude < 1e-6f)
            return Vector3.forward;
        return dir.normalized;
    }

    public bool IsOwnCollider(Collider collider)
    {
        if (collider == null)
            return false;
        Transform root = transform;
        return collider.transform == root || collider.transform.IsChildOf(root);
    }

    public int CollectMeleeHits(
        ItemData item,
        WeaponAction action,
        WeaponAttack attack,
        CharacterBodyHost[] hosts,
        MeleeHitContact[] contacts)
    {
        int hitCount = MeleeHitbox.Collect(
            this, item, action, attack, _meleeColliders, hosts, contacts);
        if (ShouldDrawMeleeHitbox &&
            MeleeHitbox.TryGetPose(this, item, action, attack, out MeleeHitboxPose pose))
        {
            _debugCuePose = pose;
            _debugCueHitCount = hitCount;
            _debugContactCount = hitCount;
            int cap = hitCount < _debugContacts.Length ? hitCount : _debugContacts.Length;
            for (int i = 0; i < cap; i++)
                _debugContacts[i] = contacts[i].WorldPoint;
            _debugCueUntilUnscaled = Time.unscaledTime + MeleeHitbox.DebugCueHoldSeconds;
        }

        return hitCount;
    }

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

    public WeaponAttack ResolveAttack(WeaponAction action) => AttackFor(action);

    public bool AllowsImpactReaction(WeaponAction action, ArmImpactKind kind) =>
        WeaponAttack.AllowsImpactReaction(AttackFor(action), kind);

    WeaponAttack AttackFor(WeaponAction action) =>
        EntryFor(action)?.attack;

    WeaponPresentation.Entry EntryFor(WeaponAction action)
    {
        if (_presentation != null &&
            _presentation.TryGetEntry(action, out WeaponPresentation.Entry entry))
            return entry;
        return null;
    }

    float ResolveActionCooldown(WeaponAction action)
    {
        WeaponPresentation.Entry entry = EntryFor(action);
        return entry != null ? entry.ActionCooldownSeconds : 0f;
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

        AttackCueFired?.Invoke(pending.Hand, pending.Action);

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
            impact,
            item);
    }

    public void ResolveCommittedHit(
        in ActionHandlerContext context,
        WeaponResolveMode resolveMode,
        ItemData item,
        Vector3 origin,
        bool consumeAmmo,
        ItemData ammo = null,
        bool applyCooldown = true,
        bool rollHitChance = true,
        bool practice = true,
        float weaponReach01 = 0f,
        Vector3? impactOverride = null,
        float rangedEffectiveDispersion = -1f)
    {
        CharacterBodyHost targetHost = context.Target;
        if (targetHost == null || targetHost.Body == null)
        {
            EmitJudgedGate(context, resolveMode, AttackPerformResult.NoTarget, item, origin);
            return;
        }

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

        Vector3 impact = impactOverride ?? ResolveImpactPoint(targetCollider, targetCenter, origin);
        ammo ??= WeaponChamber.ResolveAmmo(context.Stack, context.Instance);
        CommitAttempt(context, item, consumeAmmo, ammo, applyCooldown: applyCooldown, practice: practice);

        ICharacterSkills skills = _skillsHost != null ? _skillsHost.Skills : null;
        int channelCount = AttackDamageTags.WriteChannels(
            item, context.Action, _hitChannelScratch, ammo);
        string hitTag = channelCount > 0
            ? _hitChannelScratch[0]
            : AttackDamageTags.Fallback;
        string skillId = CombatMath.SkillIdForTag(item, hitTag);
        int skillLevel = skills != null && !string.IsNullOrEmpty(skillId)
            ? skills.Level(skillId)
            : 0;
        int strength = skills != null ? skills.Level(AttributeIds.Str) : StrengthBaselineFallback;
        float factor = context.OffenseFactor;

        if (rollHitChance)
        {
            float hitChance = CombatMath.HitChance(
                    item,
                    context.Action,
                    skillLevel,
                    aimedPart,
                    ammo,
                    rangedEffectiveDispersion)
                * factor
                * ResolveAttackerWearEncAccuracyFactor()
                * ResolveAttackerEnvAccuracyFactor();

            if (UnityEngine.Random.value > hitChance)
                aimedPart = AimPartResolver.ScatterToNeighbor(targetHost.Body, aimedPart);
        }

        EquipmentWearState wear = ResolveTargetWear(targetHost);
        int damage = 0;
        for (int i = 0; i < channelCount; i++)
        {
            string damageTag = _hitChannelScratch[i];
            int channelDamage = Mathf.Max(
                0,
                Mathf.RoundToInt(
                    CombatMath.DamageForTag(item, damageTag, strength, skillLevel, ammo) * factor));
            channelDamage = WearCombatDefense.MitigateDamage(
                wear,
                aimedPart,
                channelDamage,
                damageTag);
            BodyPartEffect[] seeds =
                string.Equals(damageTag, AttackDamageTags.Cut, StringComparison.Ordinal)
                    ? BuildSeeds(_presentation, context.Action, context.Attack)
                    : null;
            BodyDamageService.ApplyHit(targetHost.Body, aimedPart, channelDamage, seeds);
            damage += channelDamage;
        }

        EmitJudged(
            context,
            resolveMode,
            AttackPerformResult.Performed,
            targetHost,
            aimedPart,
            damage,
            origin,
            impact,
            item,
            ammo,
            weaponReach01);
    }

    public void CommitAttempt(
        in ActionHandlerContext context,
        ItemData item,
        bool consumeAmmo,
        ItemData ammo = null,
        bool applyCooldown = true,
        bool practice = true)
    {
        if (applyCooldown && !WeaponActionUtil.IsRanged(context.Action))
        {
            float cooldown = CombatMath.AttackIntervalSeconds(item, context.Action);
            BeginWeaponCooldown(context.Hand, cooldown);
        }

        if (consumeAmmo)
            WeaponChamber.TryConsume(context.Instance);
        if (practice)
            Practice(item, context.Attack, context.Action, ammo);
    }

    public void EmitJudged(
        in ActionHandlerContext context,
        WeaponResolveMode resolveMode,
        AttackPerformResult result,
        CharacterBodyHost target,
        string aimedPartId,
        int damage,
        Vector3 origin,
        Vector3 impact,
        ItemData item = null,
        ItemData ammo = null,
        float weaponReach01 = 0f)
    {
        if (item == null)
            item = ItemFor(context.ItemId);
        ammo ??= WeaponChamber.ResolveAmmo(context.Stack, context.Instance);
        int n = AttackDamageTags.WriteChannels(item, context.Action, _hitChannelScratch, ammo);
        string hitTag = n > 0 ? _hitChannelScratch[0] : AttackDamageTags.Fallback;
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
            hitTag,
            context.Attack,
            weaponReach01);
        AttackJudged?.Invoke(outcome);
        AnyAttackJudged?.Invoke(outcome);
    }

    void BeginActionCooldown(WieldHand hand, float seconds)
    {
        if (seconds <= 0f)
            return;
        BeginCooldownSlot(
            _actionCooldownRemaining,
            _actionCooldownDuration,
            hand,
            seconds);
    }

    void BeginWeaponCooldown(WieldHand hand, float seconds)
    {
        BeginCooldownSlot(
            _weaponCooldownRemaining,
            _weaponCooldownDuration,
            hand,
            seconds);
    }

    void BeginCooldownSlot(
        float[] remaining,
        float[] durationSlots,
        WieldHand hand,
        float seconds)
    {
        int index = CooldownIndex(hand);
        float duration = Mathf.Max(0f, seconds);
        remaining[index] = duration;
        durationSlots[index] = duration;
    }

    static void TickCooldownSlots(float[] remaining, float dt)
    {
        for (int i = 0; i < remaining.Length; i++)
        {
            if (remaining[i] <= 0f)
                continue;
            remaining[i] = Mathf.Max(0f, remaining[i] - dt);
        }
    }

    void TickRecoilSlots(float dt)
    {
        float recover = CombatMath.RecoilRecoverPerSecond * dt;
        if (recover <= 0f)
            return;

        for (int i = 0; i < _recoilRemaining.Length; i++)
        {
            if (_recoilRemaining[i] <= 0f)
                continue;
            _recoilRemaining[i] = Mathf.Max(0f, _recoilRemaining[i] - recover);
        }
    }

    void TickAimProgress(float dt)
    {
        if (_aimIntent != null && _aimIntent.AimHeld)
        {
            _aim01 = 1f;
            return;
        }

        if (!IsAiming)
        {
            _aim01 = 0f;
            return;
        }

        int aimSpeed = CombatMath.AimSpeedOf(CurrentItem);
        if (aimSpeed <= 0)
        {
            _aim01 = 1f;
            return;
        }

        _aim01 = Mathf.Min(1f, _aim01 + dt * CombatMath.AimProgressPerSecond(aimSpeed));
    }

    static bool HasAnyRemaining(float[] remaining)
    {
        for (int i = 0; i < remaining.Length; i++)
        {
            if (remaining[i] > 0f)
                return true;
        }

        return false;
    }

    static void ConsiderProgress(
        float[] remaining,
        float[] durationSlots,
        ref float bestRemaining,
        ref float bestDuration)
    {
        for (int i = 0; i < remaining.Length; i++)
        {
            if (remaining[i] <= bestRemaining)
                continue;
            bestRemaining = remaining[i];
            bestDuration = durationSlots[i];
        }
    }

    static int CooldownIndex(WieldHand hand)
    {
        int index = (int)hand;
        if ((uint)index >= HandCooldownSlotCount)
            return (int)WieldHand.Right;
        return index;
    }

    void Practice(ItemData item, WeaponAttack attack, WeaponAction action, ItemData ammo = null)
    {
        if (_skillsHost == null)
            return;
        _ = attack;
        string damageTag = AttackDamageTags.Resolve(item, action, ammo);
        string skillId = CombatMath.SkillIdForTag(item, damageTag);
        int xp = CombatMath.PracticeXp(action);
        if (string.IsNullOrEmpty(skillId) || xp <= 0)
            return;
        _skillsHost.Skills?.AddPractice(skillId, xp);
    }

    static BodyPartEffect[] BuildSeeds(
        WeaponPresentation presentation,
        WeaponAction action,
        WeaponAttack attack)
    {
        if (presentation != null &&
            presentation.TryGetEntry(action, out WeaponPresentation.Entry entry) &&
            entry?.effectSeeds != null &&
            entry.effectSeeds.Length > 0)
        {
            var seeds = new BodyPartEffect[entry.effectSeeds.Length];
            for (int i = 0; i < entry.effectSeeds.Length; i++)
            {
                WeaponPresentation.EffectSeed seed = entry.effectSeeds[i];
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

        if (attack == null ||
            attack.EffectSeeds == null ||
            attack.EffectSeeds.Length == 0)
            return null;

        var attackSeeds = new BodyPartEffect[attack.EffectSeeds.Length];
        for (int i = 0; i < attack.EffectSeeds.Length; i++)
        {
            WeaponAttack.EffectSeed seed = attack.EffectSeeds[i];
            if (seed == null)
            {
                attackSeeds[i] = default;
                continue;
            }

            attackSeeds[i] = new BodyPartEffect(
                seed.effectId,
                seed.intensity,
                seed.remainingSeconds);
        }

        return attackSeeds;
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

    bool ShouldDrawMeleeHitbox => Config.DebugMode.MeleeHitbox;

    void DrawMeleeHitboxDebugLines()
    {
        if (!ShouldDrawMeleeHitbox)
            return;
        if (!TryGetMeleeHitboxDebugDraw(out MeleeHitboxPose pose, out Color color, out bool cueHold))
            return;

        MeleeHitbox.DrawDebugWire(pose, color, 0f);
        if (!cueHold)
            return;
        for (int i = 0; i < _debugContactCount; i++)
            MeleeHitbox.DrawDebugContact(_debugContacts[i], 0f);
    }

    void OnDrawGizmos()
    {
        if (!ShouldDrawMeleeHitbox)
            return;
        if (!TryGetMeleeHitboxDebugDraw(out MeleeHitboxPose pose, out Color color, out bool cueHold))
            return;

        MeleeHitbox.DrawGizmoWire(pose, color);
        if (!cueHold)
            return;
        for (int i = 0; i < _debugContactCount; i++)
            MeleeHitbox.DrawGizmoContact(_debugContacts[i]);
    }

    void OnCameraPostRender(Camera cam)
    {
        if (cam == null)
            return;
        if (cam.cameraType == CameraType.Preview || cam.cameraType == CameraType.Reflection)
            return;
        if ((cam.cullingMask & (1 << gameObject.layer)) == 0)
            return;
        if (!ShouldDrawMeleeHitbox)
            return;
        if (!TryGetMeleeHitboxDebugDraw(out MeleeHitboxPose pose, out Color color, out bool cueHold))
            return;

        MeleeHitbox.DrawGl(
            pose,
            color,
            cueHold ? _debugContacts : null,
            cueHold ? _debugContactCount : 0,
            cam);
    }

    bool TryGetMeleeHitboxDebugDraw(out MeleeHitboxPose pose, out Color color, out bool cueHold)
    {
        pose = default;
        color = MeleeHitbox.PreviewWire;
        cueHold = Time.unscaledTime < _debugCueUntilUnscaled && _debugCuePose.IsValid;
        if (cueHold)
        {
            pose = _debugCuePose;
            color = _debugCueHitCount > 0 ? MeleeHitbox.CueHitWire : MeleeHitbox.CueMissWire;
            return true;
        }

        if (WeaponActionUtil.IsRanged(_selectedAction) ||
            WeaponActionUtil.SuppressesAttackTrigger(_selectedAction))
            return false;

        if (!MeleeHitbox.TryGetPose(
                this,
                CurrentItem,
                _selectedAction,
                AttackFor(_selectedAction),
                out pose))
            return false;

        color = MeleeHitbox.PreviewWire;
        return true;
    }
}
