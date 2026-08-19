// ============================================================
// PlayerInventoryRuntime — Session·Detector 소유 (게임플레이 진입점)
// ============================================================

using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public sealed class PlayerInventoryRuntime : MonoBehaviour
{
    [SerializeField] PlayerInventoryHost _host;
    [SerializeField] NearbyContainerDetector _detector;

    readonly LootProximityCoordinator _lootProximity = new();

    InventorySession _session;
    bool _inventoryContextActive;
    readonly HashSet<object> _contextOwners = new();

    public InventorySession Session => _session;
    public PlayerInventoryHost Host => _host;
    public LootProximityCoordinator LootProximity => _lootProximity;
    public bool IsInventoryContextActive => _inventoryContextActive;

    public static PlayerInventoryRuntime Active { get; private set; }
    public static event System.Action<PlayerInventoryRuntime> ActiveChanged;

    public void BindBody(PlayerInventoryHost host, NearbyContainerDetector detector)
    {
        _host = host;
        _detector = detector;
        EnsureReferences();
        if (_session != null && _detector != null)
            _detector.Bind(_session, _lootProximity);
    }

    void Awake()
    {
        EnsureReferences();
        _session = new InventorySession();
        if (_detector != null)
            _detector.Bind(_session, _lootProximity);
    }

    void OnEnable()
    {
        Active = this;
        ActiveChanged?.Invoke(this);
    }

    void OnDisable()
    {
        EndInventoryContext();

        if (Active == this)
        {
            Active = null;
            ActiveChanged?.Invoke(null);
        }
    }

    void OnDestroy()
    {
        EndInventoryContext();
    }

    void OnValidate() => EnsureReferences();
    void Reset() => EnsureReferences();

    void EnsureReferences()
    {
        if (!_host) TryGetComponent(out _host);
        if (!_detector) TryGetComponent(out _detector);
    }

    public void AcquireContext(object owner)
    {
        if (owner == null || _session == null)
            return;

        if (!_contextOwners.Add(owner))
            return;

        if (_contextOwners.Count == 1)
            BeginInventoryContext();
    }

    public void ReleaseContext(object owner)
    {
        if (owner == null)
            return;

        if (!_contextOwners.Remove(owner))
            return;

        if (_contextOwners.Count == 0)
            EndInventoryContext();
    }

    void BeginInventoryContext()
    {
        if (_inventoryContextActive || _session == null)
            return;

        _host.RegisterToSession(_session);
        _session.RefreshNestedContainers();
        _detector.Activate();
        _inventoryContextActive = true;
    }

    void EndInventoryContext()
    {
        _contextOwners.Clear();

        if (!_inventoryContextActive || _session == null)
            return;

        _detector.Deactivate();
        _host.UnregisterFromSession(_session);
        _inventoryContextActive = false;
    }

    public bool TryAddSidebarContainer(InventoryContainer container) =>
        _session != null && _session.TryAddSidebarContainer(container);

    public void RefreshNearbyContainers() => _detector?.RefreshImmediate();

    public bool IsWorldLootContainer(string instanceId)
    {
        if (string.IsNullOrEmpty(instanceId))
            return false;

        return _detector != null && _detector.IsLootContainer(instanceId);
    }

    public bool TryIncludeLootContainer(InventoryContainer container) =>
        _detector != null && _detector.TryIncludeManagedContainer(container);

    public void SeedContainerIfEmpty(InventoryContainer container) =>
        InventoryDemoSeeder.SeedIfEmpty(container);
}
