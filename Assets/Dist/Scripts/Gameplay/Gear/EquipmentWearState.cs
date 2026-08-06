// ============================================================
// EquipmentWearState — 착용(Wear) 스택 목록 (들기 슬롯과 분리)
// ============================================================

using System;
using System.Collections.Generic;
using Garunnir.Runtime.Gameplay.Data;

public sealed class EquipmentWearState
{
    readonly List<ItemStack> _worn = new(16);

    public event Action Changed;

    public IReadOnlyList<ItemStack> Worn => _worn;

    public bool Contains(ItemStack stack) => stack != null && _worn.Contains(stack);

    public bool TryAdd(ItemStack stack)
    {
        if (stack?.Item == null || _worn.Contains(stack))
            return false;
        if (!GearHandleRules.IsWearable(stack.Item))
            return false;
        if (WearOverlapRules.HasConflict(this, stack.Item))
            return false;

        _worn.Add(stack);
        Changed?.Invoke();
        return true;
    }

    public bool TryRemove(ItemStack stack)
    {
        if (stack == null || !_worn.Remove(stack))
            return false;

        Changed?.Invoke();
        return true;
    }

    public void CollectFiltered(string coverPartId, List<ItemStack> into)
    {
        if (into == null)
            return;

        into.Clear();
        if (string.IsNullOrEmpty(coverPartId))
        {
            into.AddRange(_worn);
            return;
        }

        for (int i = 0; i < _worn.Count; i++)
        {
            ItemStack stack = _worn[i];
            if (GearHandleRules.CoversPart(stack?.Item, coverPartId))
                into.Add(stack);
        }
    }

    public void Clear()
    {
        if (_worn.Count == 0)
            return;
        _worn.Clear();
        Changed?.Invoke();
    }
}
