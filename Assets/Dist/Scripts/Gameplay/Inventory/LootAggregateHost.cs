// ============================================================
// LootAggregateHost — 사이드바 소스 스택을 모으는 가상 집계 컨테이너 SSOT
// ============================================================

using System.Collections.Generic;
using Garunnir.Runtime.Gameplay.Data;
using UnityEngine;

public sealed class LootAggregateHost
{
    public const string DefaultInstanceId = "loot-aggregate";
    public const string DefaultContainerDefId = "loot_aggregate";

    readonly InventoryContainer _container;
    readonly Dictionary<ItemStack, InventoryContainer> _stackToOwner = new();
    readonly List<ItemStack> _removalScratch = new();

    InventorySession _session;
    bool _isActive;
    int _lastSourceContentSignature;

    public InventoryContainer Container => _container;
    public bool IsActive => _isActive;

    public LootAggregateHost(string containerDefId, InventorySession session)
    {
        _session = session ?? throw new System.ArgumentNullException(nameof(session));

        ContainerData containerDef = GameplayData.GetContainer(containerDefId);
        if (containerDef == null)
        {
            Debug.LogError($"[LootAggregateHost] Container definition '{containerDefId}' not found.");
            return;
        }

        _container = InventoryContainer.Create(
            containerDef,
            new FixedContainerCapacityPolicy(),
            DefaultInstanceId);
    }

    public void Dispose() => EndContext();

    public void BeginContext()
    {
        if (_container == null || _isActive)
            return;

        _isActive = true;
        _lastSourceContentSignature = 0;
    }

    public void EndContext()
    {
        if (!_isActive)
            return;

        _isActive = false;
        _lastSourceContentSignature = 0;
        ClearSyncedStacks();
    }

    public void SyncFromSources(IReadOnlyList<InventoryContainer> sidebarSources)
    {
        if (_container == null || !_isActive)
            return;

        var desiredStacks = new HashSet<ItemStack>();
        bool changed = false;

        if (sidebarSources != null)
        {
            for (int i = 0; i < sidebarSources.Count; i++)
            {
                InventoryContainer source = sidebarSources[i];
                if (source == null || source == _container)
                    continue;

                if (IsAggregateContainer(source))
                    continue;

                if (source.InstanceId == PlayerInventoryHost.DefaultInstanceId)
                    continue;

                IReadOnlyList<ItemStack> stacks = source.Stacks;
                for (int s = 0; s < stacks.Count; s++)
                {
                    ItemStack stack = stacks[s];
                    if (stack == null)
                        continue;

                    desiredStacks.Add(stack);
                    _stackToOwner[stack] = source;
                }
            }
        }

        _removalScratch.Clear();
        IReadOnlyList<ItemStack> currentStacks = _container.Stacks;
        for (int i = 0; i < currentStacks.Count; i++)
        {
            ItemStack stack = currentStacks[i];
            if (stack == null || desiredStacks.Contains(stack))
                continue;

            _removalScratch.Add(stack);
        }

        for (int i = 0; i < _removalScratch.Count; i++)
        {
            ItemStack stack = _removalScratch[i];
            changed |= _container.TryRemoveStackReference(stack);
            _stackToOwner.Remove(stack);
        }

        foreach (ItemStack stack in desiredStacks)
            changed |= _container.TryAddStackReference(stack);

        int sourceSignature = ComputeSourceContentSignature(sidebarSources);
        bool sourcesContentChanged = sourceSignature != _lastSourceContentSignature;
        _lastSourceContentSignature = sourceSignature;

        if (changed)
            _session.NotifyExternalStacksChanged(_container);
        else if (sourcesContentChanged)
            _container.NotifyContentsChanged();
    }

    static int ComputeSourceContentSignature(IReadOnlyList<InventoryContainer> sidebarSources)
    {
        if (sidebarSources == null || sidebarSources.Count == 0)
            return 0;

        unchecked
        {
            int signature = 17;
            for (int i = 0; i < sidebarSources.Count; i++)
            {
                InventoryContainer source = sidebarSources[i];
                if (source == null)
                    continue;

                if (source.InstanceId == DefaultInstanceId)
                    continue;

                if (source.InstanceId == PlayerInventoryHost.DefaultInstanceId)
                    continue;

                signature = signature * 31 + source.ContentVersion;
                signature = signature * 31 + source.Stacks.Count;
            }

            return signature;
        }
    }

    public static bool IsAggregateContainer(InventoryContainer container) =>
        container != null && container.InstanceId == DefaultInstanceId;

    public bool TryGetOwner(ItemStack stack, out InventoryContainer owner)
    {
        if (stack == null)
        {
            owner = null;
            return false;
        }

        return _stackToOwner.TryGetValue(stack, out owner);
    }

    void ClearSyncedStacks()
    {
        _container?.ClearStackReferences();
        _stackToOwner.Clear();
    }
}
