// ============================================================
// UIPlayerStatusBodyTabStrip — HUD 피격도 탭 펼침·선택 스트립
// ============================================================

using System;
using UnityEngine;
using UnityEngine.UI;

public sealed class UIPlayerStatusBodyTabStrip : MonoBehaviour
{
    [Serializable]
    struct TabSlot
    {
        public Button button;
        public Image icon;
        public CharacterWindowTab tab;
    }

    [SerializeField] RectTransform _maskRoot;
    [SerializeField] TabSlot[] _slots;

    Action<CharacterWindowTab> _onTabSelected;
    CharacterWindowTab _selectedTab = CharacterWindowTab.Status;
    Sprite[] _slotSprites;
    bool _expanded;

    public void Initialize(Action<CharacterWindowTab> onTabSelected)
    {
        _onTabSelected = onTabSelected;
        CacheSlotSprites();
        WireButtons();
        SetExpanded(false, force: true);
        SetSelectedTab(_selectedTab);
    }

    void OnDisable() => UnwireButtons();

    public void SetSelectedTab(CharacterWindowTab tab)
    {
        _selectedTab = tab;
        if (_expanded)
            SetExpanded(false);
        else
            UpdateCollapsedIcon();
    }

    void CacheSlotSprites()
    {
        if (_slots == null)
            return;

        _slotSprites = new Sprite[_slots.Length];
        for (int i = 0; i < _slots.Length; i++)
            _slotSprites[i] = _slots[i].icon != null ? _slots[i].icon.sprite : null;
    }

    void WireButtons()
    {
        UnwireButtons();
        if (_slots == null)
            return;

        for (int i = 0; i < _slots.Length; i++)
        {
            int captured = i;
            Button button = _slots[i].button;
            if (button == null)
                continue;

            button.onClick.AddListener(() => OnSlotClicked(captured));
        }
    }

    void UnwireButtons()
    {
        if (_slots == null)
            return;

        for (int i = 0; i < _slots.Length; i++)
        {
            if (_slots[i].button != null)
                _slots[i].button.onClick.RemoveAllListeners();
        }
    }

    void OnSlotClicked(int index)
    {
        if (!_expanded)
        {
            SetExpanded(true);
            return;
        }

        if (_slots == null || index < 0 || index >= _slots.Length)
            return;

        _onTabSelected?.Invoke(_slots[index].tab);
        SetExpanded(false);
    }

    void SetExpanded(bool expanded, bool force = false)
    {
        if (!force && _expanded == expanded)
            return;

        _expanded = expanded;
        ApplySlotVisibility();
        RebuildLayout();
    }

    void ApplySlotVisibility()
    {
        if (_slots == null || _slots.Length == 0)
            return;

        for (int i = 1; i < _slots.Length; i++)
        {
            TabSlot slot = _slots[i];
            GameObject target = slot.button != null ? slot.button.gameObject : slot.icon?.gameObject;
            if (target != null)
                target.SetActive(_expanded);
        }

        if (_expanded)
            RestoreSlotSprites();
        else
            UpdateCollapsedIcon();
    }

    void RestoreSlotSprites()
    {
        if (_slots == null || _slotSprites == null)
            return;

        for (int i = 0; i < _slots.Length && i < _slotSprites.Length; i++)
        {
            Image icon = _slots[i].icon;
            if (icon != null && _slotSprites[i] != null)
                icon.sprite = _slotSprites[i];
        }
    }

    void UpdateCollapsedIcon()
    {
        if (_slots == null || _slots.Length == 0)
            return;

        Image collapsedIcon = _slots[0].icon;
        if (collapsedIcon == null)
            return;

        for (int i = 0; i < _slots.Length; i++)
        {
            if (_slots[i].tab != _selectedTab)
                continue;

            if (_slotSprites != null && i < _slotSprites.Length && _slotSprites[i] != null)
                collapsedIcon.sprite = _slotSprites[i];
            else if (_slots[i].icon != null)
                collapsedIcon.sprite = _slots[i].icon.sprite;
            break;
        }
    }

    void RebuildLayout()
    {
        if (_maskRoot != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(_maskRoot);

        if (transform is RectTransform stripRoot)
            LayoutRebuilder.ForceRebuildLayoutImmediate(stripRoot);
    }
}
