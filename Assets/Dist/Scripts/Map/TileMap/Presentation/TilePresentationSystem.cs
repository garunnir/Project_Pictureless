// ============================================================
// TilePresentationSystem — 월드 타일 표현 단일 진입점
// ============================================================

using System;
using IsoTilemap;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class TilePresentationSystem : MonoBehaviour
{
    public static TilePresentationSystem Instance { get; private set; }

    TileViewPresentationApplier _applier;
    Guid _activeLootTileId = Guid.Empty;
    LootProximityCoordinator _subscribedCoordinator;

    public void Initialize(TileViewPresentationApplier applier) => _applier = applier;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[TilePresentationSystem] Duplicate instance ignored.", this);
            return;
        }

        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        UnsubscribeFromProximity();
    }

    void OnEnable()
    {
        PlayerInventoryRuntime.ActiveChanged += OnActivePlayerChanged;
        TrySubscribeToProximity(PlayerInventoryRuntime.Active);
    }

    void OnDisable()
    {
        PlayerInventoryRuntime.ActiveChanged -= OnActivePlayerChanged;
        UnsubscribeFromProximity();
        ClearLootContainerHighlight();
    }

    void OnActivePlayerChanged(PlayerInventoryRuntime runtime) =>
        TrySubscribeToProximity(runtime);

    void TrySubscribeToProximity(PlayerInventoryRuntime runtime)
    {
        UnsubscribeFromProximity();

        if (runtime?.LootProximity == null)
            return;

        _subscribedCoordinator = runtime.LootProximity;
        _subscribedCoordinator.ActiveLootContainerChanged += OnActiveLootContainerChanged;
        OnActiveLootContainerChanged(_subscribedCoordinator.ActiveContainer);
    }

    void UnsubscribeFromProximity()
    {
        if (_subscribedCoordinator == null)
            return;

        _subscribedCoordinator.ActiveLootContainerChanged -= OnActiveLootContainerChanged;
        _subscribedCoordinator = null;
    }

    void OnActiveLootContainerChanged(InventoryContainer container)
    {
        ClearLootContainerHighlight();

        if (container == null)
            return;

        if (!TryResolvePresentationTileId(container, out Guid tileId))
            return;

        SetLootContainerHighlight(tileId, true);
    }

    public void SetLootContainerHighlight(Guid presentationTileId, bool highlighted)
    {
        if (_applier == null || presentationTileId == Guid.Empty)
            return;

        if (highlighted)
        {
            if (_activeLootTileId != Guid.Empty && _activeLootTileId != presentationTileId)
                _applier.SetSelected(_activeLootTileId, false);

            _applier.SetSelected(presentationTileId, true);
            _activeLootTileId = presentationTileId;
            return;
        }

        if (_activeLootTileId == presentationTileId)
            _activeLootTileId = Guid.Empty;

        _applier.SetSelected(presentationTileId, false);
    }

    public void ClearLootContainerHighlight()
    {
        if (_applier == null || _activeLootTileId == Guid.Empty)
        {
            _activeLootTileId = Guid.Empty;
            return;
        }

        _applier.SetSelected(_activeLootTileId, false);
        _activeLootTileId = Guid.Empty;
    }

    static bool TryResolvePresentationTileId(InventoryContainer container, out Guid tileId)
    {
        tileId = Guid.Empty;
        if (container == null)
            return false;

        if (ContainerTileViewRegistry.Instance.TryGetPresentationTileId(container.InstanceId, out tileId))
            return tileId != Guid.Empty;

        if (!InventoryContainerRegistry.TryGetProviderByInstanceId(container.InstanceId, out IInventoryContainerProvider provider))
            return false;

        if (provider is ContainerInteractable interactable)
        {
            tileId = interactable.PresentationTileId;
            return tileId != Guid.Empty;
        }

        return false;
    }
}
