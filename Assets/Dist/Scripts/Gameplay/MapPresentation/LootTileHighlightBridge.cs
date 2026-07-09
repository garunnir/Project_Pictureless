// ============================================================
// LootTileHighlightBridge — 인벤 루팅 이벤트 → 타일 하이라이트 연결
// ============================================================

using System;
using IsoTilemap;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class LootTileHighlightBridge : MonoBehaviour
{
    [SerializeField] TileMapManager _tileMapManager;

    ITileLootHighlightSink _sink;
    LootProximityCoordinator _subscribedCoordinator;

    void OnEnable()
    {
        _sink = ResolveSink();
        PlayerInventoryRuntime.ActiveChanged += OnActivePlayerChanged;
        TrySubscribeToProximity(PlayerInventoryRuntime.Active);
    }

    void OnDisable()
    {
        PlayerInventoryRuntime.ActiveChanged -= OnActivePlayerChanged;
        UnsubscribeFromProximity();
        _sink?.ClearLootHighlight();
        _sink = null;
    }

    void OnActivePlayerChanged(PlayerInventoryRuntime runtime) =>
        TrySubscribeToProximity(runtime);

    ITileLootHighlightSink ResolveSink() =>
        _tileMapManager != null ? _tileMapManager.PresentationSystem : null;

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
        _sink ??= ResolveSink();
        _sink?.ClearLootHighlight();

        if (container == null)
            return;

        if (!TryResolvePresentationTileId(container, out Guid tileId))
            return;

        _sink?.SetLootHighlight(tileId, true);
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
