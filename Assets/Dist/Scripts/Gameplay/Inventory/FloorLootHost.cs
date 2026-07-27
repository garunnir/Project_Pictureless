// ============================================================
// FloorLootHost — 주변 소형 아이템을 모으는 가상 바닥 컨테이너 SSOT
// ============================================================

using System.Collections.Generic;
using Garunnir.Runtime.Gameplay.Data;
using IsoTilemap;
using UnityEngine;

public sealed class FloorLootHost
{
    public const string DefaultInstanceId = "floor-loot";
    public const string DefaultContainerDefId = "floor_loot";

    readonly InventoryContainer _container;
    readonly Dictionary<ItemStack, SmallItemObject> _stackToObject = new();
    readonly List<ItemStack> _removalScratch = new();
    readonly List<ItemStack> _additionScratch = new();

    InventorySession _session;
    SmallItemObject _smallItemPrefab;
    System.Func<Vector3> _resolveDropWorldPosition;
    System.Func<IWorldGrid> _resolveWorldGrid;
    bool _isActive;

    public InventoryContainer Container => _container;
    public bool IsActive => _isActive;

    public FloorLootHost(
        string containerDefId,
        InventorySession session,
        SmallItemObject smallItemPrefab = null,
        System.Func<Vector3> resolveDropWorldPosition = null,
        System.Func<IWorldGrid> resolveWorldGrid = null)
    {
        _session = session ?? throw new System.ArgumentNullException(nameof(session));
        _smallItemPrefab = smallItemPrefab;
        _resolveDropWorldPosition = resolveDropWorldPosition;
        _resolveWorldGrid = resolveWorldGrid;

        ContainerData containerDef = GameplayData.GetContainer(containerDefId);
        if (containerDef == null)
        {
            Debug.LogError($"[FloorLootHost] Container definition '{containerDefId}' not found.");
            return;
        }

        _container = InventoryContainer.Create(
            containerDef,
            new FixedContainerCapacityPolicy(),
            DefaultInstanceId);

        _session.StacksChanged += OnStacksChanged;
    }

    public void Dispose()
    {
        if (_session != null)
            _session.StacksChanged -= OnStacksChanged;

        EndContext();
    }

    public void BeginContext()
    {
        if (_container == null || _isActive)
            return;

        _isActive = true;
        _session.TryAddSidebarContainer(_container);
    }

    public void EndContext()
    {
        if (!_isActive)
            return;

        _isActive = false;
        ClearSyncedStacks();
        _session.TryRemoveSidebarContainer(DefaultInstanceId);
    }

    public void SyncFromNearbyItems(IReadOnlyList<SmallItemObject> nearbyItems)
    {
        if (_container == null || !_isActive)
            return;

        var desiredStacks = new HashSet<ItemStack>();
        bool changed = false;

        if (nearbyItems != null)
        {
            for (int i = 0; i < nearbyItems.Count; i++)
            {
                SmallItemObject item = nearbyItems[i];
                ItemStack stack = item?.Stack;
                if (stack == null)
                    continue;

                desiredStacks.Add(stack);
                _stackToObject[stack] = item;
            }
        }

        _removalScratch.Clear();
        IReadOnlyList<ItemStack> stacksRead = _container.Stacks;
        for (int i = 0; i < stacksRead.Count; i++)
        {
            ItemStack stack = stacksRead[i];
            if (stack == null || desiredStacks.Contains(stack))
                continue;

            _removalScratch.Add(stack);
        }

        for (int i = 0; i < _removalScratch.Count; i++)
        {
            ItemStack stack = _removalScratch[i];
            changed |= _container.TryRemoveStackReference(stack);
            _stackToObject.Remove(stack);
        }

        foreach (ItemStack stack in desiredStacks)
            changed |= _container.TryAddStackReference(stack);

        if (changed)
            _session.NotifyExternalStacksChanged(_container);
    }

    void OnStacksChanged(InventoryStacksChangeSet changeSet)
    {
        if (_container == null || changeSet == null)
            return;

        if (!changeSet.FullRefresh && !changeSet.Contains(_container))
            return;

        SpawnWorldObjectsForOrphanStacks();

        _removalScratch.Clear();
        IReadOnlyList<ItemStack> stacks = _container.Stacks;

        foreach (KeyValuePair<ItemStack, SmallItemObject> entry in _stackToObject)
        {
            ItemStack stack = entry.Key;
            if (stack == null || ContainsStack(stacks, stack))
                continue;

            _removalScratch.Add(stack);
        }

        for (int i = 0; i < _removalScratch.Count; i++)
        {
            ItemStack stack = _removalScratch[i];
            if (!_stackToObject.TryGetValue(stack, out SmallItemObject item))
                continue;

            _stackToObject.Remove(stack);
            item?.NotifyPickedUp();
        }
    }

    void SpawnWorldObjectsForOrphanStacks()
    {
        if (_smallItemPrefab == null || _resolveDropWorldPosition == null)
            return;

        IReadOnlyList<ItemStack> stacks = _container.Stacks;
        _additionScratch.Clear();

        for (int i = 0; i < stacks.Count; i++)
        {
            ItemStack stack = stacks[i];
            if (stack?.Item == null || _stackToObject.ContainsKey(stack))
                continue;

            _additionScratch.Add(stack);
        }

        if (_additionScratch.Count == 0)
            return;

        Vector3 worldPos = _resolveDropWorldPosition.Invoke();
        IWorldGrid worldGrid = _resolveWorldGrid?.Invoke();

        for (int i = 0; i < _additionScratch.Count; i++)
        {
            ItemStack stack = _additionScratch[i];
            SmallItemObject spawned = SmallItemSpawner.Spawn(
                _smallItemPrefab,
                stack,
                worldPos,
                worldGrid);

            if (spawned != null)
                _stackToObject[stack] = spawned;
        }
    }

    void ClearSyncedStacks()
    {
        _container?.ClearStackReferences();
        _stackToObject.Clear();
    }

    static bool ContainsStack(IReadOnlyList<ItemStack> stacks, ItemStack target)
    {
        if (target == null)
            return false;

        for (int i = 0; i < stacks.Count; i++)
        {
            if (ReferenceEquals(stacks[i], target))
                return true;
        }

        return false;
    }
}
