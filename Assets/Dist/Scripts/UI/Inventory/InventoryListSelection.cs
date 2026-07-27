// ============================================================
// InventoryListSelection — 리스트 내 다중 스택 선택 SSOT
// ============================================================

using System;
using System.Collections.Generic;

public sealed class InventoryListSelection
{
    readonly HashSet<ItemStack> _selected = new();
    readonly List<ItemStack> _scratch = new();

    public event Action SelectionChanged;

    public int Count => _selected.Count;

    public bool IsSelected(ItemStack stack) => stack != null && _selected.Contains(stack);

    public void Clear()
    {
        if (_selected.Count == 0)
            return;

        _selected.Clear();
        SelectionChanged?.Invoke();
    }

    public void SetSingle(ItemStack stack)
    {
        _selected.Clear();
        if (stack != null)
            _selected.Add(stack);

        SelectionChanged?.Invoke();
    }

    public void SetMany(IReadOnlyList<ItemStack> stacks)
    {
        _selected.Clear();
        if (stacks != null)
        {
            for (int i = 0; i < stacks.Count; i++)
            {
                if (stacks[i] != null)
                    _selected.Add(stacks[i]);
            }
        }

        SelectionChanged?.Invoke();
    }

    public IReadOnlyList<ItemStack> GetSelectedStacks()
    {
        _scratch.Clear();
        foreach (ItemStack stack in _selected)
            _scratch.Add(stack);

        return _scratch;
    }

    public void Remove(ItemStack stack)
    {
        if (stack == null || !_selected.Remove(stack))
            return;

        SelectionChanged?.Invoke();
    }
}
