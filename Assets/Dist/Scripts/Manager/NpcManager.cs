// ============================================================
// NpcManager — 씬 NPC를 원격 틱 (유닛별 FSM, 프리팹에 뇌 스크립트 없음)
// ============================================================

using System;
using System.Collections.Generic;
using Garunnir.Runtime.Gameplay.Data;
using UnityEngine;

public enum NpcCombatState
{
    Idle = 0,
    Patrol = 1,
    Alert = 2,
    Chase = 3,
    Attack = 4,
    Return = 5,
    Dead = 6
}

public static class NpcAgentDefaults
{
    public const float DetectRadius = 10f;
    public const float LoseRadius = 14f;
    public const float AttackStandDistance = 1.1f;
    public const float AlertSeconds = 0.35f;
    public const float StoppingDistance = 0.1f;
}

[Serializable]
public sealed class NpcAgentEntry
{
    public Transform character;
    public Transform[] waypoints;
    public MovementStyle patrolStyle;
    public MovementStyle chaseStyle;
    public MovementStyle holdStyle;
    [Min(0f)] public float detectRadius = NpcAgentDefaults.DetectRadius;
    [Min(0f)] public float loseRadius = NpcAgentDefaults.LoseRadius;
    [Min(0f)] public float attackStandDistance = NpcAgentDefaults.AttackStandDistance;
    [Min(0f)] public float alertSeconds = NpcAgentDefaults.AlertSeconds;
    public bool suppressMode;
}

[DefaultExecutionOrder(-20)]
[DisallowMultipleComponent]
public sealed class NpcManager : MonoBehaviour
{
    [SerializeField] List<NpcAgentEntry> _agents = new();

    readonly List<NpcAgentRuntime> _runtimes = new(8);

    void OnEnable() => BindRuntimes();

    void OnDisable()
    {
        for (int i = 0; i < _runtimes.Count; i++)
            _runtimes[i].Release();
        _runtimes.Clear();
    }

    void Update()
    {
        // 할당 없음. 호스트는 CharacterBodyHost 레지스트리, 상태는 행 단위.
        float dt = TimeScaleService.Delta(TimeScaleChannel.World);
        if (dt <= 0f)
            return;

        for (int i = 0; i < _runtimes.Count; i++)
            _runtimes[i].Tick(dt);
    }

    public void Register(NpcAgentEntry entry)
    {
        if (entry == null || entry.character == null)
            return;

        if (_agents == null)
            _agents = new List<NpcAgentEntry>();
        _agents.Add(entry);

        if (!isActiveAndEnabled)
            return;

        var runtime = new NpcAgentRuntime();
        if (!runtime.TryBind(entry))
            return;

        _runtimes.Add(runtime);
    }

    void BindRuntimes()
    {
        for (int i = 0; i < _runtimes.Count; i++)
            _runtimes[i].Release();
        _runtimes.Clear();

        if (_agents == null)
            return;

        for (int i = 0; i < _agents.Count; i++)
        {
            NpcAgentEntry entry = _agents[i];
            if (entry == null || entry.character == null)
                continue;

            var runtime = new NpcAgentRuntime();
            if (!runtime.TryBind(entry))
                continue;

            _runtimes.Add(runtime);
        }
    }

    sealed class NpcAgentRuntime
    {
        NpcAgentEntry _entry;
        Transform _transform;
        CharacterMotor _motor;
        CharacterAttacker _attacker;
        CharacterAimIntent _aimIntent;
        CharacterSkillsHost _skillsHost;
        CharacterState _characterState;
        CharacterBodyHost _selfHost;
        ICharacterDefeat _defeat;

        NpcCombatState _state = NpcCombatState.Idle;
        int _patrolIndex;
        float _alertTimer;
        Vector3 _homePosition;
        CharacterBodyHost _target;
        float _distanceToTarget = float.MaxValue;

        public bool TryBind(NpcAgentEntry entry)
        {
            _entry = entry;
            _transform = entry.character;
            GameObject go = _transform.gameObject;

            _motor = go.GetComponent<CharacterMotor>();
            _attacker = go.GetComponent<CharacterAttacker>();
            _aimIntent = go.GetComponent<CharacterAimIntent>();
            _skillsHost = go.GetComponent<CharacterSkillsHost>();
            _characterState = go.GetComponent<CharacterState>();
            _selfHost = go.GetComponent<CharacterBodyHost>();

            if (_motor == null || _attacker == null || _selfHost == null)
            {
                Debug.LogError(
                    $"[NpcManager] '{go.name}' needs CharacterMotor, CharacterAttacker, CharacterBodyHost.",
                    go);
                return false;
            }

            _homePosition = _transform.position;
            ApplyAimPreference();

            _defeat = _skillsHost != null ? _skillsHost.Defeat : null;
            if (_defeat != null)
                _defeat.Changed += OnDefeatChanged;

            if (_defeat != null && _defeat.IsDefeated)
                EnterDead();
            else
                EnterPatrol();

            return true;
        }

        public void Release()
        {
            if (_defeat != null)
                _defeat.Changed -= OnDefeatChanged;
            _defeat = null;
            NpcSteer.Stop(_motor);
            ReleaseCombatAim();
        }

        public void Tick(float dt)
        {
            if (_transform == null || _motor == null)
                return;
            if (!_transform.gameObject.activeInHierarchy)
                return;
            if (_motor.IsPossessed)
                return;
            if (_state == NpcCombatState.Dead)
                return;

            if (_defeat != null && _defeat.IsDefeated)
            {
                EnterDead();
                return;
            }

            RefreshTarget();

            switch (_state)
            {
                case NpcCombatState.Idle:
                case NpcCombatState.Patrol:
                    TickPatrol();
                    break;
                case NpcCombatState.Alert:
                    TickAlert(dt);
                    break;
                case NpcCombatState.Chase:
                    TickChase();
                    break;
                case NpcCombatState.Attack:
                    TickAttack();
                    break;
                case NpcCombatState.Return:
                    TickReturn();
                    break;
            }
        }

        void TickPatrol()
        {
            if (_target != null)
            {
                EnterAlert();
                return;
            }

            Transform[] waypoints = _entry.waypoints;
            if (waypoints == null || waypoints.Length == 0)
            {
                NpcSteer.Stop(_motor);
                _motor.SetActiveMovementStyle(_entry.holdStyle);
                return;
            }

            _motor.SetActiveMovementStyle(_entry.patrolStyle);
            Transform waypoint = waypoints[_patrolIndex];
            if (waypoint == null)
            {
                AdvancePatrolIndex();
                return;
            }

            if (NpcSteer.TryArriveOrSteer(
                    _motor,
                    _transform.position,
                    waypoint.position,
                    ResolveStoppingDistance(_entry.patrolStyle)))
                AdvancePatrolIndex();
        }

        void TickAlert(float dt)
        {
            _alertTimer -= dt;
            if (_target == null)
            {
                EnterReturn();
                return;
            }

            if (_alertTimer > 0f)
                return;

            EnterChase();
        }

        void TickChase()
        {
            if (_target == null)
            {
                EnterReturn();
                return;
            }

            if (IsSelectedActionInRange(_distanceToTarget))
            {
                EnterAttack();
                return;
            }

            _motor.SetActiveMovementStyle(_entry.chaseStyle);
            NpcSteer.TryArriveOrSteer(
                _motor,
                _transform.position,
                _target.transform.position,
                ResolveStoppingDistance(_entry.chaseStyle));
        }

        void TickAttack()
        {
            if (_target == null)
            {
                EnterReturn();
                return;
            }

            if (!IsSelectedActionInRange(_distanceToTarget))
            {
                EnterChase();
                return;
            }

            _motor.SetActiveMovementStyle(_entry.holdStyle);
            if (_distanceToTarget > _entry.attackStandDistance)
            {
                NpcSteer.TryArriveOrSteer(
                    _motor,
                    _transform.position,
                    _target.transform.position,
                    _entry.attackStandDistance);
            }
            else
            {
                NpcSteer.Stop(_motor);
            }

            AimAtTarget(_target);

            WeaponAction action = _attacker.SelectedAction;
            if (action == WeaponAction.Raise)
                return;

            if (_attacker.GetCooldown(_attacker.ActiveWieldHand) > 0f ||
                _attacker.HasPendingFor(_attacker.ActiveWieldHand))
                return;

            AttackPerformResult result = _attacker.TryPerformSelected(_target);
            if (result == AttackPerformResult.OutOfRange)
                EnterChase();
        }

        void TickReturn()
        {
            if (_target != null)
            {
                EnterAlert();
                return;
            }

            _motor.SetActiveMovementStyle(_entry.patrolStyle);
            if (NpcSteer.TryArriveOrSteer(
                    _motor,
                    _transform.position,
                    _homePosition,
                    ResolveStoppingDistance(_entry.patrolStyle)))
                EnterPatrol();
        }

        void RefreshTarget()
        {
            if (_target != null)
            {
                if (!IsUsableTarget(_target))
                {
                    ClearTarget();
                }
                else
                {
                    _distanceToTarget = HorizontalDistance(_target.transform.position);
                    if (_distanceToTarget > _entry.loseRadius)
                        ClearTarget();
                    return;
                }
            }

            CharacterBodyHost best = null;
            float bestDist = float.MaxValue;
            int hostCount = CharacterBodyHost.ActiveCount;
            for (int i = 0; i < hostCount; i++)
            {
                CharacterBodyHost host = CharacterBodyHost.GetActive(i);
                if (!IsUsableTarget(host) || host == _selfHost)
                    continue;
                if (!IsPreferredHostile(host))
                    continue;

                float dist = HorizontalDistance(host.transform.position);
                if (dist > _entry.detectRadius || dist >= bestDist)
                    continue;

                best = host;
                bestDist = dist;
            }

            _target = best;
            _distanceToTarget = best != null ? bestDist : float.MaxValue;
        }

        void ClearTarget()
        {
            _target = null;
            _distanceToTarget = float.MaxValue;
        }

        static bool IsPreferredHostile(CharacterBodyHost host)
        {
            ICharacterBody body = host.Body;
            return body != null && ReferenceEquals(body, GameplayData.Body);
        }

        static bool IsUsableTarget(CharacterBodyHost host)
        {
            if (host == null || !host.isActiveAndEnabled)
                return false;
            ICharacterBody body = host.Body;
            return body != null && !body.IsDeadState;
        }

        float HorizontalDistance(Vector3 world)
        {
            Vector3 offset = world - _transform.position;
            offset.y = 0f;
            return offset.magnitude;
        }

        bool IsSelectedActionInRange(float distance)
        {
            if (_attacker == null)
                return false;
            WeaponAction action = _attacker.SelectedAction;
            if (!_attacker.CanPerform(action))
                return false;
            ItemData item = _attacker.ItemFor(_attacker.ItemId);
            ItemData ammo = WeaponChamber.ResolveAmmo(
                _attacker.WieldedStack,
                _attacker.WieldedInstance);
            return distance <= CombatMath.RangeMeters(item, action, ammo);
        }

        static float ResolveStoppingDistance(MovementStyle style) =>
            style != null ? style.StoppingDistance : NpcAgentDefaults.StoppingDistance;

        void SetAimHeld(bool held) => _aimIntent?.SetAimHeld(held);

        void ReleaseCombatAim()
        {
            SetAimHeld(false);
            _characterState?.ClearAim();
        }

        void AimAtTarget(CharacterBodyHost target)
        {
            if (_characterState == null || target == null)
                return;

            Vector3 toTarget = target.transform.position - _transform.position;
            toTarget.y = 0f;
            float reach = toTarget.magnitude;
            if (reach < CharacterAttacker.MinRayDistance)
                return;

            _characterState.SetAimDir(toTarget / reach, target.transform.position, reach);
        }

        void EnterIdle()
        {
            _state = NpcCombatState.Idle;
            ReleaseCombatAim();
            NpcSteer.Stop(_motor);
            _motor.SetActiveMovementStyle(_entry.holdStyle);
        }

        void EnterPatrol()
        {
            _state = NpcCombatState.Patrol;
            ReleaseCombatAim();
            _motor.SetActiveMovementStyle(_entry.patrolStyle);
            if (_entry.waypoints == null || _entry.waypoints.Length == 0)
                EnterIdle();
        }

        void EnterAlert()
        {
            _state = NpcCombatState.Alert;
            ReleaseCombatAim();
            _alertTimer = _entry.alertSeconds;
            NpcSteer.Stop(_motor);
            _motor.SetActiveMovementStyle(_entry.holdStyle);
        }

        void EnterChase()
        {
            _state = NpcCombatState.Chase;
            ReleaseCombatAim();
            _motor.SetActiveMovementStyle(_entry.chaseStyle);
        }

        void EnterAttack()
        {
            _state = NpcCombatState.Attack;
            SetAimHeld(true);
            AimAtTarget(_target);
            _motor.SetActiveMovementStyle(_entry.holdStyle);
        }

        void EnterReturn()
        {
            _state = NpcCombatState.Return;
            ReleaseCombatAim();
            _motor.SetActiveMovementStyle(_entry.patrolStyle);
            NpcSteer.TryArriveOrSteer(
                _motor,
                _transform.position,
                _homePosition,
                ResolveStoppingDistance(_entry.patrolStyle));
        }

        void EnterDead()
        {
            _state = NpcCombatState.Dead;
            ReleaseCombatAim();
            NpcSteer.Stop(_motor);
            _motor.SetActiveMovementStyle(_entry.holdStyle);
            _motor.SetDesiredWorldDir(Vector3.zero);
        }

        void OnDefeatChanged()
        {
            if (_defeat != null && _defeat.IsDefeated)
                EnterDead();
        }

        void AdvancePatrolIndex()
        {
            Transform[] waypoints = _entry.waypoints;
            if (waypoints == null || waypoints.Length == 0)
                return;
            _patrolIndex = (_patrolIndex + 1) % waypoints.Length;
        }

        void ApplyAimPreference()
        {
            if (_aimIntent == null)
                return;

            _aimIntent.SetPreferredPart(
                _entry.suppressMode ? BodyPartIds.LegL : BodyPartIds.Torso);
        }
    }
}
