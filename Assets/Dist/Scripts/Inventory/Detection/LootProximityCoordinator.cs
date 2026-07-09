// ============================================================
// LootProximityCoordinator — 반경 내 월드 컨테이너 감지·활성 선택 SSOT
// ============================================================

using System;
using System.Collections.Generic;

public sealed class LootProximityCoordinator
{
    readonly List<InventoryContainer> _detected = new();
    readonly HashSet<string> _detectedIds = new();

    string _activeInstanceId = string.Empty;

    public event Action<IReadOnlyList<InventoryContainer>> NearbyContainersChanged;
    public event Action<InventoryContainer> ActiveLootContainerChanged;

    public IReadOnlyList<InventoryContainer> DetectedContainers => _detected;

    public InventoryContainer ActiveContainer
    {
        get
        {
            if (string.IsNullOrEmpty(_activeInstanceId))
                return null;

            for (int i = 0; i < _detected.Count; i++)
            {
                InventoryContainer container = _detected[i];
                if (container != null && container.InstanceId == _activeInstanceId)
                    return container;
            }

            return null;
        }
    }

    public void NotifyDetectedContainers(IReadOnlyList<InventoryContainer> detected)
    {
        _detected.Clear();
        _detectedIds.Clear();

        if (detected != null)
        {
            for (int i = 0; i < detected.Count; i++)
            {
                InventoryContainer container = detected[i];
                if (container == null || _detectedIds.Contains(container.InstanceId))
                    continue;

                _detected.Add(container);
                _detectedIds.Add(container.InstanceId);
            }
        }

        NearbyContainersChanged?.Invoke(_detected);
        EnsureActiveInDetectedSet();
    }

    public bool RequestActiveContainer(InventoryContainer container)
    {
        if (container == null || !_detectedIds.Contains(container.InstanceId))
            return false;

        if (_activeInstanceId == container.InstanceId)
            return true;

        _activeInstanceId = container.InstanceId;
        ActiveLootContainerChanged?.Invoke(container);
        return true;
    }

    public bool RequestActiveContainer(string instanceId)
    {
        if (string.IsNullOrEmpty(instanceId) || !_detectedIds.Contains(instanceId))
            return false;

        for (int i = 0; i < _detected.Count; i++)
        {
            InventoryContainer container = _detected[i];
            if (container != null && container.InstanceId == instanceId)
                return RequestActiveContainer(container);
        }

        return false;
    }

    public void ClearActive()
    {
        if (string.IsNullOrEmpty(_activeInstanceId))
            return;

        _activeInstanceId = string.Empty;
        ActiveLootContainerChanged?.Invoke(null);
    }

    void EnsureActiveInDetectedSet()
    {
        if (_detected.Count == 0)
        {
            if (!string.IsNullOrEmpty(_activeInstanceId))
            {
                _activeInstanceId = string.Empty;
                ActiveLootContainerChanged?.Invoke(null);
            }

            return;
        }

        if (!string.IsNullOrEmpty(_activeInstanceId) && _detectedIds.Contains(_activeInstanceId))
            return;

        InventoryContainer next = _detected[0];
        _activeInstanceId = next != null ? next.InstanceId : string.Empty;
        ActiveLootContainerChanged?.Invoke(next);
    }
}
