// ============================================================
// DualWieldAttackDriver — Primary 손 완료 후 보조손 순차 + OffHand 배율
// ============================================================

using Garunnir.Runtime.Gameplay.Data;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterAttacker))]
public sealed class DualWieldAttackDriver : MonoBehaviour
{
    CharacterAttacker _attacker;
    CharacterSkillsHost _skillsHost;
    bool _awaitingSecondary;
    CharacterBodyHost _pendingTarget;
    string _secondaryItemId;
    WeaponAction _secondaryAction;
    float _secondaryFactor;
    int _savedLoadedRounds;
    string _savedPrimaryItemId;
    WeaponAction _savedPrimaryAction;

    void Awake()
    {
        _attacker = GetComponent<CharacterAttacker>();
        TryGetComponent(out _skillsHost);
    }

    void OnEnable()
    {
        if (_attacker != null)
            _attacker.AttackResolved += OnAttackResolved;
    }

    void OnDisable()
    {
        if (_attacker != null)
            _attacker.AttackResolved -= OnAttackResolved;
        ClearPending();
    }

    /// <summary>듀얼이면 Primary→Secondary 순차 시작. 아니면 false(호출측 단발).</summary>
    public bool TryPerformDual(CharacterBodyHost target)
    {
        PlayerGearHost gearHost = PlayerGearHost.Active;
        CharacterGearService gear = gearHost != null ? gearHost.Service : null;
        if (gear == null || _attacker == null || target == null)
            return false;

        ICharacterSkills skills = _skillsHost != null ? _skillsHost.Skills : null;
        if (!PrimaryWieldResolver.TryResolvePrimary(
                gear.Wield,
                gear.HandActions,
                skills,
                _attacker.LoadedRounds,
                out PrimaryWieldResolver.HandScore primary,
                out PrimaryWieldResolver.HandScore secondary))
            return false;

        if (primary.Action == null || primary.Stack?.Item == null)
            return false;

        ClearPending();
        _savedLoadedRounds = _attacker.LoadedRounds;
        _savedPrimaryItemId = primary.Stack.ItemId;
        _savedPrimaryAction = primary.Action.Value;

        _attacker.SetWieldedItem(primary.Stack.ItemId, _savedLoadedRounds);
        _attacker.TrySelectAction(primary.Action.Value);
        AttackPerformResult result = _attacker.TryPerform(primary.Action.Value, target, 1f);

        bool hasSecondary = !gear.Wield.IsTwoHand
            && secondary.Stack?.Item != null
            && secondary.Action != null
            && secondary.IsOffHand;

        if (!hasSecondary)
            return true;

        if (result == AttackPerformResult.Cooling
            || result == AttackPerformResult.Unsupported
            || result == AttackPerformResult.NoAmmo
            || result == AttackPerformResult.OutOfRange
            || result == AttackPerformResult.NoTarget)
            return true;

        WieldHand offHand = secondary.Slot == WieldSlotId.Left
            ? WieldHand.Left
            : WieldHand.Right;
        _secondaryItemId = secondary.Stack.ItemId;
        _secondaryAction = secondary.Action.Value;
        _secondaryFactor = PrimaryWieldResolver.OffHandFactor(skills, offHand);
        _pendingTarget = target;
        _awaitingSecondary = true;
        return true;
    }

    void OnAttackResolved(AttackOutcome outcome)
    {
        if (!_awaitingSecondary || _pendingTarget == null || _attacker == null)
            return;

        _awaitingSecondary = false;
        CharacterBodyHost target = _pendingTarget;
        string secondaryId = _secondaryItemId;
        WeaponAction secondaryAction = _secondaryAction;
        float factor = _secondaryFactor;
        int rounds = _attacker.LoadedRounds;
        ClearPending();

        if (target == null || target.Body == null || target.Body.IsDeadState)
        {
            RestorePrimary(rounds);
            return;
        }

        _attacker.SetWieldedItem(secondaryId, rounds);
        _attacker.TrySelectAction(secondaryAction);
        _attacker.TryPerform(secondaryAction, target, factor);
        RestorePrimary(_attacker.LoadedRounds);
    }

    void RestorePrimary(int loadedRounds)
    {
        if (_attacker == null)
            return;
        _attacker.SetWieldedItem(_savedPrimaryItemId ?? string.Empty, loadedRounds);
        _attacker.TrySelectAction(_savedPrimaryAction);
        PlayerGearHost.Active?.RefreshPrimaryWield();
    }

    void ClearPending()
    {
        _awaitingSecondary = false;
        _pendingTarget = null;
        _secondaryItemId = null;
        _secondaryFactor = 1f;
    }
}
