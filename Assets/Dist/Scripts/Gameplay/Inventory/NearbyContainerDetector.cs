// ============================================================
// NearbyContainerDetector — GridPos 주변 컨테이너 감지 → Session 사이드바 증감
// ============================================================

using System.Collections.Generic;
using System.Text;
using IsoTilemap;
using Sirenix.OdinInspector;
using UnityEngine;

[RequireComponent(typeof(CharacterState))]
public sealed class NearbyContainerDetector : MonoBehaviour
{
    const string LogPrefix = "[NearbyContainerDetector]";

    [Required, SerializeField] CharacterState _characterState;
    [SerializeField] string _floorLootDefId = FloorLootHost.DefaultContainerDefId;
    [Required, SerializeField] SmallItemObject _smallItemPrefab;
    [SerializeField, Min(0)] int _radiusCells = 2;
    [SerializeField] bool _sameFloorOnly = true;
    [SerializeField, Min(0)] int _verticalToleranceCells = 0;

    readonly List<IInventoryContainerProvider> _scanResults = new();
    readonly List<InventoryContainer> _detectedContainersScratch = new();
    readonly List<Vector3Int> _cellsInRangeScratch = new();
    readonly List<SmallItemObject> _nearbySmallItemsScratch = new();
    readonly HashSet<string> _managedWorldContainerIds = new();
    readonly FixedContainerCapacityPolicy _nestedContainerPolicy = new();

    InventorySession _session;
    LootProximityCoordinator _lootProximity;
    FloorLootHost _floorLootHost;
    IWorldGrid _cachedWorldGrid;
    bool _isActive;
    bool _isRefreshing;

    public void Bind(InventorySession session, LootProximityCoordinator lootProximity)
    {
        _session = session;
        _lootProximity = lootProximity;

        _floorLootHost?.Dispose();
        _floorLootHost = !string.IsNullOrEmpty(_floorLootDefId)
            ? new FloorLootHost(
                _floorLootDefId,
                _session,
                _smallItemPrefab,
                ResolveDropWorldPosition,
                ResolveWorldGrid)
            : null;
    }

    Vector3 ResolveDropWorldPosition() =>
        _characterState != null ? _characterState.BodyWorldPoint : Vector3.zero;

    IWorldGrid ResolveWorldGrid()
    {
        if (_cachedWorldGrid != null)
            return _cachedWorldGrid;

        var tileMapManager = FindFirstObjectByType<TileMapManager>();
        _cachedWorldGrid = tileMapManager != null ? tileMapManager.WorldGrid : null;
        return _cachedWorldGrid;
    }

    public void Activate()
    {
        if (_isActive)
            return;

        _isActive = true;
        _floorLootHost?.BeginContext();
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
        _floorLootHost?.EndContext();
    }

    public bool IsLootContainer(string instanceId)
    {
        if (string.IsNullOrEmpty(instanceId))
            return false;

        if (_floorLootHost?.Container != null &&
            instanceId == FloorLootHost.DefaultInstanceId)
            return true;

        return _managedWorldContainerIds.Contains(instanceId);
    }

    public void RefreshImmediate()
    {
        if (_characterState == null)
            return;

        Refresh(_characterState.ResolveCurrentGridCell());
    }

    void OnDestroy()
    {
        Deactivate();
        _floorLootHost?.Dispose();
        _floorLootHost = null;
    }

    void OnValidate() => EnsureReferences();
    void Reset() => EnsureReferences();

    void EnsureReferences()
    {
        if (!_characterState) TryGetComponent(out _characterState);
    }

    void Subscribe()
    {
        _characterState.GridPosChanged += OnGridPosChanged;
        if (_session != null)
            _session.StacksChanged += OnStacksChanged;
    }

    void Unsubscribe()
    {
        _characterState.GridPosChanged -= OnGridPosChanged;
        if (_session != null)
            _session.StacksChanged -= OnStacksChanged;
    }

    void OnGridPosChanged(Vector3Int gridPos)
    {
        if (!_isActive)
            return;

        Refresh(gridPos);
    }

    void OnStacksChanged()
    {
        if (!_isActive || _isRefreshing)
            return;

        RefreshImmediate();
    }

    void Refresh(Vector3Int center)
    {
        if (_session == null || _characterState == null || _isRefreshing)
            return;

        _isRefreshing = true;
        try
        {
            RefreshCore(center);
        }
        finally
        {
            _isRefreshing = false;
        }
    }

    void RefreshCore(Vector3Int center)
    {
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

        SyncFloorLoot(center);
        PromoteFloorNestedContainers(desiredIds);

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

    void SyncFloorLoot(Vector3Int center)
    {
        if (_floorLootHost == null || !_floorLootHost.IsActive)
            return;

        EnumerateCellsInRange(center, _cellsInRangeScratch);
        SmallItemRegistry.CollectInCells(_cellsInRangeScratch, _nearbySmallItemsScratch);
        _floorLootHost.SyncFromNearbyItems(_nearbySmallItemsScratch);
    }

    /// <summary>
    /// floor-loot 안 휴대 컨테이너(Nested)를 managed 월드 루트로 올린다.
    /// session 사이드바에는 넣지 않는다 (PurgeNestedFromSidebar와 충돌 방지).
    /// </summary>
    void PromoteFloorNestedContainers(HashSet<string> desiredIds)
    {
        InventoryContainer floor = _floorLootHost is { IsActive: true } ? _floorLootHost.Container : null;
        if (floor == null || desiredIds == null)
            return;

        IReadOnlyList<ItemStack> stacks = floor.Stacks;
        for (int i = 0; i < stacks.Count; i++)
        {
            ItemStack stack = stacks[i];
            if (stack?.Item == null || !stack.Item.is_container)
                continue;

            if (!stack.TryEnsureNested(_nestedContainerPolicy) || stack.Nested == null)
                continue;

            string nestedId = stack.Nested.InstanceId;
            if (string.IsNullOrEmpty(nestedId))
                continue;

            desiredIds.Add(nestedId);
        }
    }

    void EnumerateCellsInRange(Vector3Int center, List<Vector3Int> buffer)
    {
        buffer.Clear();

        int yMin = _sameFloorOnly ? center.y : center.y - Mathf.Max(0, _verticalToleranceCells);
        int yMax = _sameFloorOnly ? center.y : center.y + Mathf.Max(0, _verticalToleranceCells);

        for (int y = yMin; y <= yMax; y++)
        {
            for (int dx = -_radiusCells; dx <= _radiusCells; dx++)
            {
                for (int dz = -_radiusCells; dz <= _radiusCells; dz++)
                {
                    var cell = new Vector3Int(center.x + dx, y, center.z + dz);
                    if (IsInScanRange(center, cell))
                        buffer.Add(cell);
                }
            }
        }
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

        if (_floorLootHost is { IsActive: true, Container: not null })
            _detectedContainersScratch.Add(_floorLootHost.Container);

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

                if (candidate.InstanceId == FloorLootHost.DefaultInstanceId)
                    continue;

                _detectedContainersScratch.Add(candidate);
            }
        }

        AppendFloorNestedToDetected();

        _lootProximity?.NotifyDetectedContainers(_detectedContainersScratch);
    }

    void AppendFloorNestedToDetected()
    {
        InventoryContainer floor = _floorLootHost is { IsActive: true } ? _floorLootHost.Container : null;
        if (floor == null)
            return;

        IReadOnlyList<ItemStack> stacks = floor.Stacks;
        for (int i = 0; i < stacks.Count; i++)
        {
            ItemStack stack = stacks[i];
            InventoryContainer nested = stack?.Nested;
            if (nested == null)
                continue;

            string nestedId = nested.InstanceId;
            if (string.IsNullOrEmpty(nestedId) || !_managedWorldContainerIds.Contains(nestedId))
                continue;

            if (ContainsDetectedContainer(nestedId))
                continue;

            _detectedContainersScratch.Add(nested);
        }
    }

    bool ContainsDetectedContainer(string instanceId)
    {
        for (int i = 0; i < _detectedContainersScratch.Count; i++)
        {
            InventoryContainer candidate = _detectedContainersScratch[i];
            if (candidate != null && candidate.InstanceId == instanceId)
                return true;
        }

        return false;
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
