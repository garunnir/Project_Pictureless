// ============================================================
// NearbyContainerDetector — GridPos 주변 컨테이너 감지 → Session 사이드바 증감
// ============================================================

using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[RequireComponent(typeof(CharacterState))]
public sealed class NearbyContainerDetector : MonoBehaviour
{
    [Required, SerializeField] CharacterState _characterState;
    [SerializeField, Min(0)] int _radiusCells = 2;
    [SerializeField] bool _sameFloorOnly = true;
    [SerializeField, Min(0)] int _verticalToleranceCells = 0;

    readonly List<IInventoryContainerProvider> _scanResults = new();
    readonly HashSet<string> _managedWorldContainerIds = new();

    InventorySession _session;
    bool _isActive;

    public void Bind(InventorySession session) => _session = session;

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

    public void RefreshImmediate() => Refresh(_characterState.GridPos);

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
        if (_session == null)
            return;

        ContainerGridRegistry.Instance.CollectAround(
            center,
            _radiusCells,
            _scanResults,
            _sameFloorOnly,
            _verticalToleranceCells);

        var desiredIds = new HashSet<string>();
        GameObject player = _characterState.gameObject;

        for (int i = 0; i < _scanResults.Count; i++)
        {
            IInventoryContainerProvider provider = _scanResults[i];
            if (provider?.Container == null)
                continue;

            if (!provider.IsAvailableToPlayer(player))
                continue;

            string instanceId = provider.Container.InstanceId;
            desiredIds.Add(instanceId);
            _session.TryAddSidebarContainer(provider.Container);
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
    }

    void RemoveManagedContainers()
    {
        if (_session == null)
        {
            _managedWorldContainerIds.Clear();
            return;
        }

        foreach (string instanceId in _managedWorldContainerIds)
            _session.TryRemoveSidebarContainer(instanceId);

        _managedWorldContainerIds.Clear();
    }
}
