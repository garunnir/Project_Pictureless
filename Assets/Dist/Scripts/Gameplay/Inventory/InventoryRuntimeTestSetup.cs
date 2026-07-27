// ============================================================
// InventoryRuntimeTestSetup — 플레이 모드 런타임 테스트용 인벤·바닥 아이템 주입
// ============================================================

using System;
using System.Collections;
using System.Collections.Generic;
using Garunnir.Runtime.Gameplay.Data;
using IsoTilemap;
using Sirenix.OdinInspector;
using UnityEngine;

public sealed class InventoryRuntimeTestSetup : MonoBehaviour
{
    [Serializable]
    struct ContainerSeedEntry
    {
        public string ItemId;
        [Min(1)] public int Count;
    }

    [Serializable]
    struct SmallItemSpawnEntry
    {
        public string ItemId;
        [Min(1)] public int Count;
        public Vector3 LocalPosition;
    }

    [Title("Container Seed")]
    [SerializeField] string _targetContainerId = PlayerInventoryHost.DefaultInstanceId;
    [SerializeField] List<ContainerSeedEntry> _containerEntries = new();
    [SerializeField] bool _seedContainerOnStart = true;
    [SerializeField] bool _useCatalogDefaultsWhenEmpty = true;

    [Title("Floor Small Items")]
    [SerializeField] bool _spawnSmallItemsOnStart = true;
    [Required, SerializeField] SmallItemObject _smallItemPrefab;
    [SerializeField] Transform _smallItemSpawnRoot;
    [SerializeField] List<SmallItemSpawnEntry> _smallItemSpawns = new();

    [SerializeField] bool _runOnlyOnce = true;

    readonly List<SmallItemObject> _spawnedSmallItems = new();
    bool _ran;

    void Reset()
    {
        _smallItemSpawnRoot = transform;
        if (_smallItemSpawns.Count == 0)
        {
            _smallItemSpawns.Add(new SmallItemSpawnEntry
            {
                LocalPosition = new Vector3(0.5f, 0f, 0f),
                Count = 1
            });
            _smallItemSpawns.Add(new SmallItemSpawnEntry
            {
                LocalPosition = new Vector3(0f, 0f, 0.5f),
                Count = 1
            });
        }
    }

    void Start()
    {
        if (_runOnlyOnce && _ran)
            return;

        StartCoroutine(RunWhenReady());
    }

    IEnumerator RunWhenReady()
    {
        yield return null;

        if (_seedContainerOnStart)
            SeedContainer();

        if (_spawnSmallItemsOnStart)
            SpawnSmallItems();

        _ran = true;
    }

    void SeedContainer()
    {
        if (!InventoryContainerRegistry.TryGetContainer(_targetContainerId, out InventoryContainer container))
        {
            Debug.LogWarning($"[InventoryRuntimeTestSetup] Target container not found: '{_targetContainerId}'.", this);
            return;
        }

        bool changed = false;
        if (_containerEntries.Count == 0 && _useCatalogDefaultsWhenEmpty)
        {
            int before = container.Stacks.Count;
            InventoryDemoSeeder.SeedIfEmpty(container);
            changed = container.Stacks.Count > before;
        }

        for (int i = 0; i < _containerEntries.Count; i++)
        {
            ContainerSeedEntry entry = _containerEntries[i];
            if (string.IsNullOrEmpty(entry.ItemId) || entry.Count <= 0)
                continue;

            ItemData item = ResolveItem(entry.ItemId);
            if (item == null)
            {
                Debug.LogWarning($"[InventoryRuntimeTestSetup] Item '{entry.ItemId}' not found.", this);
                continue;
            }

            changed |= container.AddItem(item, entry.Count) > 0;
        }

        if (!changed)
            return;

        PlayerInventoryRuntime runtime = PlayerInventoryRuntime.Active;
        runtime?.Session?.NotifyExternalStacksChanged(container);

        Debug.Log(
            $"[InventoryRuntimeTestSetup] Seeded '{_targetContainerId}' stacks={container.Stacks.Count}",
            this);
    }

    void SpawnSmallItems()
    {
        if (_smallItemPrefab == null)
        {
            Debug.LogWarning("[InventoryRuntimeTestSetup] SmallItem prefab is not assigned.", this);
            return;
        }

        if (_smallItemSpawns.Count == 0)
            return;

        Transform root = _smallItemSpawnRoot != null ? _smallItemSpawnRoot : transform;
        IWorldGrid worldGrid = ResolveWorldGrid();

        int spawnedCount = 0;
        for (int i = 0; i < _smallItemSpawns.Count; i++)
        {
            SmallItemSpawnEntry entry = _smallItemSpawns[i];
            if (string.IsNullOrEmpty(entry.ItemId))
                continue;

            ItemData item = ResolveItem(entry.ItemId);
            if (item == null)
                continue;

            SmallItemObject instance = SmallItemSpawner.SpawnLocal(
                _smallItemPrefab,
                item,
                entry.Count,
                root,
                entry.LocalPosition,
                worldGrid);

            if (instance == null)
                continue;

            _spawnedSmallItems.Add(instance);
            spawnedCount++;
        }

        if (spawnedCount > 0)
        {
            PlayerInventoryRuntime.Active?.RefreshNearbyContainers();
            Debug.Log($"[InventoryRuntimeTestSetup] Spawned floor small items={spawnedCount}", this);
        }
    }

    static ItemData ResolveItem(string itemId)
    {
        return GameplayData.GetItem(itemId);
    }

    IWorldGrid ResolveWorldGrid()
    {
        var tileMapManager = FindFirstObjectByType<TileMapManager>();
        return tileMapManager != null ? tileMapManager.WorldGrid : null;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Transform root = _smallItemSpawnRoot != null ? _smallItemSpawnRoot : transform;
        Gizmos.color = new Color(0.4f, 0.85f, 1f, 0.9f);

        for (int i = 0; i < _smallItemSpawns.Count; i++)
        {
            Vector3 world = root.TransformPoint(_smallItemSpawns[i].LocalPosition);
            Gizmos.DrawWireSphere(world, 0.15f);
        }
    }
#endif
}
