// ============================================================
// MapPlantService — 심기·수확·시듦·오버레이 타겟 GO 오케스트레이션
// ============================================================
// flowchart LR
//   Clock[WorldClock] --> Bridge[MapClockSnapshot]
//   Load[MapPlantHost.Load] --> CatchUp
//   CatchUp --> Wither[byproducts + remove]
//   Plant[Inventory seed] --> Host[MapPlantHost]
//   Harvest --> Loot[body or SmallItem floor]

using System;
using System.Collections.Generic;
using Garunnir.Runtime.Gameplay.Data;
using IsoTilemap;
using UnityEngine;

public static class MapPlantService
{
    const string NullItemId = "null";

    static readonly Dictionary<Vector3Int, GameObject> Views = new();
    static readonly List<Vector3Int> RemoveScratch = new();
    static MapPlantHost _boundHost;
    static Material _overlayMaterial;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void DomainReset()
    {
        MapPlantHost.RuntimeAssigned -= OnHostAssigned;
        MapPlantHost.AfterLoaded -= CatchUpAll;
        if (_boundHost != null)
            _boundHost.Overlay.Changed -= SyncViews;
        _boundHost = null;
        Views.Clear();
        _overlayMaterial = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Hook()
    {
        RegisterClock();
        MapPlantHost.RuntimeAssigned -= OnHostAssigned;
        MapPlantHost.AfterLoaded -= CatchUpAll;
        MapPlantHost.RuntimeAssigned += OnHostAssigned;
        MapPlantHost.AfterLoaded += CatchUpAll;
    }

    static void RegisterClock()
    {
        MapClockSnapshot.GetDayIndex = () =>
        {
            WorldClock clock = WorldClock.Instance;
            return clock != null ? clock.DayIndex : 0;
        };
        MapClockSnapshot.GetMinuteOfDay = () =>
        {
            WorldClock clock = WorldClock.Instance;
            return clock != null ? clock.MinuteOfDay : 0;
        };
        MapClockSnapshot.SetTime = (day, minute) =>
        {
            WorldClock.Instance?.SetTime(day, minute);
        };
    }

    static void OnHostAssigned(MapPlantHost host)
    {
        if (_boundHost != null)
            _boundHost.Overlay.Changed -= SyncViews;

        _boundHost = host;
        if (host == null)
        {
            ClearViews();
            return;
        }

        host.Overlay.Changed += SyncViews;
        SyncViews();
    }

    public static bool CanPlant(ItemStack stack, InventoryContainer container) =>
        GetPlantBlockedReason(stack, container) == null;

    public static string GetPlantBlockedReason(ItemStack stack, InventoryContainer container)
    {
        if (MoodGameplayGate.IsBlocked)
            return HarvestContextLabels.HarvestBlocked;
        if (stack?.Item?.seed == null || stack.Count < 1)
            return HarvestContextLabels.HarvestBlocked;
        if (!PlayerItemAccess.OwnsInBodyOrWield(stack, container))
            return HarvestContextLabels.HarvestBlocked;

        MapPlantHost host = MapPlantHost.Runtime;
        if (host == null)
            return HarvestContextLabels.HarvestBlocked;
        if (!TryResolvePlayerWorld(out Vector3 world))
            return HarvestContextLabels.HarvestBlocked;

        Vector3Int cell = host.ResolveCellFromWorld(world);
        if (!host.IsPlantable(cell) || host.HasPlant(cell))
            return HarvestContextLabels.HarvestBlocked;

        return null;
    }

    public static bool TryPlant(ItemStack stack, InventoryContainer container)
    {
        if (GetPlantBlockedReason(stack, container) != null)
            return false;

        MapPlantHost host = MapPlantHost.Runtime;
        if (!TryResolvePlayerWorld(out Vector3 world))
            return false;

        Vector3Int cell = host.ResolveCellFromWorld(world);
        string seedId = stack.Item.id;
        int planted = ItemRot.CurrentWorldMinute();

        if (PlayerItemAccess.TryTakeOne(stack, container) <= 0)
            return false;

        if (host.TryAddPlant(cell, seedId, planted))
            return true;

        RefundSeed(seedId, container);
        return false;
    }

    public static string GetHarvestBlockedReason(Vector3Int cell)
    {
        CatchUpCell(cell);

        MapPlantHost host = MapPlantHost.Runtime;
        if (host == null || !host.TryGetPlant(cell, out PlantCell plant))
            return HarvestContextLabels.HarvestBlocked;

        PlantGrowthStage stage = ResolveStage(plant);
        if (PlantGrowth.IsHarvestable(stage))
            return null;

        return HarvestContextLabels.HarvestNotReady;
    }

    public static bool HasDigQuality(ItemData item)
    {
        if (item?.qualities == null)
            return false;

        for (int i = 0; i < item.qualities.Count; i++)
        {
            QualityEntry quality = item.qualities[i];
            if (quality == null || string.IsNullOrEmpty(quality.id))
                continue;
            if (!quality.id.Equals(MapPlantConsts.DigQualityId, StringComparison.OrdinalIgnoreCase))
                continue;
            if (quality.level >= MapPlantConsts.MinDigQualityLevel)
                return true;
        }

        return false;
    }

    public static bool IsFertilizerItem(ItemData item)
    {
        if (item?.flags == null)
            return false;

        for (int i = 0; i < item.flags.Count; i++)
        {
            string flag = item.flags[i];
            if (!string.IsNullOrEmpty(flag) &&
                flag.Equals(MapPlantConsts.FertilizerFlag, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    public static bool CanTill(ItemStack stack, InventoryContainer container) =>
        GetTillBlockedReason(stack, container) == null;

    public static string GetTillBlockedReason(ItemStack stack, InventoryContainer container)
    {
        if (MoodGameplayGate.IsBlocked)
            return HarvestContextLabels.TillBlocked;
        if (!HasDigQuality(stack?.Item) || !PlayerItemAccess.OwnsInBodyOrWield(stack, container))
            return HarvestContextLabels.TillBlocked;
        if (!TryResolvePlayerCell(out Vector3Int cell))
            return HarvestContextLabels.TillBlocked;

        MapPlantHost host = MapPlantHost.Runtime;
        if (host == null || !host.IsTillable(cell))
            return HarvestContextLabels.TillBlocked;

        return null;
    }

    public static string GetTillBlockedReason(Vector3Int cell)
    {
        if (MoodGameplayGate.IsBlocked)
            return HarvestContextLabels.TillBlocked;

        MapPlantHost host = MapPlantHost.Runtime;
        if (host == null || !host.IsTillable(cell))
            return HarvestContextLabels.TillBlocked;
        if (!PlayerHasDigTool())
            return HarvestContextLabels.TillBlocked;

        return null;
    }

    public static bool TryTill(ItemStack stack, InventoryContainer container)
    {
        if (GetTillBlockedReason(stack, container) != null)
            return false;
        if (!TryResolvePlayerCell(out Vector3Int cell))
            return false;

        MapPlantHost host = MapPlantHost.Runtime;
        return host != null && host.TryTill(cell);
    }

    public static bool TryTill(Vector3Int cell)
    {
        if (GetTillBlockedReason(cell) != null)
            return false;

        MapPlantHost host = MapPlantHost.Runtime;
        return host != null && host.TryTill(cell);
    }

    public static bool CanFertilize(ItemStack stack, InventoryContainer container) =>
        GetFertilizeBlockedReason(stack, container) == null;

    public static string GetFertilizeBlockedReason(ItemStack stack, InventoryContainer container)
    {
        if (MoodGameplayGate.IsBlocked)
            return HarvestContextLabels.FertilizeBlocked;
        if (stack == null ||
            !IsFertilizerItem(stack.Item) ||
            stack.Count < 1 ||
            !PlayerItemAccess.OwnsInBodyOrWield(stack, container))
            return HarvestContextLabels.FertilizeBlocked;
        if (!TryResolvePlayerCell(out Vector3Int cell))
            return HarvestContextLabels.FertilizeBlocked;

        return GetFertilizePlantBlockedReason(cell);
    }

    public static string GetFertilizeBlockedReason(Vector3Int cell)
    {
        if (MoodGameplayGate.IsBlocked)
            return HarvestContextLabels.FertilizeBlocked;
        if (FindFertilizer(out _, out _) == null)
            return HarvestContextLabels.FertilizeBlocked;

        return GetFertilizePlantBlockedReason(cell);
    }

    public static bool TryFertilize(ItemStack stack, InventoryContainer container)
    {
        if (GetFertilizeBlockedReason(stack, container) != null)
            return false;
        if (!TryResolvePlayerCell(out Vector3Int cell))
            return false;

        MapPlantHost host = MapPlantHost.Runtime;
        if (host == null)
            return false;
        if (PlayerItemAccess.TryTakeOne(stack, container) <= 0)
            return false;
        if (host.TryFertilize(cell))
            return true;

        RefundSeed(stack.Item.id, container);
        return false;
    }

    public static bool TryFertilize(Vector3Int cell)
    {
        if (GetFertilizeBlockedReason(cell) != null)
            return false;

        ItemStack stack = FindFertilizer(out InventoryContainer container, out _);
        if (stack == null)
            return false;

        MapPlantHost host = MapPlantHost.Runtime;
        if (host == null)
            return false;
        if (PlayerItemAccess.TryTakeOne(stack, container) <= 0)
            return false;
        if (host.TryFertilize(cell))
            return true;

        RefundSeed(stack.Item.id, container);
        return false;
    }

    public static bool TryHarvest(Vector3Int cell)
    {
        CatchUpCell(cell);

        MapPlantHost host = MapPlantHost.Runtime;
        if (host == null || !host.TryGetPlant(cell, out PlantCell plant))
            return false;

        PlantGrowthStage stage = ResolveStage(plant);
        if (!PlantGrowth.IsHarvestable(stage))
            return false;

        ItemData seedItem = GameplayData.GetItem(plant.SeedItemId);
        SeedDetailData seed = seedItem?.seed;
        Vector3 world = CellWorld(host, cell);

        if (seed != null && IsGrantedItemId(seed.fruit))
            GrantItem(seed.fruit, 1, world);
        if (seed != null && seed.seeds && IsGrantedItemId(plant.SeedItemId))
            GrantItem(plant.SeedItemId, 1, world);

        return host.TryRemovePlant(cell);
    }

    public static void CatchUpAll()
    {
        MapPlantHost host = MapPlantHost.Runtime;
        if (host == null)
            return;

        IReadOnlyList<PlantCell> plants = host.Overlay.Plants;
        RemoveScratch.Clear();
        for (int i = 0; i < plants.Count; i++)
            RemoveScratch.Add(plants[i].Cell);

        for (int i = 0; i < RemoveScratch.Count; i++)
            CatchUpCell(RemoveScratch[i]);
    }

    public static void CatchUpCell(Vector3Int cell)
    {
        MapPlantHost host = MapPlantHost.Runtime;
        if (host == null || !host.TryGetPlant(cell, out PlantCell plant))
            return;

        if (!PlantGrowth.IsWithered(ResolveStage(plant)))
            return;

        Wither(host, plant);
    }

    static void Wither(MapPlantHost host, PlantCell plant)
    {
        ItemData seedItem = GameplayData.GetItem(plant.SeedItemId);
        SeedDetailData seed = seedItem?.seed;
        Vector3 world = CellWorld(host, plant.Cell);

        if (seed?.byproducts != null)
        {
            for (int i = 0; i < seed.byproducts.Count; i++)
            {
                string id = seed.byproducts[i];
                if (IsGrantedItemId(id))
                    GrantItem(id, 1, world);
            }
        }

        host.TryRemovePlant(plant.Cell);
    }

    static PlantGrowthStage ResolveStage(PlantCell plant)
    {
        ItemData item = GameplayData.GetItem(plant.SeedItemId);
        return PlantGrowth.Resolve(
            item,
            plant.PlantedWorldMinute,
            ItemRot.CurrentWorldMinute(),
            BuildGrowthContext(plant));
    }

    static PlantGrowthContext BuildGrowthContext(PlantCell plant)
    {
        MapPlantHost host = MapPlantHost.Runtime;
        WeatherKind kind = PlayerGearHost.Active != null
            ? PlayerGearHost.Active.WorldWeatherKind
            : WeatherKind.Clear;

        int daysPerYear = WorldClockSettings.DefaultDaysPerYear;
        int daysPerSeason = WorldClockSettings.DefaultDaysPerSeason;
        int minutesPerDay = WorldClockSettings.DefaultMinutesPerDay;
        int currentDay = 0;
        WorldClock clock = WorldClock.Instance;
        if (clock != null)
        {
            currentDay = clock.DayIndex;
            if (clock.Settings != null)
            {
                daysPerYear = clock.Settings.DaysPerYear;
                daysPerSeason = clock.Settings.DaysPerSeason;
                minutesPerDay = clock.Settings.MinutesPerDay;
            }
        }

        int plantedDay = minutesPerDay > 0 ? plant.PlantedWorldMinute / minutesPerDay : 0;
        bool winterSpan = WorldCalendar.SpanIncludesSeason(
            plantedDay,
            currentDay,
            WorldSeason.Winter,
            daysPerYear,
            daysPerSeason);
        bool outdoor = host == null || host.IsOutdoorCell(plant.Cell);
        bool greenhouse = host != null && host.IsGreenhouseCell(plant.Cell);
        bool frostKills = winterSpan && outdoor && !greenhouse;

        return new PlantGrowthContext(plant.Fertilized, WeatherGrowFactor(kind), frostKills);
    }

    static float WeatherGrowFactor(WeatherKind kind)
    {
        switch (kind)
        {
            case WeatherKind.Rain:
                return PlantGrowth.WeatherRainGrowFactor;
            case WeatherKind.Wind:
                return PlantGrowth.WeatherWindGrowFactor;
            default:
                return PlantGrowth.WeatherClearGrowFactor;
        }
    }

    static string GetFertilizePlantBlockedReason(Vector3Int cell)
    {
        CatchUpCell(cell);

        MapPlantHost host = MapPlantHost.Runtime;
        if (host == null || !host.TryGetPlant(cell, out PlantCell plant))
            return HarvestContextLabels.FertilizeBlocked;
        if (plant.Fertilized)
            return HarvestContextLabels.FertilizeBlocked;
        if (PlantGrowth.IsWithered(ResolveStage(plant)))
            return HarvestContextLabels.FertilizeBlocked;

        return null;
    }

    static bool TryResolvePlayerCell(out Vector3Int cell)
    {
        cell = default;
        MapPlantHost host = MapPlantHost.Runtime;
        if (host == null || !TryResolvePlayerWorld(out Vector3 world))
            return false;

        cell = host.ResolveCellFromWorld(world);
        return true;
    }

    static bool PlayerHasDigTool()
    {
        if (FindInBodyAndWield(HasDigQuality, out _, out _) != null)
            return true;
        return false;
    }

    static ItemStack FindFertilizer(out InventoryContainer container, out bool fromWield)
    {
        return FindInBodyAndWield(IsFertilizerItem, out container, out fromWield);
    }

    static ItemStack FindInBodyAndWield(
        Func<ItemData, bool> match,
        out InventoryContainer container,
        out bool fromWield)
    {
        container = null;
        fromWield = false;
        if (match == null)
            return null;

        InventoryContainer body = PlayerInventoryRuntime.Active?.Host?.Container;
        if (body?.Stacks != null)
        {
            IReadOnlyList<ItemStack> stacks = body.Stacks;
            for (int i = 0; i < stacks.Count; i++)
            {
                ItemStack stack = stacks[i];
                if (stack?.Item == null || stack.Count < 1 || !match(stack.Item))
                    continue;
                container = body;
                return stack;
            }
        }

        WieldSlots wield = PlayerGearHost.Active?.Service?.Wield;
        if (wield == null)
            return null;

        if (wield.Left?.Item != null && wield.Left.Count >= 1 && match(wield.Left.Item))
        {
            fromWield = true;
            return wield.Left;
        }

        if (wield.Right?.Item != null &&
            wield.Right != wield.Left &&
            wield.Right.Count >= 1 &&
            match(wield.Right.Item))
        {
            fromWield = true;
            return wield.Right;
        }

        return null;
    }

    static void SyncViews()
    {
        MapPlantHost host = MapPlantHost.Runtime;
        if (host == null)
        {
            ClearViews();
            return;
        }

        IReadOnlyList<PlantCell> plants = host.Overlay.Plants;
        var desired = new HashSet<Vector3Int>();
        for (int i = 0; i < plants.Count; i++)
        {
            PlantCell plant = plants[i];
            desired.Add(plant.Cell);
            if (!Views.ContainsKey(plant.Cell))
                Views[plant.Cell] = CreateView(host, plant);
        }

        RemoveScratch.Clear();
        foreach (KeyValuePair<Vector3Int, GameObject> pair in Views)
        {
            if (!desired.Contains(pair.Key))
                RemoveScratch.Add(pair.Key);
        }

        for (int i = 0; i < RemoveScratch.Count; i++)
        {
            Vector3Int cell = RemoveScratch[i];
            if (Views.TryGetValue(cell, out GameObject go) && go != null)
                UnityEngine.Object.Destroy(go);
            Views.Remove(cell);
        }
    }

    static void ClearViews()
    {
        foreach (KeyValuePair<Vector3Int, GameObject> pair in Views)
        {
            if (pair.Value != null)
                UnityEngine.Object.Destroy(pair.Value);
        }

        Views.Clear();
    }

    static GameObject CreateView(MapPlantHost host, PlantCell plant)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "Plant_" + plant.Cell.x + "_" + plant.Cell.y + "_" + plant.Cell.z;
        go.transform.SetParent(host.transform, true);
        go.transform.position = CellWorld(host, plant.Cell);
        go.transform.localScale = new Vector3(
            MapPlantConsts.OverlayScale,
            MapPlantConsts.OverlayHeight,
            MapPlantConsts.OverlayScale);

        if (go.TryGetComponent(out Collider col))
            col.isTrigger = true;

        if (go.TryGetComponent(out MeshRenderer renderer))
        {
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            Material mat = OverlayMaterial();
            if (mat != null)
                renderer.sharedMaterial = mat;
        }

        var interactable = go.AddComponent<MapPlantInteractable>();
        interactable.BindCell(plant.Cell);
        go.AddComponent<TileObjectInteractionTarget>();
        return go;
    }

    static Material OverlayMaterial()
    {
        if (_overlayMaterial != null)
            return _overlayMaterial;

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            shader = Shader.Find("Unlit/Color");
        if (shader == null)
            return null;

        _overlayMaterial = new Material(shader) { name = "MapPlantOverlay" };
        var color = new Color(0.22f, 0.55f, 0.18f, 1f);
        if (_overlayMaterial.HasProperty("_BaseColor"))
            _overlayMaterial.SetColor("_BaseColor", color);
        else if (_overlayMaterial.HasProperty("_Color"))
            _overlayMaterial.SetColor("_Color", color);
        return _overlayMaterial;
    }

    static Vector3 CellWorld(MapPlantHost host, Vector3Int cell)
    {
        Vector3 pos = TileHelper.ConvertGridToWorldPos(cell, host.CellSize);
        pos.y += MapPlantConsts.OverlayYOffset;
        return pos;
    }

    static bool TryResolvePlayerWorld(out Vector3 world)
    {
        world = default;
        PlayerGearHost gear = PlayerGearHost.Active;
        if (gear != null && gear.TryGetComponent(out CharacterState state))
        {
            world = state.BodyWorldPoint.sqrMagnitude > 1e-6f
                ? state.BodyWorldPoint
                : gear.transform.position;
            return true;
        }

        PlayerInventoryRuntime runtime = PlayerInventoryRuntime.Active;
        if (runtime?.Host == null)
            return false;

        world = runtime.Host.WorldPosition;
        return true;
    }

    static void RefundSeed(string seedId, InventoryContainer container)
    {
        ItemData item = GameplayData.GetItem(seedId);
        if (item == null)
            return;

        if (container != null)
        {
            container.AddItem(item, 1);
            PlayerInventoryRuntime.Active?.Session?.NotifyExternalStacksChanged(container);
            return;
        }

        GrantItem(seedId, 1, Vector3.zero);
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

        SpawnFloor(item, count, world);
    }

    static void SpawnFloor(ItemData item, int count, Vector3 world)
    {
        SmallItemObject prefab = FindSmallItemPrefab();
        if (prefab == null)
        {
            Debug.LogWarning("[MapPlantService] SmallItem prefab missing; floor grant skipped for " + item.id);
            return;
        }

        IWorldGrid grid = null;
        TileMapManager map = UnityEngine.Object.FindFirstObjectByType<TileMapManager>();
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

        return UnityEngine.Object.FindFirstObjectByType<SmallItemObject>(FindObjectsInactive.Include);
    }

    static bool IsGrantedItemId(string id) =>
        !string.IsNullOrEmpty(id) &&
        !id.Equals(NullItemId, StringComparison.OrdinalIgnoreCase);
}
