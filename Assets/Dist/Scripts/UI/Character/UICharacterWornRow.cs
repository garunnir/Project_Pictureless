// ============================================================
// UICharacterWornRow — 착용 목록 행 (호버 · 더블클릭 벗기 · 이름 겹침 바)
// ============================================================

using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class UICharacterWornRow :
    MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerClickHandler
{
    TMP_Text _label;
    ItemNameStatusBar _nameBar;
    ItemStack _stack;
    CharacterGearService _gear;
    int _strength;
    Action<string> _onHover;
    Action _onExit;
    Action<ItemStack, bool> _onUnequip;

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
        _label.fontSize = 14f;
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
        ItemStack stack,
        CharacterGearService gear,
        int strength,
        Action<string> onHover,
        Action onExit,
        Action<ItemStack, bool> onUnequip)
    {
        EnsureNameBar();
        _stack = stack;
        _gear = gear;
        _strength = strength;
        _onHover = onHover;
        _onExit = onExit;
        _onUnequip = onUnequip;

        if (stack?.Item == null)
        {
            if (_label != null)
                _label.text = string.Empty;
            _nameBar?.Clear();
            return;
        }

        string covers = stack.Item.armor?.covers != null
            ? string.Join(",", stack.Item.armor.covers)
            : string.Empty;
        _label.text = string.IsNullOrEmpty(covers)
            ? stack.Item.name
            : $"{stack.Item.name} ({covers})";

        RefreshNameBar();
    }

    public void RefreshNameBar()
    {
        if (_nameBar == null)
            return;

        ItemTimedNameProgress.Apply(_nameBar, _stack);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_stack?.Item == null)
            return;

        int required = GearHandleRules.RequiredStrForWear(_stack.Item);
        bool strain = GearHandleRules.HasLiftStrain(_strength, _stack.Item, false);
        var sb = new StringBuilder(160);
        sb.Append(_stack.Item.name).Append('\n');
        sb.Append(CharacterGearLabels.FormatRequiredStr(required, _strength, strain));
        CharacterGearLabels.AppendItemArmorHover(sb, _stack.Item.armor);

        _onHover?.Invoke(sb.ToString());
    }

    public void OnPointerExit(PointerEventData eventData) => _onExit?.Invoke();

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_stack == null)
            return;

        if (eventData.button == PointerEventData.InputButton.Right)
        {
            _onUnequip?.Invoke(_stack, false);
            return;
        }

        if (eventData.button == PointerEventData.InputButton.Left && eventData.clickCount >= 2)
            _onUnequip?.Invoke(_stack, false);
    }
}
