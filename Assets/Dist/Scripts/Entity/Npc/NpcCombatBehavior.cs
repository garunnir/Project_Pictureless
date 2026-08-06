// ============================================================
// NpcCombatBehavior — 전투 NPC FSM (Patrol/Chase/Attack/Dead)
// ============================================================

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

[DisallowMultipleComponent]
[RequireComponent(typeof(NpcMovement))]
[RequireComponent(typeof(NpcSteerToPoint))]
[RequireComponent(typeof(NpcSenses))]
[RequireComponent(typeof(CharacterAttacker))]
[RequireComponent(typeof(CharacterAimIntent))]
[RequireComponent(typeof(CharacterSkillsHost))]
public sealed class NpcCombatBehavior : MonoBehaviour
{
    [SerializeField] Transform[] _patrolWaypoints;
    [SerializeField] MovementStyle _patrolStyle;
    [SerializeField] MovementStyle _chaseStyle;
    [SerializeField] MovementStyle _holdStyle;
    [SerializeField, Min(0f)] float _attackStandDistance = 1.1f;
    [SerializeField, Min(0f)] float _alertSeconds = 0.35f;
    [SerializeField] bool _suppressMode;
    [SerializeField] TimeScaleChannel _timeChannel = TimeScaleChannel.World;

    NpcMovement _movement;
    NpcSteerToPoint _steer;
    NpcSenses _senses;
    CharacterAttacker _attacker;
    CharacterAimIntent _aimIntent;
    CharacterSkillsHost _skillsHost;
    ICharacterDefeat _defeat;

    NpcCombatState _state = NpcCombatState.Idle;
    int _patrolIndex;
    float _alertTimer;
    Vector3 _homePosition;
    WeaponAction _pendingAction = WeaponAction.Bashing;

    public NpcCombatState State => _state;
    public bool SuppressMode
    {
        get => _suppressMode;
        set
        {
            _suppressMode = value;
            ApplyAimPreference();
        }
    }

    void Awake()
    {
        _movement = GetComponent<NpcMovement>();
        _steer = GetComponent<NpcSteerToPoint>();
        _senses = GetComponent<NpcSenses>();
        _attacker = GetComponent<CharacterAttacker>();
        _aimIntent = GetComponent<CharacterAimIntent>();
        _skillsHost = GetComponent<CharacterSkillsHost>();
        _homePosition = transform.position;
        ApplyAimPreference();
    }

    void OnEnable()
    {
        _defeat = _skillsHost != null ? _skillsHost.Defeat : null;
        if (_defeat != null)
            _defeat.Changed += OnDefeatChanged;
        if (_defeat != null && _defeat.IsDefeated)
            EnterDead();
        else
            EnterPatrol();
    }

    void OnDisable()
    {
        if (_defeat != null)
            _defeat.Changed -= OnDefeatChanged;
        _defeat = null;
        _steer?.ClearDestination();
    }

    void Update()
    {
        float dt = TimeScaleService.Delta(_timeChannel);
        if (dt <= 0f || _state == NpcCombatState.Dead)
            return;

        if (_defeat != null && _defeat.IsDefeated)
        {
            EnterDead();
            return;
        }

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
        if (_senses.HasTarget)
        {
            EnterAlert();
            return;
        }

        if (_patrolWaypoints == null || _patrolWaypoints.Length == 0)
        {
            _steer.ClearDestination();
            _movement.SetActiveMovementStyle(_holdStyle);
            return;
        }

        _movement.SetActiveMovementStyle(_patrolStyle);
        Transform waypoint = _patrolWaypoints[_patrolIndex];
        if (waypoint == null)
        {
            AdvancePatrolIndex();
            return;
        }

        _steer.SetDestination(waypoint.position);
        if (_steer.IsArrived)
            AdvancePatrolIndex();
    }

    void TickAlert(float dt)
    {
        _alertTimer -= dt;
        if (!_senses.HasTarget)
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
        if (!_senses.HasTarget)
        {
            EnterReturn();
            return;
        }

        CharacterBodyHost target = _senses.Target;
        float distance = _senses.DistanceToTarget;
        if (_attacker.TryGetBestAction(distance, out WeaponAction action))
        {
            _pendingAction = action;
            EnterAttack();
            return;
        }

        _movement.SetActiveMovementStyle(_chaseStyle);
        _steer.SetTarget(target.transform);
    }

    void TickAttack()
    {
        if (!_senses.HasTarget)
        {
            EnterReturn();
            return;
        }

        CharacterBodyHost target = _senses.Target;
        float distance = _senses.DistanceToTarget;
        if (!_attacker.TryGetBestAction(distance, out WeaponAction action))
        {
            EnterChase();
            return;
        }

        _pendingAction = action;
        _movement.SetActiveMovementStyle(_holdStyle);
        if (distance > _attackStandDistance)
            _steer.SetTarget(target.transform);
        else
            _steer.ClearDestination();

        AttackPerformResult result = _attacker.TryPerform(_pendingAction, target);
        if (result == AttackPerformResult.OutOfRange)
            EnterChase();
    }

    void TickReturn()
    {
        if (_senses.HasTarget)
        {
            EnterAlert();
            return;
        }

        _movement.SetActiveMovementStyle(_patrolStyle);
        _steer.SetDestination(_homePosition);
        if (_steer.IsArrived)
            EnterPatrol();
    }

    void EnterIdle()
    {
        _state = NpcCombatState.Idle;
        _steer.ClearDestination();
        _movement.SetActiveMovementStyle(_holdStyle);
    }

    void EnterPatrol()
    {
        _state = NpcCombatState.Patrol;
        _movement.SetActiveMovementStyle(_patrolStyle);
        if (_patrolWaypoints == null || _patrolWaypoints.Length == 0)
            EnterIdle();
    }

    void EnterAlert()
    {
        _state = NpcCombatState.Alert;
        _alertTimer = _alertSeconds;
        _steer.ClearDestination();
        _movement.SetActiveMovementStyle(_holdStyle);
    }

    void EnterChase()
    {
        _state = NpcCombatState.Chase;
        _movement.SetActiveMovementStyle(_chaseStyle);
    }

    void EnterAttack()
    {
        _state = NpcCombatState.Attack;
        _movement.SetActiveMovementStyle(_holdStyle);
    }

    void EnterReturn()
    {
        _state = NpcCombatState.Return;
        _movement.SetActiveMovementStyle(_patrolStyle);
        _steer.SetDestination(_homePosition);
    }

    void EnterDead()
    {
        _state = NpcCombatState.Dead;
        _steer.ClearDestination();
        _movement.SetActiveMovementStyle(_holdStyle);
        _movement.SetDesiredWorldDir(Vector3.zero);
        enabled = false;
    }

    void OnDefeatChanged()
    {
        if (_defeat != null && _defeat.IsDefeated)
            EnterDead();
    }

    void AdvancePatrolIndex()
    {
        if (_patrolWaypoints == null || _patrolWaypoints.Length == 0)
            return;
        _patrolIndex = (_patrolIndex + 1) % _patrolWaypoints.Length;
    }

    void ApplyAimPreference()
    {
        if (_aimIntent == null)
            return;

        _aimIntent.SetPreferredPart(
            _suppressMode ? BodyPartIds.LegL : BodyPartIds.Torso);
    }
}
