// ============================================================
// CharacterAttacker — 무기 액션 시전 (가용 마스크·명중·숙련)
// ============================================================

using System;
using Garunnir.Runtime.Gameplay.Data;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterAimIntent))]
[RequireComponent(typeof(CharacterSkillsHost))]
public sealed class CharacterAttacker : MonoBehaviour
{
    static readonly WeaponAction[] ActionOrder =
    {
        WeaponAction.Swing,
        WeaponAction.Stab,
        WeaponAction.Trigger
    };

    const float AimHeight = 0.15f;
    const float MinRayDistance = 0.001f;
    const float SurfaceProbeMargin = 1f;
    const float FallbackImpactRadius = 0.4f;

    [SerializeField] WeaponProfile _weapon;
    [SerializeField] LayerMask _rangedObstructionMask = ~0;
    [SerializeField] TimeScaleChannel _timeChannel = TimeScaleChannel.World;
    [SerializeField] WeaponAction _selectedAction = WeaponAction.Swing;

    CharacterAimIntent _aimIntent;
    CharacterSkillsHost _skillsHost;
    Collider _selfCollider;
    readonly float[] _cooldownRemaining = new float[3];

    public event Action AvailableActionsChanged;
    public event Action SelectedActionChanged;

    /// <summary>실제로 시전된 공격(Performed/Miss)의 판정 결과. 연출 계층이 구독한다.</summary>
    public event Action<AttackOutcome> AttackResolved;

    public WeaponProfile Weapon => _weapon;
    public WeaponActionMask AvailableActions { get; private set; }
    public WeaponAction SelectedAction => _selectedAction;

    void Awake()
    {
        _aimIntent = GetComponent<CharacterAimIntent>();
        _skillsHost = GetComponent<CharacterSkillsHost>();
        _selfCollider = GetComponentInChildren<Collider>();
        if (_weapon != null)
            _weapon.RebuildSupportedActions();
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

    public void SetWeapon(WeaponProfile weapon)
    {
        _weapon = weapon;
        if (_weapon != null)
            _weapon.RebuildSupportedActions();
        RebuildAvailableActions();
        ClampSelectedAction();
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
        action = WeaponAction.Swing;
        if (_weapon == null)
            return false;

        WeaponAction best = default;
        bool found = false;
        float bestRange = float.MaxValue;

        for (int i = 0; i < ActionOrder.Length; i++)
        {
            WeaponAction candidate = ActionOrder[i];
            if (!CanPerform(candidate))
                continue;
            if (!_weapon.TryGetEntry(candidate, out WeaponProfile.Entry entry))
                continue;
            if (GetCooldown(candidate) > 0f)
                continue;
            if (distance > entry.range)
                continue;

            if (!found || entry.range < bestRange)
            {
                found = true;
                best = candidate;
                bestRange = entry.range;
            }
        }

        if (!found)
            return false;

        action = best;
        return true;
    }

    public AttackPerformResult TryPerform(
        WeaponAction action,
        CharacterBodyHost targetHost)
    {
        if (_weapon == null ||
            !_weapon.TryGetEntry(action, out WeaponProfile.Entry entry))
        {
            Debug.LogWarning(
                $"[CharacterAttacker] Unsupported action {action} on {name}",
                this);
            return AttackPerformResult.Unsupported;
        }

        if (!CanPerform(action))
        {
            Debug.LogWarning(
                $"[CharacterAttacker] Action {action} not available on {name}",
                this);
            return AttackPerformResult.Unsupported;
        }

        if (GetCooldown(action) > 0f)
            return AttackPerformResult.Cooling;

        if (targetHost == null || targetHost.Body == null)
            return AttackPerformResult.NoTarget;

        Vector3 toTarget = targetHost.transform.position - transform.position;
        toTarget.y = 0f;
        float distance = toTarget.magnitude;
        if (distance > entry.range)
            return AttackPerformResult.OutOfRange;

        Collider targetCollider = targetHost.GetComponentInChildren<Collider>();
        Vector3 origin = ResolveBodyCenter(transform, _selfCollider);
        Vector3 targetCenter = ResolveBodyCenter(targetHost.transform, targetCollider);

        if (entry.resolveMode == WeaponResolveMode.RangedRay)
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
                BeginCooldown(action, entry.cooldownSeconds);
                Practice(entry);
                return Resolve(
                    entry,
                    action,
                    AttackPerformResult.Miss,
                    targetHost,
                    string.Empty,
                    0,
                    origin,
                    blocker.point);
            }
        }

        BeginCooldown(action, entry.cooldownSeconds);
        Practice(entry);

        if (!AimPartResolver.TryResolve(
                targetHost.Body,
                _aimIntent != null ? _aimIntent.PreferredPartId : BodyPartIds.Torso,
                out string aimedPart))
            return AttackPerformResult.NoTarget;

        Vector3 impact = ResolveImpactPoint(targetCollider, targetCenter, origin);

        ICharacterSkills skills = _skillsHost != null ? _skillsHost.Skills : null;
        int skillLevel = skills != null && !string.IsNullOrEmpty(entry.skillId)
            ? skills.Level(entry.skillId)
            : 0;
        float hitChance = Mathf.Clamp01(
            (entry.accuracy + skillLevel * entry.accuracyPerSkillLevel) *
            BodyPartHitDifficulty.Get(aimedPart));

        if (UnityEngine.Random.value > hitChance)
        {
            return Resolve(
                entry,
                action,
                AttackPerformResult.Miss,
                targetHost,
                aimedPart,
                0,
                origin,
                impact);
        }

        BodyPartEffect[] seeds = BuildSeeds(entry);
        BodyDamageService.ApplyHit(targetHost.Body, aimedPart, entry.damage, seeds);
        return Resolve(
            entry,
            action,
            AttackPerformResult.Performed,
            targetHost,
            aimedPart,
            entry.damage,
            origin,
            impact);
    }

    AttackPerformResult Resolve(
        WeaponProfile.Entry entry,
        WeaponAction action,
        AttackPerformResult result,
        CharacterBodyHost target,
        string aimedPartId,
        int damage,
        Vector3 origin,
        Vector3 impact)
    {
        AttackResolved?.Invoke(new AttackOutcome(
            action,
            entry.resolveMode,
            result,
            target,
            aimedPartId,
            damage,
            origin,
            impact));
        return result;
    }

    /// <summary>조준·타격 기준 높이. 콜라이더가 있으면 그 중심, 없으면 발치 기준 오프셋.</summary>
    static Vector3 ResolveBodyCenter(Transform owner, Collider collider) =>
        collider != null
            ? collider.bounds.center
            : owner.position + Vector3.up * AimHeight;

    /// <summary>공격자에서 타겟을 향한 접점. 표면을 못 잡으면 중심에서 반경만큼 물린다.</summary>
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

    void Practice(WeaponProfile.Entry entry)
    {
        if (_skillsHost == null ||
            string.IsNullOrEmpty(entry.skillId) ||
            entry.practiceXp <= 0)
            return;
        _skillsHost.Skills?.AddPractice(entry.skillId, entry.practiceXp);
    }

    static BodyPartEffect[] BuildSeeds(WeaponProfile.Entry entry)
    {
        if (entry.effectSeeds == null || entry.effectSeeds.Length == 0)
            return null;

        var seeds = new BodyPartEffect[entry.effectSeeds.Length];
        for (int i = 0; i < entry.effectSeeds.Length; i++)
        {
            WeaponProfile.EffectSeed seed = entry.effectSeeds[i];
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

    void RebuildAvailableActions()
    {
        WeaponActionMask previous = AvailableActions;
        WeaponActionMask mask = WeaponActionMask.None;
        ICharacterSkills skills = _skillsHost != null ? _skillsHost.Skills : null;

        if (_weapon != null && _weapon.Entries != null)
        {
            WeaponProfile.Entry[] entries = _weapon.Entries;
            for (int i = 0; i < entries.Length; i++)
            {
                WeaponProfile.Entry entry = entries[i];
                if (entry == null)
                    continue;

                int level = 0;
                if (skills != null && !string.IsNullOrEmpty(entry.skillId))
                    level = skills.Level(entry.skillId);
                if (level < entry.minimumSkillLevel)
                    continue;

                mask |= WeaponActionUtil.ToMask(entry.action);
            }
        }

        AvailableActions = mask;
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
