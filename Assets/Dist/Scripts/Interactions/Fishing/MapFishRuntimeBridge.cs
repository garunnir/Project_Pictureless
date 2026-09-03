// ============================================================
// MapFishRuntimeBridge — DistScript에서 MapFishService 훅·카탈로그 배선
// ============================================================

using Garunnir.Runtime.Gameplay.Data;
using IsoTilemap;
using UnityEngine;

static class MapFishRuntimeBridge
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void WireHooks()
    {
        MapFishService.Configure(new MapFishRuntimeHooks
        {
            IsMoodBlocked = () => MoodGameplayGate.IsBlocked,
            OwnsInBodyOrWield = PlayerItemAccess.OwnsInBodyOrWield,
            FishBlockedLabel = () => ItemContextMenuLabels.FishBlocked,
            RollCatchItemId = RollCatchItemId,
            GrantItem = GrantItem,
            TryResolveActorCell = MapPlantService.TryResolveActorCell,
            TryTakeFromStack = PlayerItemAccess.TryTakeOne
        });

        MapPlantHost.AfterLoaded -= OnMapLoaded;
        MapPlantHost.AfterLoaded += OnMapLoaded;
    }

    /// <summary>MapGameplayBootstrap SerializeField 주입. SOData 한곳 SSOT.</summary>
    public static void BindCatalogs(FishingLootCatalog loot, FishWorkClipCatalog workClips)
    {
        FishingLootCatalog.BindRuntime(loot);
        FishWorkClipCatalog.BindRuntime(workClips);

        if (loot == null)
            Debug.LogError("[MapFishRuntimeBridge] FishingLootCatalog is not assigned on MapGameplayBootstrap.");
        if (workClips == null)
            Debug.LogError("[MapFishRuntimeBridge] FishWorkClipCatalog is not assigned on MapGameplayBootstrap.");
    }

    static void OnMapLoaded()
    {
        MapPlantHost plant = MapPlantHost.Runtime;
        MapFishTrapHost host = MapFishTrapHost.EnsureRuntime();
        host.BindMapContext(TileMapCacheHub.Runtime, plant != null ? plant.CellSize : 1f);
        host.CatchUpAll();
    }

    static string RollCatchItemId(ItemData rod)
    {
        FishingLootCatalog catalog = FishingLootCatalog.Runtime;
        if (catalog == null)
            return UnityEngine.Random.value <= 0.65f ? MapFishConsts.DefaultFishItemId : null;

        return catalog.TryRollCatch(rod, out string itemId) ? itemId : null;
    }

    static void GrantItem(string itemId, int count, Vector3 world)
    {
        ItemData item = GameplayData.GetItem(itemId);
        if (item == null || count < 1)
            return;

        var stack = new ItemStack(item, count);
        CharacterGearService gear = PlayerGearHost.Active?.Service;
        if (gear != null && gear.CanDepositToBody(stack))
        {
            gear.DepositToBody(stack);
            return;
        }

        InventoryContainer body = PlayerInventoryRuntime.Active?.Host?.Container;
        if (body != null && body.CapacityPolicy != null && body.CapacityPolicy.CanAccept(body, stack))
        {
            body.AddItem(item, count);
            PlayerInventoryRuntime.Active.Session?.NotifyExternalStacksChanged(body);
            return;
        }

        SmallItemObject prefab = FindSmallItemPrefab();
        if (prefab == null)
        {
            Debug.LogWarning("[MapFishRuntimeBridge] SmallItem prefab missing; floor grant skipped for " + itemId);
            return;
        }

        IWorldGrid grid = null;
        TileMapManager map = Object.FindFirstObjectByType<TileMapManager>();
        if (map != null)
            grid = map.WorldGrid;

        SmallItemSpawner.Spawn(prefab, item, count, world, grid);
    }

    static SmallItemObject FindSmallItemPrefab()
    {
        SmallItemObject[] all = Resources.FindObjectsOfTypeAll<SmallItemObject>();
        for (int i = 0; i < all.Length; i++)
        {
            SmallItemObject obj = all[i];
            if (obj == null)
                continue;
            if (obj.gameObject.scene.IsValid())
                continue;
            return obj;
        }

        return null;
    }
}
