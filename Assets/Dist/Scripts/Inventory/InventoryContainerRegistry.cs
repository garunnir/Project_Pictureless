// ============================================================
// InventoryContainerRegistry — ContainerId 기반 런타임 조회 레지스트리
// ============================================================

using System.Collections.Generic;
using UnityEngine;

public static class InventoryContainerRegistry
{
    static readonly Dictionary<string, IInventoryContainerProvider> ProvidersById = new();
    static readonly List<IInventoryContainerProvider> ProvidersScratch = new();

    public static bool Register(IInventoryContainerProvider provider)
    {
        if (provider == null || provider.Container == null || string.IsNullOrWhiteSpace(provider.ContainerId))
            return false;

        if (ProvidersById.TryGetValue(provider.ContainerId, out IInventoryContainerProvider existing) &&
            existing != provider)
        {
            Debug.LogError($"[InventoryContainerRegistry] Duplicate ContainerId '{provider.ContainerId}'.");
            return false;
        }

        ProvidersById[provider.ContainerId] = provider;
        return true;
    }

    public static void Unregister(IInventoryContainerProvider provider)
    {
        if (provider == null || string.IsNullOrWhiteSpace(provider.ContainerId))
            return;

        if (ProvidersById.TryGetValue(provider.ContainerId, out IInventoryContainerProvider current) &&
            current == provider)
        {
            ProvidersById.Remove(provider.ContainerId);
        }
    }

    public static bool TryGetContainer(string containerId, out InventoryContainer container)
    {
        container = null;
        if (string.IsNullOrWhiteSpace(containerId))
            return false;

        if (!ProvidersById.TryGetValue(containerId, out IInventoryContainerProvider provider))
            return false;

        container = provider.Container;
        return container != null;
    }

    public static bool TryGetProviderByInstanceId(
        string instanceId,
        out IInventoryContainerProvider provider)
    {
        provider = null;
        if (string.IsNullOrWhiteSpace(instanceId))
            return false;

        foreach (KeyValuePair<string, IInventoryContainerProvider> entry in ProvidersById)
        {
            IInventoryContainerProvider candidate = entry.Value;
            if (candidate?.Container == null)
                continue;

            if (candidate.Container.InstanceId == instanceId)
            {
                provider = candidate;
                return true;
            }
        }

        return false;
    }

    public static IReadOnlyList<IInventoryContainerProvider> GetProvidersSnapshot()
    {
        ProvidersScratch.Clear();
        foreach (KeyValuePair<string, IInventoryContainerProvider> entry in ProvidersById)
        {
            if (entry.Value != null)
                ProvidersScratch.Add(entry.Value);
        }

        return ProvidersScratch;
    }
}
