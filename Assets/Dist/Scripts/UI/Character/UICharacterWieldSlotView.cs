// ============================================================
// UICharacterWieldSlotView — L/R 들기 슬롯 (아이콘·액션·호버·해제 · 이름 겹침 바)
// ============================================================

using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class UICharacterWieldSlotView :
    MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerClickHandler
{
    TMP_Text _label;
    ItemNameStatusBar _nameBar;
    CharacterGearService _gear;
    WieldSlotId _slot;
    int _strength;
    Action<string> _onHover;
    Action _onExit;
    Action<WieldSlotId, bool> _onUnequip;

    void EnsureLabel()
    {
        if (_label != null)
            return;

        Transform labelChild = transform.Find(ItemNameStatusBar.LabelObjectName);
        if (labelChild != null)
            _label = labelChild.GetComponent<TMP_Text>();
        if (_label == null)
            _label = GetComponent<TMP_Text>();
        if (_label == null)
            _label = gameObject.AddComponent<TextMeshProUGUI>();
        _label.fontSize = 15f;
        _label.raycastTarget = false;
    }

    void EnsureNameBar()
    {
        EnsureLabel();
        if (_nameBar != null)
            return;
        _nameBar = ItemNameStatusBar.Ensure(ref _label);
    }

    public void Bind(
        CharacterGearService gear,
        WieldSlotId slot,
        int strength,
        Action<string> onHover,
        Action onExit,
        Action<WieldSlotId, bool> onUnequip)
    {
        EnsureNameBar();
        _gear = gear;
        _slot = slot;
        _strength = strength;
        _onHover = onHover;
        _onExit = onExit;
        _onUnequip = onUnequip;

        ItemStack stack = gear?.Wield?.Get(slot);
        string slotName = slot == WieldSlotId.Left
            ? CharacterGearLabels.SlotLeft
            : CharacterGearLabels.SlotRight;

        if (stack?.Item == null)
        {
            _label.text = $"{slotName}: —";
            _nameBar?.Clear();
            return;
        }

        WeaponAction? action = null;
        gear.HandActions.TryGet(stack.ItemId, out action);
        string actionLabel = FormatAction(action);
        _label.text = $"{slotName}: {stack.Item.name} [{actionLabel}]";
        RefreshNameBar();
    }

    public void RefreshNameBar()
    {
        if (_nameBar == null)
            return;

        ItemStack stack = _gear?.Wield?.Get(_slot);
        ItemTimedNameProgress.Apply(_nameBar, stack);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        ItemStack stack = _gear?.Wield?.Get(_slot);
        if (stack?.Item == null)
            return;

        bool twoHand = _gear.Wield.IsTwoHand;
        int required = GearHandleRules.RequiredStr(stack.Item, twoHand);
        bool strain = GearHandleRules.HasLiftStrain(_strength, stack.Item, twoHand);
        var sb = new StringBuilder(128);
        sb.Append(stack.Item.name).Append('\n');
        sb.Append(CharacterGearLabels.FormatRequiredStr(required, _strength, strain));
        _onHover?.Invoke(sb.ToString());
    }

    public void OnPointerExit(PointerEventData eventData) => _onExit?.Invoke();

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_gear == null)
            return;

        ItemStack stack = _gear.Wield.Get(_slot);
        if (stack?.Item == null)
            return;

        if (eventData.button == PointerEventData.InputButton.Right)
        {
            CycleHandAction(stack.ItemId);
            return;
        }

        if (eventData.button == PointerEventData.InputButton.Left && eventData.clickCount >= 2)
            _onUnequip?.Invoke(_slot, false);
    }

    void CycleHandAction(string itemId)
    {
        if (!_gear.HandActions.TryGet(itemId, out WeaponAction? current))
            current = null;

        WeaponAction? next;
        if (current == null)
            next = WeaponAction.Bashing;
        else if (current == WeaponAction.Bashing)
            next = WeaponAction.Cutting;
        else if (current == WeaponAction.Cutting)
            next = WeaponAction.Gun;
        else
            next = null;

        _gear.TrySetHandAction(itemId, next);
    }

    static string FormatAction(WeaponAction? action)
    {
        if (action == null)
            return CharacterGearLabels.ActionNone;
        switch (action.Value)
        {
            case WeaponAction.Bashing: return CharacterGearLabels.ActionBash;
            case WeaponAction.Cutting: return CharacterGearLabels.ActionCut;
            case WeaponAction.Gun: return CharacterGearLabels.ActionGun;
            default: return CharacterGearLabels.ActionNone;
        }
    }
}
