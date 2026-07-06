// ============================================================
// ContainerInteractable — 월드 상자·냉장고 등 (Interactable + 컨테이너 Provider)
// ============================================================

using Garunnir.Runtime.Gameplay.Item;
using Interactions;
using IsoTilemap;
using UnityEngine;

public sealed class ContainerInteractable : Interactable, IInventoryContainerProvider
{
    const string LogPrefix = "[ContainerInteractable]";

    [SerializeField] ContainerDefinitionSO _definition;
    [SerializeField, Min(0.01f)] float _cellSize = 1f;
    [SerializeField] bool _seedDemoItems = true;

    InventoryContainer _container;
    Vector3Int _gridPosition;

    public InventoryContainer Container => _container;
    public Vector3Int GridPosition => _gridPosition;

    protected override void Awake()
    {
        base.Awake();

        if (_definition == null)
        {
            Debug.LogWarning($"{LogPrefix} ContainerDefinitionSO is not assigned on {name}.", this);
            return;
        }

        _container = InventoryContainer.Create(
            _definition,
            new FixedContainerCapacityPolicy(),
            $"world-{name}-{GetInstanceID()}");
    }

    void Start()
    {
        if (_seedDemoItems)
            InventoryDemoSeeder.SeedIfEmpty(_container);
    }

    void OnEnable()
    {
        RefreshGridPosition();
        if (_container != null)
            ContainerGridRegistry.Instance.Register(this);
    }

    void OnDisable()
    {
        ContainerGridRegistry.Instance.Unregister(this);
    }

    public bool IsAvailableToPlayer(GameObject player) => player != null;

    public override void Interact(GameObject interactor)
    {
        if (UIOverlayRouter.Instance != null)
        {
            UIOverlayRouter.Instance.OpenLootFromInteractable(this);
            return;
        }

        Debug.Log($"{LogPrefix} Interact {displayName} ({_container?.InstanceId}) — UIOverlayRouter missing", this);
    }

    void RefreshGridPosition()
    {
        _gridPosition = TileHelper.ConvertWorldToGrid(transform.position, _cellSize);
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        RefreshGridPosition();
    }
#endif
}
