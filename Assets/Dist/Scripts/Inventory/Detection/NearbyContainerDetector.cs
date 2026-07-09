// ============================================================
// NearbyContainerDetector — GridPos 주변 컨테이너 감지 → Session 사이드바 증감
// ============================================================

using System.Collections.Generic;
using System.Text;
using Sirenix.OdinInspector;
using UnityEngine;

[RequireComponent(typeof(CharacterState))]
public sealed class NearbyContainerDetector : MonoBehaviour
{
    const string LogPrefix = "[NearbyContainerDetector]";

    [Required, SerializeField] CharacterState _characterState;
    [SerializeField, Min(0)] int _radiusCells = 2;
    [SerializeField] bool _sameFloorOnly = true;
    [SerializeField, Min(0)] int _verticalToleranceCells = 0;

    readonly List<IInventoryContainerProvider> _scanResults = new();
    readonly List<InventoryContainer> _detectedContainersScratch = new();
    readonly HashSet<string> _managedWorldContainerIds = new();

    InventorySession _session;
    LootProximityCoordinator _lootProximity;
    bool _isActive;

    public void Bind(InventorySession session, LootProximityCoordinator lootProximity)
    {
        _session = session;
        _lootProximity = lootProximity;
    }

    public void Activate()
    {
        if (_isActive)
            return;

        _isActive = true;
        Subscribe();
        RefreshImmediate();
    }

    public void Deactivate()
    {
        if (!_isActive)
            return;

        _isActive = false;
        Unsubscribe();
        RemoveManagedContainers();
    }

    public void RefreshImmediate()
    {
        if (_characterState == null)
            return;

        Refresh(_characterState.ResolveCurrentGridCell());
    }

    void OnDestroy() => Deactivate();

    void OnValidate() => EnsureReferences();
    void Reset() => EnsureReferences();

    void EnsureReferences()
    {
        if (!_characterState) TryGetComponent(out _characterState);
    }

    void Subscribe() => _characterState.GridPosChanged += OnGridPosChanged;

    void Unsubscribe() => _characterState.GridPosChanged -= OnGridPosChanged;

    void OnGridPosChanged(Vector3Int gridPos)
    {
        if (!_isActive)
            return;

        Refresh(gridPos);
    }

    void Refresh(Vector3Int center)
    {
        if (_session == null || _characterState == null)
            return;

        IReadOnlyList<IInventoryContainerProvider> providers = InventoryContainerRegistry.GetProvidersSnapshot();
        _scanResults.Clear();

        var desiredIds = new HashSet<string>();
        _detectedContainersScratch.Clear();
        GameObject player = _characterState.gameObject;

        int skippedNull = 0;
        int skippedDistance = 0;
        int skippedUnavailable = 0;
        int skippedPlayerBody = 0;

        for (int i = 0; i < providers.Count; i++)
        {
            IInventoryContainerProvider provider = providers[i];
            if (provider?.Container == null)
            {
                skippedNull++;
                continue;
            }

            InventoryContainer container = provider.Container;
            if (container.InstanceId == PlayerInventoryHost.DefaultInstanceId)
            {
                skippedPlayerBody++;
                continue;
            }

            Vector3Int providerCell = _characterState.ResolveGridCell(provider.WorldPosition);
            if (!IsInScanRange(center, providerCell))
            {
                skippedDistance++;
                continue;
            }

            _scanResults.Add(provider);

            if (!provider.IsAvailableToPlayer(player))
            {
                skippedUnavailable++;
                continue;
            }

            string instanceId = container.InstanceId;
            desiredIds.Add(instanceId);
            _detectedContainersScratch.Add(container);
            _session.TryAddSidebarContainer(container);
        }

        var staleIds = new List<string>();
        foreach (string managedId in _managedWorldContainerIds)
        {
            if (!desiredIds.Contains(managedId))
                staleIds.Add(managedId);
        }

        for (int i = 0; i < staleIds.Count; i++)
            _session.TryRemoveSidebarContainer(staleIds[i]);

        _managedWorldContainerIds.Clear();
        foreach (string id in desiredIds)
            _managedWorldContainerIds.Add(id);

        if (DebugLogController.InventoryProximityScanEnabled)
            LogScan(
                center,
                skippedNull,
                skippedDistance,
                skippedUnavailable,
                skippedPlayerBody,
                staleIds.Count);

        PublishDetectedContainers();
    }

    bool IsInScanRange(Vector3Int center, Vector3Int target)
    {
        int dx = Mathf.Abs(target.x - center.x);
        int dz = Mathf.Abs(target.z - center.z);
        if (dx > _radiusCells || dz > _radiusCells)
            return false;

        if (_sameFloorOnly)
            return target.y == center.y;

        int yDiff = Mathf.Abs(target.y - center.y);
        return yDiff <= Mathf.Max(0, _verticalToleranceCells);
    }

    void LogScan(
        Vector3Int center,
        int skippedNull,
        int skippedDistance,
        int skippedUnavailable,
        int skippedPlayerBody,
        int removedCount)
    {
        Vector3Int cachedGridPos = _characterState != null ? _characterState.GridPos : Vector3Int.zero;
        Vector3 bodyWorld = _characterState != null ? _characterState.BodyWorldPoint : Vector3.zero;
        Vector3 transformWorld = _characterState != null ? _characterState.transform.position : Vector3.zero;

        var message = new StringBuilder(256);
        message.Append(LogPrefix).Append(" Scan ");
        message.Append("playerCell=").Append(center);
        message.Append(" cachedGridPos=").Append(cachedGridPos);
        message.Append(" bodyWorld=").Append(bodyWorld);
        message.Append(" transform=").Append(transformWorld);
        message.Append(" radius=").Append(_radiusCells);
        message.Append(" sameFloor=").Append(_sameFloorOnly);
        message.Append(" rawHits=").Append(_scanResults.Count);
        message.Append(" accepted=").Append(_detectedContainersScratch.Count);
        message.Append(" removed=").Append(removedCount);
        message.Append(" skippedNull=").Append(skippedNull);
        message.Append(" skippedDistance=").Append(skippedDistance);
        message.Append(" skippedUnavailable=").Append(skippedUnavailable);
        message.Append(" skippedPlayerBody=").Append(skippedPlayerBody);
        message.Append(" active=").Append(_isActive);

        if (_scanResults.Count == 0)
        {
            message.Append(" | no providers matched world-grid scan range");
        }
        else
        {
            message.Append(" | hits:");
            for (int i = 0; i < _scanResults.Count; i++)
            {
                IInventoryContainerProvider provider = _scanResults[i];
                if (provider == null)
                {
                    message.Append(" [null]");
                    continue;
                }

                InventoryContainer container = provider.Container;
                string id = container != null ? container.InstanceId : "no-container";
                Vector3 providerWorld = provider.WorldPosition;
                Vector3Int providerCell = _characterState.ResolveGridCell(providerWorld);
                int yDiff = providerCell.y - center.y;
                message.Append(" {world=").Append(providerWorld);
                message.Append(" cell=").Append(providerCell);
                message.Append(" id=").Append(id);
                message.Append(" yDiff=").Append(yDiff);
                message.Append(" avail=").Append(provider.IsAvailableToPlayer(_characterState.gameObject));
                message.Append('}');
            }
        }

        if (_detectedContainersScratch.Count > 0)
        {
            message.Append(" | managed:");
            foreach (string id in _managedWorldContainerIds)
                message.Append(' ').Append(id);
        }

        DebugLogController.LogInventoryProximityScan(message.ToString(), this);
    }

    public bool IsManagedWorldContainer(string instanceId) =>
        !string.IsNullOrEmpty(instanceId) && _managedWorldContainerIds.Contains(instanceId);

    /// <summary>스캔 누락 시 상호작용 대상 등을 반경 목록에 수동 포함 (이미 managed면 false).</summary>
    public bool TryIncludeManagedContainer(InventoryContainer container)
    {
        if (!_isActive || _session == null || container == null)
            return false;

        string instanceId = container.InstanceId;
        if (string.IsNullOrEmpty(instanceId) || _managedWorldContainerIds.Contains(instanceId))
            return false;

        _session.TryAddSidebarContainer(container);
        _managedWorldContainerIds.Add(instanceId);
        DebugLogController.LogInventoryProximityScan(
            $"{LogPrefix} TryIncludeManagedContainer manual include id={instanceId}",
            this);

        PublishDetectedContainers();
        return true;
    }

    void PublishDetectedContainers()
    {
        _detectedContainersScratch.Clear();

        if (_session != null)
        {
            IReadOnlyList<InventoryContainer> sidebar = _session.GetSidebarContainers();
            for (int i = 0; i < sidebar.Count; i++)
            {
                InventoryContainer candidate = sidebar[i];
                if (candidate == null)
                    continue;

                if (!_managedWorldContainerIds.Contains(candidate.InstanceId))
                    continue;

                if (candidate.InstanceId == PlayerInventoryHost.DefaultInstanceId)
                    continue;

                _detectedContainersScratch.Add(candidate);
            }
        }

        _lootProximity?.NotifyDetectedContainers(_detectedContainersScratch);
    }

    void RemoveManagedContainers()
    {
        if (_session != null)
        {
            foreach (string instanceId in _managedWorldContainerIds)
                _session.TryRemoveSidebarContainer(instanceId);
        }

        _managedWorldContainerIds.Clear();
        _detectedContainersScratch.Clear();
        PublishDetectedContainers();
    }
}
