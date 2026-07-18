// ============================================================
// ContainerInteractable — 월드 컨테이너 provider (액션은 Catalog)
// ============================================================

using System;
using System.Security.Cryptography;
using System.Text;
using Garunnir.Runtime.Gameplay.Data;
using IsoTilemap;
using UnityEngine;

[RequireComponent(typeof(ContainerTileViewRegistrar))]
[RequireComponent(typeof(TileObjectInteractionTarget))]
public sealed class ContainerInteractable : MonoBehaviour, IInventoryContainerProvider
{
    const string LogPrefix = "[ContainerInteractable]";

    [SerializeField] string _containerDefId = "crate";
    [SerializeField] string _containerId;
    [SerializeField] bool _seedDemoItems = true;
    [SerializeField] TileView _tileView;
    [SerializeField] Guid _presentationTileId;

    InventoryContainer _container;
    IWorldGrid _worldGrid;
    string _runtimeContainerId;

    public string ContainerId =>
        string.IsNullOrWhiteSpace(_runtimeContainerId) ? _containerId : _runtimeContainerId;

    public InventoryContainer Container => _container;
    public Vector3 WorldPosition => transform.position;
    public Vector3Int GridPosition => ResolveGridPosition();
    public TileView TileView => _tileView;
    public Guid PresentationTileId => ResolvePresentationTileId();

    public string OpenLootActionLabel => InteractionLabels.OpenContainer;

    void Awake()
    {
        ContainerData containerDef = GameplayData.GetContainer(_containerDefId);
        if (containerDef == null)
        {
            Debug.LogWarning($"{LogPrefix} Container definition '{_containerDefId}' not found on {name}.", this);
            return;
        }

        if (string.IsNullOrWhiteSpace(_containerId))
            _containerId = $"world-{name}";

        _runtimeContainerId = ResolveRuntimeContainerId(_containerId);

        _container = InventoryContainer.Create(
            containerDef,
            new FixedContainerCapacityPolicy(),
            _runtimeContainerId);

        if (_presentationTileId == Guid.Empty)
            _presentationTileId = CreateDeterministicTileId(_runtimeContainerId);

        if (_tileView == null)
            _tileView = GetComponentInChildren<TileView>(true);

        EnsureWorldGrid();
        SyncTileViewGrid();

        TileObjectInteractionTarget target = GetComponent<TileObjectInteractionTarget>();
        target?.BindTileView(_tileView);
    }

    void Start()
    {
        if (_seedDemoItems)
            InventoryDemoSeeder.SeedIfEmpty(_container);
    }

    void OnEnable()
    {
        EnsureWorldGrid();
        SyncTileViewGrid();

        if (_container != null)
            InventoryContainerRegistry.Register(this);
    }

    void OnDisable() => InventoryContainerRegistry.Unregister(this);

    public bool IsAvailableToPlayer(GameObject player) => player != null;

    public void BindWorldGrid(IWorldGrid worldGrid)
    {
        _worldGrid = worldGrid;
        SyncTileViewGrid();
    }

    public void BindTileView(TileView tileView)
    {
        _tileView = tileView;
        SyncTileViewGrid();
    }

    public void OpenLoot()
    {
        if (UIOverlayRouter.Instance != null)
        {
            UIOverlayRouter.Instance.OpenLootFromInteractable(this);
            return;
        }

        Debug.Log($"{LogPrefix} OpenLoot ({_container?.InstanceId}) — UIOverlayRouter missing", this);
    }

    Guid ResolvePresentationTileId()
    {
        if (_presentationTileId != Guid.Empty)
            return _presentationTileId;

        string id = ContainerId;
        if (string.IsNullOrWhiteSpace(id))
            return Guid.Empty;

        _presentationTileId = CreateDeterministicTileId(id);
        return _presentationTileId;
    }

    Vector3Int ResolveGridPosition()
    {
        EnsureWorldGrid();
        return _worldGrid != null
            ? _worldGrid.WorldToCell(transform.position)
            : TileHelper.ConvertWorldToGrid(transform.position, 1f);
    }

    void SyncTileViewGrid()
    {
        if (_tileView != null)
            _tileView.gridPos = ResolveGridPosition();
    }

    void EnsureWorldGrid()
    {
        if (_worldGrid != null)
            return;
    }

    string ResolveRuntimeContainerId(string templateId)
    {
        if (!Application.isPlaying)
            return templateId;

        if (!HasDuplicateTemplateId(templateId))
            return templateId;

        string unique = $"{templateId}-{GetInstanceID():x}";
        Debug.LogWarning(
            $"{LogPrefix} Duplicate template containerId '{templateId}' on {name}. Runtime id changed to '{unique}'.",
            this);
        return unique;
    }

    bool HasDuplicateTemplateId(string templateId)
    {
        ContainerInteractable[] all = FindObjectsByType<ContainerInteractable>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < all.Length; i++)
        {
            ContainerInteractable other = all[i];
            if (other == null || other == this)
                continue;

            if (string.Equals(other._containerId, templateId, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    public static Guid CreateDeterministicTileId(string containerId)
    {
        if (string.IsNullOrWhiteSpace(containerId))
            return Guid.Empty;

        using var md5 = MD5.Create();
        byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes($"container-presentation:{containerId}"));
        return new Guid(hash);
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(_containerId))
            _containerId = $"world-{name}";

        SyncTileViewGrid();
    }
#endif
}
