// ============================================================
// ToolUseWieldSession — 도구 사용 시 임시 들기 → 종료 원복
// ============================================================

using System;

/// <summary>
/// M0: API + 거부 규칙. 실제 도구 액션 소비자는 후속 마일스톤에서 Begin/End 호출.
/// 중첩 Begin 거부. 세션 중 수동 들기/해제 거부.
/// </summary>
public sealed class ToolUseWieldSession
{
    ItemStack _savedLeft;
    ItemStack _savedRight;
    bool _savedTwoHand;
    ItemStack _tool;
    WieldHand _hand;
    bool _active;

    public bool IsActive => _active;
    public ItemStack Tool => _tool;

    public event Action Changed;

    public bool TryBegin(
        WieldSlots slots,
        ItemStack tool,
        WieldHand hand,
        Func<ItemStack, WieldHand, bool> tryWieldImmediate)
    {
        if (_active || slots == null || tool?.Item == null || tryWieldImmediate == null)
            return false;

        slots.ClearKeepingStacks(out _savedLeft, out _savedRight, out _savedTwoHand);
        if (!tryWieldImmediate(tool, hand))
        {
            slots.Restore(_savedLeft, _savedRight, _savedTwoHand);
            ClearSaved();
            return false;
        }

        _tool = tool;
        _hand = hand;
        _active = true;
        Changed?.Invoke();
        return true;
    }

    public bool TryEnd(WieldSlots slots, Action<ItemStack> unwieldToolImmediate)
    {
        if (!_active || slots == null)
            return false;

        ItemStack tool = _tool;
        ItemStack left = _savedLeft;
        ItemStack right = _savedRight;
        bool twoHand = _savedTwoHand;

        unwieldToolImmediate?.Invoke(tool);
        slots.Restore(left, right, twoHand);

        _active = false;
        _tool = null;
        ClearSaved();
        Changed?.Invoke();
        return true;
    }

    void ClearSaved()
    {
        _savedLeft = null;
        _savedRight = null;
        _savedTwoHand = false;
    }
}
