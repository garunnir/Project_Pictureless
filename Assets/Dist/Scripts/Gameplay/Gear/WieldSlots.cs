// ============================================================
// WieldSlots — L/R 들기 슬롯 (양손=동일 스택 참조, UI 별도 칸 없음)
// ============================================================

using System;

public sealed class WieldSlots
{
    ItemStack _left;
    ItemStack _right;
    bool _twoHand;

    public event Action Changed;

    public ItemStack Left => _left;
    public ItemStack Right => _right;
    public bool IsTwoHand => _twoHand;

    public ItemStack Get(WieldSlotId slot) =>
        slot == WieldSlotId.Left ? _left : _right;

    public bool IsOccupied(WieldSlotId slot) => Get(slot) != null;

    public bool Contains(ItemStack stack)
    {
        if (stack == null)
            return false;
        return _left == stack || _right == stack;
    }

    public bool TryWield(ItemStack stack, WieldHand hand, out ItemStack displacedLeft, out ItemStack displacedRight)
    {
        displacedLeft = null;
        displacedRight = null;
        if (stack?.Item == null)
            return false;

        if (hand == WieldHand.TwoHand || GearHandleRules.IsTwoHandWeapon(stack.Item))
        {
            displacedLeft = ClearSlotInternal(WieldSlotId.Left);
            displacedRight = ClearSlotInternal(WieldSlotId.Right);
            if (displacedLeft == stack)
                displacedLeft = null;
            if (displacedRight == stack)
                displacedRight = null;
            if (displacedLeft != null && displacedLeft == displacedRight)
                displacedRight = null;

            _left = stack;
            _right = stack;
            _twoHand = true;
            Changed?.Invoke();
            return true;
        }

        WieldSlotId slot = hand == WieldHand.Left ? WieldSlotId.Left : WieldSlotId.Right;
        if (_twoHand)
        {
            ItemStack previous = _left;
            _left = null;
            _right = null;
            _twoHand = false;
            if (previous != null && previous != stack)
            {
                if (slot == WieldSlotId.Left)
                    displacedLeft = previous;
                else
                    displacedRight = previous;
            }
        }

        ItemStack displaced = ClearSlotInternal(slot);
        if (displaced != null && displaced != stack)
        {
            if (slot == WieldSlotId.Left)
                displacedLeft = displaced;
            else
                displacedRight = displaced;
        }

        if (slot == WieldSlotId.Left)
            _left = stack;
        else
            _right = stack;

        // 이미 든 스택을 반대 한손으로 옮기면 출발 칸을 비운다 (양손 모드가 아님).
        if (slot == WieldSlotId.Left)
        {
            if (_right == stack)
                _right = null;
        }
        else if (_left == stack)
        {
            _left = null;
        }

        Changed?.Invoke();
        return true;
    }

    public bool TryGetGrip(ItemStack stack, out WieldHand hand)
    {
        hand = WieldHand.Left;
        if (stack == null || !Contains(stack))
            return false;

        if (_twoHand)
        {
            hand = WieldHand.TwoHand;
            return true;
        }

        if (_left == stack)
        {
            hand = WieldHand.Left;
            return true;
        }

        hand = WieldHand.Right;
        return true;
    }

    public static WieldHand OppositeHand(WieldSlotId slot) =>
        slot == WieldSlotId.Left ? WieldHand.Right : WieldHand.Left;

    public bool TryUnwield(ItemStack stack, out ItemStack removed)
    {
        removed = null;
        if (stack == null || !Contains(stack))
            return false;

        if (_twoHand && (_left == stack || _right == stack))
        {
            removed = _left ?? _right;
            _left = null;
            _right = null;
            _twoHand = false;
            Changed?.Invoke();
            return removed != null;
        }

        if (_left == stack)
        {
            removed = _left;
            _left = null;
            Changed?.Invoke();
            return true;
        }

        if (_right == stack)
        {
            removed = _right;
            _right = null;
            Changed?.Invoke();
            return true;
        }

        return false;
    }

    public bool TryUnwieldSlot(WieldSlotId slot, out ItemStack removed)
    {
        removed = null;
        if (_twoHand)
            return TryUnwield(_left ?? _right, out removed);

        removed = ClearSlotInternal(slot);
        if (removed == null)
            return false;
        Changed?.Invoke();
        return true;
    }

    public void Snapshot(out ItemStack left, out ItemStack right, out bool twoHand)
    {
        left = _left;
        right = _right;
        twoHand = _twoHand;
    }

    public void Restore(ItemStack left, ItemStack right, bool twoHand)
    {
        _left = left;
        _right = right;
        _twoHand = twoHand;
        Changed?.Invoke();
    }

    public void ClearKeepingStacks(out ItemStack left, out ItemStack right, out bool twoHand)
    {
        Snapshot(out left, out right, out twoHand);
        _left = null;
        _right = null;
        _twoHand = false;
        Changed?.Invoke();
    }

    ItemStack ClearSlotInternal(WieldSlotId slot)
    {
        if (slot == WieldSlotId.Left)
        {
            ItemStack prev = _left;
            _left = null;
            return prev;
        }

        ItemStack prevR = _right;
        _right = null;
        return prevR;
    }
}
