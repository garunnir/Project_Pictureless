// ============================================================
// PlayerInventoryRuntime — Session·Detector 소유 (게임플레이 진입점)
// ============================================================

using Sirenix.OdinInspector;
using UnityEngine;

[RequireComponent(typeof(PlayerInventoryHost))]
[RequireComponent(typeof(NearbyContainerDetector))]
public sealed class PlayerInventoryRuntime : MonoBehaviour
{
    [Required, SerializeField] PlayerInventoryHost _host;
    [Required, SerializeField] NearbyContainerDetector _detector;

    readonly LootProximityCoordinator _lootProximity = new();

    InventorySession _session;
    bool _inventoryContextActive;

    public InventorySession Session => _session;
    public PlayerInventoryHost Host => _host;
    public LootProximityCoordinator LootProximity => _lootProximity;
    public bool IsInventoryContextActive => _inventoryContextActive;

    public static PlayerInventoryRuntime Active { get; private set; }
    public static event System.Action<PlayerInventoryRuntime> ActiveChanged;

    void Awake()
    {
        EnsureReferences();
        _session = new InventorySession();
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

    public void BeginInventoryContext()
    {
        if (_inventoryContextActive || _session == null)
            return;

        _host.RegisterToSession(_session);
        _session.RefreshNestedContainers();
        _detector.Activate();
        _inventoryContextActive = true;
    }

    public void EndInventoryContext()
    {
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
