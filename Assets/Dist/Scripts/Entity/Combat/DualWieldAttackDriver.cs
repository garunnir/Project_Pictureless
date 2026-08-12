// ============================================================
// DualWieldAttackDriver — TwoHand 1회 / 듀얼 양손 Action (손별 판정)
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
    ItemStack _savedPrimaryStack;
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
    /// TwoHand·한손·듀얼 시전. 듀얼이면 Primary Resolve 후 Offhand.
    /// Primary Gate(NoAmmo/NoTarget/OutOfRange/Cooling)와 무관하게 양손 Action.
    /// target null(조준점)이어도 양손 시전. Unsupported 손만 스킵.
    /// false면 호출측 단발(<see cref="CharacterAttacker.TryPerformSelected"/>).
    /// </summary>
    public bool TryPerformDual(CharacterBodyHost target)
    {
        PlayerGearHost gearHost = PlayerGearHost.Active;
        CharacterGearService gear = gearHost != null ? gearHost.Service : null;
        if (gear == null || _attacker == null)
            return false;

        ICharacterSkills skills = _skillsHost != null ? _skillsHost.Skills : null;
        if (!PrimaryWieldResolver.TryResolvePrimary(
                gear.Wield,
                _attacker.Catalog,
                skills,
                out PrimaryWieldResolver.HandScore primary,
                out PrimaryWieldResolver.HandScore secondary))
            return false;

        bool hasPrimary = primary.Action != null && primary.Stack?.Item != null;
        bool hasSecondary = !gear.Wield.IsTwoHand
            && secondary.Stack?.Item != null
            && secondary.Action != null
            && secondary.IsOffHand;

        if (!hasPrimary && !hasSecondary)
            return false;

        ClearPending();
        _savedPrimaryStack = primary.Stack;
        _savedPrimaryHand = CharacterAttacker.AnimHandFrom(gear.Wield, primary.Slot);

        if (hasSecondary)
            QueueSecondary(secondary, target, skills);

        if (hasPrimary)
        {
            ApplyStep(primary, _savedPrimaryHand);
            _attacker.TryPerform(primary.Action.Value, target, 1f);
        }

        // Unsupported는 AttackResolved를 안 올림 — 대기 중이면 Offhand를 여기서 시전.
        if (_awaitingSecondary)
            PerformPendingSecondary();

        return true;
    }

    void OnAttackResolved(AttackOutcome outcome)
    {
        if (!_awaitingSecondary || _attacker == null)
            return;

        PerformPendingSecondary();
    }

    void QueueSecondary(
        PrimaryWieldResolver.HandScore secondary,
        CharacterBodyHost target,
        ICharacterSkills skills)
    {
        WieldHand offHand = HandFromSlot(secondary.Slot);
        _secondary = secondary;
        _secondaryFactor = PrimaryWieldResolver.OffHandFactor(skills, offHand);
        _pendingTarget = target;
        _awaitingSecondary = true;
    }

    void PerformPendingSecondary()
    {
        if (!_awaitingSecondary || _attacker == null)
            return;

        CharacterBodyHost target = _pendingTarget;
        PrimaryWieldResolver.HandScore secondary = _secondary;
        float factor = _secondaryFactor;
        ClearPending();

        if (secondary.Action == null || secondary.Stack?.Item == null)
        {
            RestorePrimary();
            return;
        }

        ApplyStep(secondary, HandFromSlot(secondary.Slot));
        _attacker.TryPerform(secondary.Action.Value, target, factor);
        // LocAnim은 AttackResolved outcome sticky로 Hand/Action/Attack를 소비하므로
        // 같은 스택에서 primary 복귀해도 시전 틱 애니는 유지된다.
        RestorePrimary();
    }

    void ApplyStep(PrimaryWieldResolver.HandScore score, WieldHand hand)
    {
        if (_attacker == null || score.Stack == null || score.Action == null)
            return;

        _attacker.SetActiveWieldHand(hand);
        _attacker.SetWieldedItem(score.Stack);
    }

    void RestorePrimary()
    {
        if (_attacker == null)
            return;
        _attacker.SetActiveWieldHand(_savedPrimaryHand);
        _attacker.SetWieldedItem(_savedPrimaryStack);
        PlayerGearHost.Active?.RefreshPrimaryWield();
    }

    void ClearPending()
    {
        _awaitingSecondary = false;
        _pendingTarget = null;
        _secondaryFactor = 1f;
        _secondary = default;
    }

    static WieldHand HandFromSlot(WieldSlotId slot) =>
        slot == WieldSlotId.Left ? WieldHand.Left : WieldHand.Right;
}
