// ============================================================
// InventoryStartupSeeder — 시작 시 특정 ContainerId 대상 아이템 주입
// ============================================================

using System;
using System.Collections;
using System.Collections.Generic;
using Garunnir.Runtime.Gameplay.Item;
using UnityEngine;

public sealed class InventoryStartupSeeder : MonoBehaviour
{
    [Serializable]
    struct SeedEntry
    {
        public ItemDefinitionSO Item;
        [Min(1)] public int Count;
    }

    [SerializeField] string _targetContainerId = PlayerInventoryHost.DefaultInstanceId;
    [SerializeField] List<SeedEntry> _entries = new();
    [SerializeField] bool _useCatalogDefaultsWhenEmpty = true;
    [SerializeField] bool _runOnlyOnce = true;

    bool _seeded;

    void Start()
    {
        StartCoroutine(SeedWhenReady());
    }

    IEnumerator SeedWhenReady()
    {
        if (_runOnlyOnce && _seeded)
            yield break;

        // Provider들이 OnEnable에서 등록될 시간을 1프레임 준다.
        yield return null;

        if (!InventoryContainerRegistry.TryGetContainer(_targetContainerId, out InventoryContainer container))
        {
            Debug.LogWarning($"[InventoryStartupSeeder] Target container not found: '{_targetContainerId}'.", this);
            yield break;
        }

        bool changed = false;
        if (_entries.Count == 0 && _useCatalogDefaultsWhenEmpty)
        {
            ItemCatalogSO catalog = GameplayData.ItemCatalog;
            if (catalog != null)
            {
                ItemDefinitionSO first = catalog.GetByIndex(0);
                ItemDefinitionSO second = catalog.GetByIndex(1);
                if (first != null)
                    changed |= container.AddItem(first, 1) > 0;
                if (second != null)
                    changed |= container.AddItem(second, 1) > 0;
            }
        }

        for (int i = 0; i < _entries.Count; i++)
        {
            SeedEntry entry = _entries[i];
            if (entry.Item == null || entry.Count <= 0)
                continue;

            changed |= container.AddItem(entry.Item, entry.Count) > 0;
        }

        if (changed)
        {
            PlayerInventoryRuntime runtime = PlayerInventoryRuntime.Active;
            if (runtime?.Session != null)
                runtime.Session.NotifyExternalStacksChanged();

            Debug.Log(
                $"[InventoryStartupSeeder] Seeded '{_targetContainerId}' stacks={container.Stacks.Count}",
                this);
        }

        _seeded = true;
    }
}
