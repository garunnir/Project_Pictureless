// ============================================================
// DualWieldAttackDriver — TwoHand 1회 / 듀얼 Primary→Offhand 교대 (손별 Action)
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
    PrimaryWieldResolver.HandScore _secondary;
    float _secondaryFactor;
    int _savedLoadedRounds;
    string _savedPrimaryItemId;
    WeaponAction _savedPrimaryAction;
    WieldHand _savedPrimaryHand;

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

    /// <summary>
    /// TwoHand·한손·듀얼 시전. 듀얼이면 Primary Resolve 후 Offhand 교대.
    /// 손별 HandAction이 달라도 스텝마다 Action/Hand를 독립 적용.
    /// false면 호출측 단발(<see cref="CharacterAttacker.TryPerformSelected"/>).
    /// </summary>
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
        _savedPrimaryHand = CharacterAttacker.AnimHandFrom(gear.Wield, primary.Slot);

        ApplyStep(primary, _savedPrimaryHand, _savedLoadedRounds);
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
        _secondary = secondary;
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
        PrimaryWieldResolver.HandScore secondary = _secondary;
        float factor = _secondaryFactor;
        int rounds = _attacker.LoadedRounds;
        ClearPending();

        if (target == null
            || target.Body == null
            || target.Body.IsDeadState
            || secondary.Action == null
            || secondary.Stack?.Item == null)
        {
            RestorePrimary(rounds);
            return;
        }

        WieldHand hand = secondary.Slot == WieldSlotId.Left
            ? WieldHand.Left
            : WieldHand.Right;
        ApplyStep(secondary, hand, rounds);
        _attacker.TryPerform(secondary.Action.Value, target, factor);
        // LocAnim은 AttackResolved outcome sticky로 Hand/Action/Attack를 소비하므로
        // 같은 스택에서 primary 복귀해도 시전 틱 애니는 유지된다.
        RestorePrimary(_attacker.LoadedRounds);
    }

    void ApplyStep(PrimaryWieldResolver.HandScore score, WieldHand hand, int loadedRounds)
    {
        if (_attacker == null || score.Stack == null || score.Action == null)
            return;

        _attacker.SetActiveWieldHand(hand);
        _attacker.SetWieldedItem(score.Stack.ItemId, loadedRounds);
        _attacker.TrySelectAction(score.Action.Value);
    }

    void RestorePrimary(int loadedRounds)
    {
        if (_attacker == null)
            return;
        _attacker.SetActiveWieldHand(_savedPrimaryHand);
        _attacker.SetWieldedItem(_savedPrimaryItemId ?? string.Empty, loadedRounds);
        _attacker.TrySelectAction(_savedPrimaryAction);
        PlayerGearHost.Active?.RefreshPrimaryWield();
    }

    void ClearPending()
    {
        _awaitingSecondary = false;
        _pendingTarget = null;
        _secondaryFactor = 1f;
        _secondary = default;
    }
}
