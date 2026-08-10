// ============================================================
// CharacterAttacker — 무기 액션 시전 (ItemData 수치 + Presentation 연출)
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
    static readonly WeaponAction[] ActionOrder =
    {
        WeaponAction.Bashing,
        WeaponAction.Cutting,
        WeaponAction.Gun
    };

    const float AimHeight = 0.15f;
    const float MinRayDistance = 0.001f;
    const float SurfaceProbeMargin = 1f;
    const float FallbackImpactRadius = 0.4f;

    [FormerlySerializedAs("_weapon")]
    [SerializeField] WeaponPresentation _presentation;
    [Tooltip("GameplayData ItemData id. 비우면 비무장.")]
    [SerializeField] string _itemId;
    [SerializeField] WeaponPresentationCatalog _catalog;
    [Tooltip("장전 시스템 전 스텁. Gun 발사마다 1 소모. 자동 장전 없음.")]
    [SerializeField, Min(0)] int _loadedRounds;
    [SerializeField] LayerMask _rangedObstructionMask = ~0;
    [SerializeField] TimeScaleChannel _timeChannel = TimeScaleChannel.World;
    [SerializeField] WeaponAction _selectedAction = WeaponAction.Bashing;
    [SerializeField] WieldHand _activeWieldHand = WieldHand.Right;

    CharacterAimIntent _aimIntent;
    CharacterSkillsHost _skillsHost;
    PlayerGearHost _gearHost;
    Collider _selfCollider;
    readonly float[] _cooldownRemaining = new float[3];

    public event Action AvailableActionsChanged;
    public event Action SelectedActionChanged;
    public event Action PresentationChanged;
    public event Action ActiveWieldHandChanged;

    /// <summary>실제로 시전된 공격(Performed/Miss)의 판정 결과. 연출 계층이 구독한다.</summary>
    public event Action<AttackOutcome> AttackResolved;

    /// <summary>모든 CharacterAttacker Resolve 공통 훅 (메시지 로그 등).</summary>
    public static event Action<AttackOutcome> AnyAttackResolved;

    public WeaponPresentation Presentation => _presentation;
    public string ItemId => _itemId;
    public int LoadedRounds => _loadedRounds;
    public WeaponActionMask AvailableActions { get; private set; }
    public WeaponAction SelectedAction => _selectedAction;
    public WieldHand ActiveWieldHand => _activeWieldHand;

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
        _selfCollider = GetComponentInChildren<Collider>();
        if (_presentation != null)
            _presentation.RebuildSupportedActions();
        RefreshPresentationFromCatalog();
        RebuildAvailableActions();
        ClampSelectedAction();
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
    }

    /// <summary>들기(Wield) 훅. 카탈로그로 Presentation resolve. rounds는 장전 전 스텁.</summary>
    public void SetWieldedItem(string itemId, int loadedRounds = 0)
    {
        if (string.Equals(_itemId, itemId, StringComparison.Ordinal) &&
            _loadedRounds == loadedRounds)
            return;

        _itemId = itemId ?? string.Empty;
        _loadedRounds = Mathf.Max(0, loadedRounds);
        RefreshPresentationFromCatalog();
        RebuildAvailableActions();
        ClampSelectedAction();
    }

    [Obsolete("Use SetWieldedItem")]
    public void SetEquippedItem(string itemId, int loadedRounds = 0) =>
        SetWieldedItem(itemId, loadedRounds);

    public void SetPresentation(WeaponPresentation presentation)
    {
        if (_presentation == presentation)
            return;

        _presentation = presentation;
        if (_presentation != null)
            _presentation.RebuildSupportedActions();
        RebuildAvailableActions();
        ClampSelectedAction();
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
        SelectedActionChanged?.Invoke();
    }

    public bool TrySelectAction(WeaponAction action)
    {
        if (!CanPerform(action))
            return false;
        if (_selectedAction == action)
            return true;
        _selectedAction = action;
        SelectedActionChanged?.Invoke();
        return true;
    }

    public bool TryGetBestAction(float distance, out WeaponAction action)
    {
        action = WeaponAction.Bashing;
        ItemData item = CurrentItem;

        WeaponAction best = default;
        bool found = false;
        float bestRange = float.MaxValue;

        for (int i = 0; i < ActionOrder.Length; i++)
        {
            WeaponAction candidate = ActionOrder[i];
            if (!CanPerform(candidate))
                continue;
            if (GetCooldown(candidate) > 0f)
                continue;

            float range = CombatMath.RangeMeters(item, candidate);
            if (distance > range)
                continue;

            if (!found || range < bestRange)
            {
                found = true;
                best = candidate;
                bestRange = range;
            }
        }

        if (!found)
            return false;

        action = best;
        return true;
    }

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

        if (GetCooldown(action) > 0f)
            return AttackPerformResult.Cooling;

        ItemData item = CurrentItem;
        if (action == WeaponAction.Gun && !CombatMath.CanFireGun(item, _loadedRounds))
            return AttackPerformResult.NoAmmo;

        if (targetHost == null || targetHost.Body == null)
            return AttackPerformResult.NoTarget;

        Vector3 toTarget = targetHost.transform.position - transform.position;
        toTarget.y = 0f;
        float distance = toTarget.magnitude;
        float range = CombatMath.RangeMeters(item, action);
        if (distance > range)
            return AttackPerformResult.OutOfRange;

        WeaponResolveMode resolveMode = WeaponActionUtil.ResolveMode(action);
        Collider targetCollider = targetHost.GetComponentInChildren<Collider>();
        Vector3 origin = ResolveBodyCenter(transform, _selfCollider);
        Vector3 targetCenter = ResolveBodyCenter(targetHost.transform, targetCollider);
        float cooldown = CombatMath.AttackIntervalSeconds(item, action);
        float factor = Mathf.Max(0f, offenseFactor);

        if (resolveMode == WeaponResolveMode.RangedRay)
        {
            Vector3 dir = targetCenter - origin;
            float rayDist = dir.magnitude;
            if (rayDist > MinRayDistance &&
                Physics.Raycast(
                    origin,
                    dir / rayDist,
                    out RaycastHit blocker,
                    rayDist,
                    _rangedObstructionMask,
                    QueryTriggerInteraction.Ignore) &&
                blocker.collider != null &&
                blocker.collider.transform != targetHost.transform &&
                !blocker.collider.transform.IsChildOf(targetHost.transform))
            {
                BeginCooldown(action, cooldown);
                ConsumeAmmoIfGun(action);
                Practice(item, action);
                return Resolve(
                    action,
                    resolveMode,
                    AttackPerformResult.Miss,
                    targetHost,
                    string.Empty,
                    0,
                    origin,
                    blocker.point);
            }
        }

        BeginCooldown(action, cooldown);
        ConsumeAmmoIfGun(action);
        Practice(item, action);

        if (!AimPartResolver.TryResolve(
                targetHost.Body,
                _aimIntent != null ? _aimIntent.PreferredPartId : BodyPartIds.Torso,
                out string aimedPart))
            return AttackPerformResult.NoTarget;

        Vector3 impact = ResolveImpactPoint(targetCollider, targetCenter, origin);

        ICharacterSkills skills = _skillsHost != null ? _skillsHost.Skills : null;
        string skillId = CombatMath.SkillId(item, action);
        int skillLevel = skills != null && !string.IsNullOrEmpty(skillId)
            ? skills.Level(skillId)
            : 0;
        int strength = skills != null ? skills.Level(AttributeIds.Str) : StrengthBaselineFallback;
        float hitChance = CombatMath.HitChance(item, action, skillLevel, aimedPart)
            * factor
            * ResolveAttackerWearEncAccuracyFactor()
            * ResolveAttackerEnvAccuracyFactor();

        if (UnityEngine.Random.value > hitChance)
        {
            return Resolve(
                action,
                resolveMode,
                AttackPerformResult.Miss,
                targetHost,
                aimedPart,
                0,
                origin,
                impact);
        }

        int damage = Mathf.Max(
            0,
            Mathf.RoundToInt(CombatMath.Damage(item, action, strength, skillLevel) * factor));
        damage = WearCombatDefense.MitigateDamage(
            ResolveTargetWear(targetHost),
            aimedPart,
            damage,
            action);
        BodyPartEffect[] seeds = BuildSeeds(action);
        BodyDamageService.ApplyHit(targetHost.Body, aimedPart, damage, seeds);
        return Resolve(
            action,
            resolveMode,
            AttackPerformResult.Performed,
            targetHost,
            aimedPart,
            damage,
            origin,
            impact);
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

    static Vector3 ResolveBodyCenter(Transform owner, Collider collider) =>
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

    float GetCooldown(WeaponAction action) =>
        _cooldownRemaining[(int)action];

    void BeginCooldown(WeaponAction action, float seconds) =>
        _cooldownRemaining[(int)action] = Mathf.Max(0f, seconds);

    void ConsumeAmmoIfGun(WeaponAction action)
    {
        if (action != WeaponAction.Gun || _loadedRounds <= 0)
            return;
        _loadedRounds--;
    }

    void Practice(ItemData item, WeaponAction action)
    {
        if (_skillsHost == null)
            return;
        string skillId = CombatMath.SkillId(item, action);
        int xp = CombatMath.PracticeXp(action);
        if (string.IsNullOrEmpty(skillId) || xp <= 0)
            return;
        _skillsHost.Skills?.AddPractice(skillId, xp);
    }

    BodyPartEffect[] BuildSeeds(WeaponAction action)
    {
        if (_presentation == null ||
            !_presentation.TryGetEntry(action, out WeaponPresentation.Entry entry) ||
            entry.effectSeeds == null ||
            entry.effectSeeds.Length == 0)
            return null;

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
        AvailableActions = CombatMath.AvailableModes(CurrentItem);
        if (previous != AvailableActions)
            AvailableActionsChanged?.Invoke();
        ClampSelectedAction();
    }

    void ClampSelectedAction()
    {
        if (CanPerform(_selectedAction))
            return;

        if (!WeaponActionUtil.TryFirstAvailable(AvailableActions, out WeaponAction next))
            return;

        if (next == _selectedAction)
            return;

        _selectedAction = next;
        SelectedActionChanged?.Invoke();
    }
}
