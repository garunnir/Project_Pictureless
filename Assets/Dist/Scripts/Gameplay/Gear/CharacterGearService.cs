// ============================================================
// CharacterGearService — Wear/Wield 오케스트레이션 + 시간바 + Primary 갱신
// ============================================================

using System;
using System.Collections.Generic;
using Garunnir.Runtime.Gameplay.Data;
using UnityEngine;

public sealed class CharacterGearService
{
    readonly EquipmentWearState _wear = new();
    readonly WieldSlots _wield = new();
    readonly HandActionBinding _handActions = new();
    readonly GearTimedAction _timed = new();
    WeaponPresentationCatalog _presentationCatalog;
    readonly ToolUseWieldSession _toolSession = new();
    readonly List<ItemStack> _filterScratch = new(16);

    Func<int> _strengthProvider;
    Func<ICharacterSkills> _skillsProvider;
    Action _onPrimaryDirty;
    Func<InventoryContainer> _bodyProvider;
    Func<InventoryContainer> _floorProvider;
    ItemStack _activeStack;

    public EquipmentWearState Wear => _wear;
    public WieldSlots Wield => _wield;
    public HandActionBinding HandActions => _handActions;
    public WeaponPresentationCatalog PresentationCatalog => _presentationCatalog;
    public GearTimedAction Timed => _timed;
    public ToolUseWieldSession ToolSession => _toolSession;
    public bool IsBusy => _timed.IsRunning;
    public ItemStack ActiveStack => _activeStack;
    public bool HasLiftStrain { get; private set; }

    public event Action Changed;
    public event Action LiftStrainChanged;

    public void Bind(
        Func<int> strengthProvider,
        Func<ICharacterSkills> skillsProvider,
        Func<InventoryContainer> bodyProvider,
        Func<InventoryContainer> floorProvider,
        Action onPrimaryDirty)
    {
        _strengthProvider = strengthProvider;
        _skillsProvider = skillsProvider;
        _bodyProvider = bodyProvider;
        _floorProvider = floorProvider;
        _onPrimaryDirty = onPrimaryDirty;

        _wear.Changed += OnDomainChanged;
        _wield.Changed += OnDomainChanged;
        _handActions.Changed += OnDomainChanged;
        _timed.Changed += OnTimedChanged;
        _timed.Completed += ClearActiveStack;
        _timed.Cancelled += ClearActiveStack;
        _toolSession.Changed += OnDomainChanged;
    }

    public void SetPresentationCatalog(WeaponPresentationCatalog catalog)
    {
        _presentationCatalog = catalog;
    }

    public void Unbind()
    {
        _wear.Changed -= OnDomainChanged;
        _wield.Changed -= OnDomainChanged;
        _handActions.Changed -= OnDomainChanged;
        _timed.Changed -= OnTimedChanged;
        _timed.Completed -= ClearActiveStack;
        _timed.Cancelled -= ClearActiveStack;
        _toolSession.Changed -= OnDomainChanged;
        ClearActiveStack();
    }

    public bool IsStackActive(ItemStack stack) =>
        stack != null && ReferenceEquals(_activeStack, stack);

    public void Tick(float deltaSeconds)
    {
        _timed.Tick(deltaSeconds);
        RefreshLiftStrain();
    }

    public IReadOnlyList<ItemStack> GetWornFiltered(string coverPartId)
    {
        _wear.CollectFiltered(coverPartId, _filterScratch);
        return _filterScratch;
    }

    public string GetWearBlockedReason(ItemStack stack)
    {
        if (stack?.Item == null)
            return CharacterGearLabels.BlockedInvalid;
        if (_toolSession.IsActive)
            return CharacterGearLabels.BlockedToolSession;
        if (IsBusy)
            return CharacterGearLabels.BlockedBusy;
        if (!GearHandleRules.IsWearable(stack.Item))
            return CharacterGearLabels.BlockedNotWearable;
        if (_wear.Contains(stack) || _wield.Contains(stack))
            return CharacterGearLabels.BlockedAlreadyEquipped;

        string overlap = WearOverlapRules.GetBlockedReason(_wear, stack.Item);
        if (overlap != null)
            return overlap;

        int str = Strength();
        if (!GearHandleRules.CanLift(str, stack.Item, twoHand: false))
            return CharacterGearLabels.FormatNeedStrength(
                GearHandleRules.RequiredStrForWear(stack.Item), str);

        return null;
    }

    public string GetWieldBlockedReason(ItemStack stack, WieldHand hand)
    {
        if (stack?.Item == null)
            return CharacterGearLabels.BlockedInvalid;
        if (_toolSession.IsActive)
            return CharacterGearLabels.BlockedToolSession;
        if (IsBusy)
            return CharacterGearLabels.BlockedBusy;
        if (_wear.Contains(stack) || _wield.Contains(stack))
            return CharacterGearLabels.BlockedAlreadyEquipped;

        bool twoHand = hand == WieldHand.TwoHand || GearHandleRules.IsTwoHandWeapon(stack.Item);
        int str = Strength();
        if (!GearHandleRules.CanLift(str, stack.Item, twoHand))
            return CharacterGearLabels.FormatNeedStrength(
                GearHandleRules.RequiredStr(stack.Item, twoHand), str);

        return null;
    }

    public bool TryBeginWear(ItemStack stack, InventoryContainer source)
    {
        string reason = GetWearBlockedReason(stack);
        if (reason != null || source == null || !source.ContainsStackReference(stack))
            return false;

        float gear = GearActionDuration.WearSeconds(stack.Item);
        float draw = InventoryTransferDuration.SecondsForStackFrom(source, stack);
        float duration = GearActionDuration.CombinedSeconds(gear, draw);

        ItemStack captured = stack;
        InventoryContainer capturedSource = source;
        return BeginTimed(captured, GearTimedAction.Kind.Wear, duration, () =>
        {
            if (!capturedSource.ContainsStackReference(captured))
                return;
            if (!capturedSource.TryRemoveStackReference(captured))
                return;
            if (!_wear.TryAdd(captured))
            {
                capturedSource.TryAddStackReference(captured);
                return;
            }

            captured.TryEnsureNested(new FixedContainerCapacityPolicy());
            NotifyPrimaryDirty();
        });
    }

    public bool TryBeginTakeOff(ItemStack stack, bool toFloor)
    {
        if (_toolSession.IsActive || IsBusy || stack == null || !_wear.Contains(stack))
            return false;

        float duration = GearActionDuration.TakeOffSeconds(stack.Item);
        ItemStack captured = stack;
        return BeginTimed(captured, GearTimedAction.Kind.TakeOff, duration, () =>
        {
            if (!_wear.TryRemove(captured))
                return;
            DepositStack(captured, toFloor);
            NotifyPrimaryDirty();
        });
    }

    public bool TryBeginWield(ItemStack stack, InventoryContainer source, WieldHand hand)
    {
        string reason = GetWieldBlockedReason(stack, hand);
        if (reason != null || source == null || !source.ContainsStackReference(stack))
            return false;

        float gear = GearActionDuration.WieldSeconds(stack.Item);
        float draw = InventoryTransferDuration.SecondsForStackFrom(source, stack);
        float duration = GearActionDuration.CombinedSeconds(gear, draw);

        ItemStack captured = stack;
        InventoryContainer capturedSource = source;
        WieldHand capturedHand = hand;
        return BeginTimed(captured, GearTimedAction.Kind.Wield, duration, () =>
        {
            if (!capturedSource.ContainsStackReference(captured))
                return;
            if (!capturedSource.TryRemoveStackReference(captured))
                return;

            if (!_wield.TryWield(captured, capturedHand, out ItemStack dL, out ItemStack dR))
            {
                capturedSource.TryAddStackReference(captured);
                return;
            }

            DepositDisplaced(dL);
            DepositDisplaced(dR);
            NotifyPrimaryDirty();
        });
    }

    public bool TryBeginUnwield(ItemStack stack, bool toFloor)
    {
        if (_toolSession.IsActive || IsBusy || stack == null || !_wield.Contains(stack))
            return false;

        float duration = GearActionDuration.UnwieldSeconds(stack.Item);
        ItemStack captured = stack;
        return BeginTimed(captured, GearTimedAction.Kind.Unwield, duration, () =>
        {
            if (!_wield.TryUnwield(captured, out ItemStack removed) || removed == null)
                return;
            DepositStack(removed, toFloor);
            NotifyPrimaryDirty();
        });
    }

    public bool TryBeginUnwieldSlot(WieldSlotId slot, bool toFloor)
    {
        ItemStack stack = _wield.Get(slot);
        return TryBeginUnwield(stack, toFloor);
    }

    /// <summary>메거진 보급 1발 → 약실. Action row 아님.</summary>
    public bool TryReload(ItemStack weapon)
    {
        if (weapon?.Instance == null)
            return false;
        if (!WeaponChamber.TryReload(weapon.Instance, weapon, weapon.Item))
            return false;
        Changed?.Invoke();
        return true;
    }

    public bool TrySetHandAction(ItemStack stack, WeaponAction? action)
    {
        if (stack?.Instance == null)
            return false;
        stack.Instance.SelectedAction = action;
        NotifyPrimaryDirty();
        return true;
    }

    public bool TryBeginToolUse(ItemStack tool, WieldHand hand)
    {
        if (_toolSession.IsActive || IsBusy)
            return false;
        if (tool?.Item == null)
            return false;
        if (_wear.Contains(tool))
            return false;

        bool twoHand = hand == WieldHand.TwoHand || GearHandleRules.IsTwoHandWeapon(tool.Item);
        if (!GearHandleRules.CanLift(Strength(), tool.Item, twoHand))
            return false;

        bool ok = _toolSession.TryBegin(_wield, tool, hand, (t, h) =>
        {
            if (!_wield.TryWield(t, h, out ItemStack dL, out ItemStack dR))
                return false;
            DepositDisplaced(dL);
            DepositDisplaced(dR);
            return true;
        });

        if (ok)
            NotifyPrimaryDirty();
        return ok;
    }

    public bool TryEndToolUse()
    {
        if (!_toolSession.IsActive)
            return false;

        bool ok = _toolSession.TryEnd(_wield, tool =>
        {
            if (_wield.Contains(tool))
                _wield.TryUnwield(tool, out _);
        });
        if (ok)
            NotifyPrimaryDirty();
        return ok;
    }

    public void RefreshLiftStrain()
    {
        bool next = false;
        int str = Strength();
        if (_wield.IsTwoHand)
        {
            ItemStack stack = _wield.Left ?? _wield.Right;
            if (stack?.Item != null)
                next = GearHandleRules.HasLiftStrain(str, stack.Item, twoHand: true);
        }
        else
        {
            if (_wield.Left?.Item != null
                && GearHandleRules.HasLiftStrain(str, _wield.Left.Item, false))
                next = true;
            if (_wield.Right?.Item != null
                && GearHandleRules.HasLiftStrain(str, _wield.Right.Item, false))
                next = true;
        }

        if (next == HasLiftStrain)
            return;

        HasLiftStrain = next;
        LiftStrainChanged?.Invoke();
        Changed?.Invoke();
    }

    void DepositDisplaced(ItemStack stack)
    {
        if (stack == null)
            return;
        DepositStack(stack, toFloor: false);
    }

    void DepositStack(ItemStack stack, bool toFloor)
    {
        if (stack == null)
            return;

        InventoryContainer target = toFloor ? _floorProvider?.Invoke() : _bodyProvider?.Invoke();
        if (target == null)
            target = _bodyProvider?.Invoke();
        if (target == null)
        {
            Debug.LogWarning("[CharacterGearService] No deposit container for unequipped stack.");
            return;
        }

        if (!target.ContainsStackReference(stack))
            target.TryAddStackReference(stack);
    }

    int Strength() => _strengthProvider?.Invoke() ?? 0;

    ICharacterSkills Skills() => _skillsProvider?.Invoke();

    bool BeginTimed(ItemStack stack, GearTimedAction.Kind kind, float duration, Action onComplete)
    {
        _activeStack = stack;
        if (!_timed.TryBegin(kind, duration, onComplete))
        {
            _activeStack = null;
            return false;
        }

        return true;
    }

    void ClearActiveStack()
    {
        if (_activeStack == null)
            return;
        _activeStack = null;
        Changed?.Invoke();
    }

    void OnTimedChanged() => Changed?.Invoke();

    void NotifyPrimaryDirty()
    {
        RefreshLiftStrain();
        _onPrimaryDirty?.Invoke();
        Changed?.Invoke();
    }

    void OnDomainChanged()
    {
        RefreshLiftStrain();
        Changed?.Invoke();
    }
}
