// ============================================================
// CraftingMaterialPool — 사이드바 컨테이너를 합성 재료/공구 풀로 합산
// ============================================================

using System;
using System.Collections.Generic;
using Garunnir.Runtime.Gameplay.Data;

public sealed class CraftingMaterialPool : IItemContainer
{
    readonly List<InventoryContainer> _sources = new();
    readonly List<InventoryContainer> _consumeOrder = new();
    readonly string _playerBodyInstanceId;

    public IReadOnlyList<InventoryContainer> Sources => _sources;

    public CraftingMaterialPool(
        IReadOnlyList<InventoryContainer> sources,
        Func<string, bool> isLootContainer = null,
        string playerBodyInstanceId = null)
    {
        _playerBodyInstanceId = playerBodyInstanceId ?? string.Empty;

        if (sources != null)
        {
            for (int i = 0; i < sources.Count; i++)
            {
                InventoryContainer container = sources[i];
                if (container != null)
                    _sources.Add(container);
            }
        }

        BuildConsumeOrder(isLootContainer);
    }

    public int CountItem(string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
            return 0;

        int total = 0;
        for (int i = 0; i < _sources.Count; i++)
            total += _sources[i].CountItem(itemId);
        return total;
    }

    public int CountToolCharges(string toolId)
    {
        if (string.IsNullOrEmpty(toolId))
            return 0;

        int total = 0;
        for (int i = 0; i < _sources.Count; i++)
        {
            IReadOnlyList<ItemStack> stacks = _sources[i].Stacks;
            for (int s = 0; s < stacks.Count; s++)
            {
                ItemStack stack = stacks[s];
                if (stack?.Instance == null || stack.ItemId != toolId)
                    continue;
                total += stack.Instance.ToolCharges;
            }
        }

        return total;
    }

    public bool TryRemoveItem(string itemId, int count)
    {
        if (string.IsNullOrEmpty(itemId) || count <= 0)
            return false;

        if (CountItem(itemId) < count)
            return false;

        int remaining = count;
        for (int i = 0; i < _consumeOrder.Count && remaining > 0; i++)
        {
            int removed = _consumeOrder[i].RemoveItem(itemId, remaining);
            remaining -= removed;
        }

        return remaining <= 0;
    }

    public bool TryConsumeToolCharges(string toolId, int amount)
    {
        if (string.IsNullOrEmpty(toolId) || amount <= 0)
            return false;

        if (CountToolCharges(toolId) < amount)
            return false;

        int remaining = amount;
        for (int i = 0; i < _consumeOrder.Count && remaining > 0; i++)
        {
            InventoryContainer container = _consumeOrder[i];
            IReadOnlyList<ItemStack> stacks = container.Stacks;
            int consumedHere = 0;

            for (int s = 0; s < stacks.Count && remaining > 0; s++)
            {
                ItemStack stack = stacks[s];
                if (stack?.Instance == null || stack.ItemId != toolId)
                    continue;

                int have = stack.Instance.ToolCharges;
                if (have <= 0)
                    continue;

                int take = Math.Min(have, remaining);
                if (!stack.Instance.TryConsumeToolCharges(take))
                    continue;

                remaining -= take;
                consumedHere += take;
            }

            if (consumedHere > 0)
                container.NotifyContentsChanged();
        }

        return remaining <= 0;
    }

    public int TryAddResult(string itemId, int count)
    {
        if (string.IsNullOrEmpty(itemId) || count <= 0)
            return 0;

        InventoryContainer dest = FindPlayerBody() ?? (_sources.Count > 0 ? _sources[0] : null);
        return dest != null ? dest.AddItem(itemId, count) : 0;
    }

    InventoryContainer FindPlayerBody()
    {
        if (string.IsNullOrEmpty(_playerBodyInstanceId))
            return null;

        for (int i = 0; i < _sources.Count; i++)
        {
            if (_sources[i].InstanceId == _playerBodyInstanceId)
                return _sources[i];
        }

        return null;
    }

    void BuildConsumeOrder(Func<string, bool> isLootContainer)
    {
        InventoryContainer body = null;
        List<InventoryContainer> owned = new();
        List<InventoryContainer> loot = new();

        for (int i = 0; i < _sources.Count; i++)
        {
            InventoryContainer container = _sources[i];
            if (!string.IsNullOrEmpty(_playerBodyInstanceId) &&
                container.InstanceId == _playerBodyInstanceId)
            {
                body = container;
                continue;
            }

            bool isLoot = isLootContainer != null && isLootContainer(container.InstanceId);
            if (isLoot)
                loot.Add(container);
            else
                owned.Add(container);
        }

        if (body != null)
            _consumeOrder.Add(body);

        for (int i = 0; i < owned.Count; i++)
            _consumeOrder.Add(owned[i]);
        for (int i = 0; i < loot.Count; i++)
            _consumeOrder.Add(loot[i]);
    }
}
