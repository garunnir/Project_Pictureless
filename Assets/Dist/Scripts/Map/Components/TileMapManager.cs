using IsoTilemap;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// 타일맵 생명주기 조율자.
/// 로드 → Factory / ViewBuilder / Controller / Saver 조립.
/// <see cref="IsoWorldGrid"/>가 그리드 규칙의 단일 출처입니다.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(-50)]
[RequireComponent(typeof(TilePresentationSystem))]
[RequireComponent(typeof(CharacterOcclusionDisplayDriver))]
public class TileMapManager : MonoBehaviour
{
    [Header("Map blood overlay")]
    [SerializeField] private MapBloodHost _bloodHost;

    [Header("Map plant overlay")]
    [SerializeField] private MapPlantHost _plantHost;

    [Header("로드 → 컨트롤러/세이버 초기화 → 저장 흐름을 책임집니다.")]
    [SerializeField] private MapFileLoader _loader;
    [SerializeField] private MapFileSaver _saver;
    [SerializeField] private TileMapController _controller;
    [SerializeField] private Transform _tileContainer;

    [Header("Prefab DB")]
    [SerializeField] private TilePrefabDB _prefabDB;

    [Header("Grid")]
    [SerializeField] private float _gridCellSize = 1f;

    [Header("Chunk Streaming")]
    [Tooltip("연결 시 청크 스트리밍 경로를 사용합니다. 청크·카메라 설정은 TileMapChunkStreamer에 있습니다.")]
    [SerializeField] private TileMapChunkStreamer _chunkStreamer;

    [Header("Tile Visibility and Presentation")]
    [Tooltip("비우면 ChunkStreamer 카메라 → Camera.main 순으로 사용합니다.")]
    [SerializeField] private Camera _visibilityCamera;
    [SerializeField] private MonoBehaviour _proximityBlendDriver;
    [SerializeField] private CharacterOcclusionDisplayDriver _occlusionDisplayDriver;
    [Tooltip("컨테이너 TileView registry bridge. 비우면 맵 타일만 조회합니다.")]
    [SerializeField] private MonoBehaviour _externalTileViewRegistry;

    [Header("Floor Visibility (chunk streaming only)")]
    [SerializeField] private MonoBehaviour _floorVisibilityDriver;
    [FormerlySerializedAs("_floorHidePresentationMode")]
    [SerializeField] private StructuralHidePresentationMode _structuralHidePresentationMode =
        StructuralHidePresentationMode.DisableGameObject;

    [Header("Character Sight Fade")]
    [Tooltip("possessed 시야 반경 밖 NPC 메시 페이드. IMapSightFadeDriver 구현체. 비우면 Find로 탐색.")]
    [SerializeField] private MonoBehaviour _characterSightFadeDriver;

    [Header("Tile Pooling (chunk streaming only)")]
    [SerializeField] private bool _enableTilePooling = true;
    [SerializeField, Min(0)] private int _maxPooledInstances = 2000;
    [SerializeField, Min(0f)] private float _maxPoolMemoryMb;
    [SerializeField, Min(1024)] private int _estimatedBytesPerTile = 65536;
    [SerializeField, Range(0f, 0.5f)] private float _poolReserveRatio = 0.15f;
    [SerializeField, Min(0)] private int _minPoolPerPrefab = 1;
    [SerializeField, Min(1)] private int _maxPoolPerPrefab = 256;
    [Tooltip("0이면 맵·스트리밍 설정으로 자동 추정합니다.")]
    [SerializeField, Min(0)] private int _streamingPeakOverride;

    private readonly IsoWorldGrid _worldGrid = new();

    private TileMapStreamingVisualizer _streamingVisualizer;
    private TileMapVisualizer _nonStreamingVisualizer;
    private TileViewPresentationApplier _presentationApplier;
    private TilePresentationSystem _presentationSystem;
    private PlayerFloorVisibilityPolicy _floorPolicy;
    private TileMapCacheHub _mapCacheHub;
    private BuildingGroupBuilder _buildingGroupBuilder;
    private MapCollisionServices _mapCollisionServices;
    private TileMapModel _boundTileModel;

    public IMapModel Model { get; private set; }
    public TileViewPresentationApplier PresentationApplier => _presentationApplier;
    public TilePresentationSystem PresentationSystem => _presentationSystem;
    public MapCollisionServices MapCollisionServices => _mapCollisionServices;
    public TilePrefabDB PrefabDB => _prefabDB;
    public IWorldGrid WorldGrid => _worldGrid;

    /// <summary>층 가시성과 동일한 playerFloorCellY (몸 위치 기준 점유셀).</summary>
    public int ResolvePlayerFloorCellY(Vector3 playerWorld)
    {
        if (_floorPolicy != null)
            return _floorPolicy.ResolvePlayerFloorCellY(playerWorld.y, playerWorld);

        return TileHelper.ConvertWorldToGrid(playerWorld, _gridCellSize).y;
    }

    public int ResolvePlayerFloorCellY(float playerHeightWorldY) =>
        ResolvePlayerFloorCellY(new Vector3(0f, playerHeightWorldY, 0f));

    /// <summary>층 가시성 정책 컨텍스트 (CharacterVisibilityBroadcaster 등).</summary>
    public bool TryResolveFloorVisibilityContext(Vector3 playerWorld, out FloorVisibilityContext ctx)
    {
        if (_floorPolicy == null)
        {
            ctx = default;
            return false;
        }

        ctx = _floorPolicy.ResolveContext(playerWorld.y, playerWorld);
        return true;
    }

    /// <summary>
    /// 월드 발밑 점유셀의 Floor(없으면 임의 타일)가 structural show인지.
    /// 타일 없으면 false. 정책 미초기화면 true(페이드는 거리·LOS만).
    /// </summary>
    public bool IsWorldStructurallyVisible(Vector3 world, in FloorVisibilityContext ctx)
    {
        if (_floorPolicy == null || _mapCacheHub == null)
            return true;

        float cellSize = _worldGrid != null ? _worldGrid.CellSize : _gridCellSize;
        Vector3Int cell = OccupiedCellCoord.ResolveFromWorld(_mapCacheHub, world, cellSize, world.y);
        if (!_mapCacheHub.TryGetCellTiles(cell.x, cell.z, cell.y, out var tiles) ||
            tiles == null ||
            tiles.Count == 0)
        {
            return false;
        }

        bool anyFloor = false;
        for (int i = 0; i < tiles.Count; i++)
        {
            TileData tile = tiles[i];
            if (!TileIdentityUtil.IsFloorTile(tile.identity))
                continue;

            anyFloor = true;
            if (_floorPolicy.IsTileVisible(tile, in ctx))
                return true;
        }

        if (anyFloor)
            return false;

        for (int i = 0; i < tiles.Count; i++)
        {
            TileData tile = tiles[i];
            if (_floorPolicy.IsTileVisible(tile, in ctx))
                return true;
        }

        return false;
    }

    /// <summary>
    /// BFS wall occlusion 갱신. policy가 invisible인 타일은 evaluate 단계에서 제외됩니다.
    /// </summary>
    public void UpdateWallOcclusionFromPlayer(
        Vector3 playerWorld,
        int playerFloorCellY,
        OcclusionProximitySettings settings)
    {
        if (Model is not TileMapModel model)
            return;

        System.Func<TileData, bool> visible = null;
        if (_floorPolicy != null)
        {
            FloorVisibilityContext ctx = _floorPolicy.ResolveContext(playerWorld.y, playerWorld);
            visible = tile => _floorPolicy.IsTileVisible(tile, in ctx);
        }

        model.UpdateOcclusionFromPlayerWorld(playerWorld, playerFloorCellY, settings, visible);
    }

    private bool UseChunkStreaming => _chunkStreamer != null;

    void EnsureOcclusionDisplayDriver()
    {
        _occlusionDisplayDriver ??= GetComponent<CharacterOcclusionDisplayDriver>();
        if (_occlusionDisplayDriver == null)
            Debug.LogError("[TileMapManager] CharacterOcclusionDisplayDriver가 씬 오브젝트에 배치되어 있어야 합니다.", this);
    }

    void Start()
    {
        _loader.Load();
        Model = _loader.Model;

        _worldGrid.ApplyFromMap(_loader.LastLoadedDto, _gridCellSize);

        if (Model is TileMapModel runtimeTileModel)
            SetupMapRuntimeCache(runtimeTileModel);

        Transform tileContainer = new GameObject("TileContainer").transform;
        tileContainer.SetParent(_tileContainer);

        var factory = CreateTileFactory(tileContainer, UseChunkStreaming);
        IMapViewBuilder viewBuilder = CreateViewBuilder(factory, UseChunkStreaming);
        WireTilePresentationApplier();

        _controller.Init(Model, viewBuilder);

        if (_proximityBlendDriver is IProximityBlendDriver proximityBlend &&
            _presentationApplier != null &&
            _floorPolicy != null &&
            _mapCacheHub != null)
        {
            proximityBlend.Init(
                _mapCacheHub,
                _presentationApplier,
                _floorPolicy,
                ResolveFloorVisibilityCamera);
        }

        if (_floorVisibilityDriver is IFloorVisibilityDriver floorVisibility && _floorPolicy != null)
        {
            IFloorVisibilitySync sync = UseChunkStreaming
                ? _streamingVisualizer
                : _nonStreamingVisualizer;
            if (sync != null)
            {
                floorVisibility.Init(_floorPolicy, sync);
                floorVisibility.ApplyNow();
            }
        }

        EnsureOcclusionDisplayDriver();
        if (_occlusionDisplayDriver != null && _presentationApplier != null)
            _occlusionDisplayDriver.Init(_presentationApplier);
        else if (_presentationApplier != null)
        {
            Debug.LogWarning(
                "[TileMapManager] CharacterOcclusionDisplayDriver가 없어 character occlusion display 보간이 비활성화됩니다.");
        }

        _chunkStreamer?.SyncNow();
        _saver.Init(Model, _worldGrid);
        SetupMapCollisionServices();
        SetupMapBlood();
        SetupMapPlant();

        if (_characterSightFadeDriver == null)
        {
            MonoBehaviour[] behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IMapSightFadeDriver)
                {
                    _characterSightFadeDriver = behaviours[i];
                    break;
                }
            }
        }

        if (_characterSightFadeDriver is IMapSightFadeDriver sightFade)
            sightFade.Init(this);
    }

    void SetupMapBlood()
    {
        _bloodHost ??= GetComponent<MapBloodHost>();
        if (_bloodHost == null)
            _bloodHost = gameObject.AddComponent<MapBloodHost>();

        float cellSize = _worldGrid != null ? _worldGrid.CellSize : _gridCellSize;
        _bloodHost.BindMapContext(_mapCacheHub, cellSize);
        _bloodHost.LoadFromDto(_loader != null ? _loader.LastLoadedDto : null);
    }

    void SetupMapPlant()
    {
        _plantHost ??= GetComponent<MapPlantHost>();
        if (_plantHost == null)
            _plantHost = gameObject.AddComponent<MapPlantHost>();

        float cellSize = _worldGrid != null ? _worldGrid.CellSize : _gridCellSize;
        _plantHost.BindMapContext(_mapCacheHub, cellSize, _prefabDB, _controller, Model);
        MapClockSnapshot.RestoreFromDto(_loader != null ? _loader.LastLoadedDto : null);
        _plantHost.LoadFromDto(_loader != null ? _loader.LastLoadedDto : null);
    }

    private void OnDestroy()
    {
        UnwireTilePresentationApplier();
        if (_floorVisibilityDriver is IFloorVisibilityDriver floorVisibility)
            floorVisibility.Shutdown();
        if (_proximityBlendDriver is IProximityBlendDriver proximityBlend)
            proximityBlend.Shutdown();
        _occlusionDisplayDriver?.Shutdown();
        if (_characterSightFadeDriver is IMapSightFadeDriver sightFade)
            sightFade.Shutdown();
    }

    private void WireTilePresentationApplier()
    {
        UnwireTilePresentationApplier();

        if (Model is not TileMapModel tileModel)
            return;

        ITileViewRegistry mapRegistry = UseChunkStreaming
            ? _streamingVisualizer
            : _nonStreamingVisualizer;

        if (mapRegistry == null)
            return;

        ITileViewRegistry registry = mapRegistry;
        if (_externalTileViewRegistry is ITileViewRegistry externalRegistry)
            registry = new CompositeTileViewRegistry(mapRegistry, externalRegistry);

        _presentationApplier = new TileViewPresentationApplier(registry, tileModel);
        tileModel.OnTileOcclusionPresentationDelta += _presentationApplier.ApplyOcclusionDelta;
        if (_floorPolicy != null && _mapCacheHub != null)
        {
            _presentationApplier.ConfigureFloorVisibility(
                _floorPolicy,
                _mapCacheHub.Buildings.Registry,
                _mapCacheHub,
                _structuralHidePresentationMode);
        }

        _streamingVisualizer?.SetPresentationApplier(_presentationApplier);
        _nonStreamingVisualizer?.SetPresentationApplier(_presentationApplier);
        EnsurePresentationSystem();
        _presentationSystem?.Initialize(_presentationApplier);
    }

    void EnsurePresentationSystem()
    {
        _presentationSystem ??= GetComponent<TilePresentationSystem>();
        if (_presentationSystem == null)
            Debug.LogError("[TileMapManager] TilePresentationSystem이 씬 오브젝트에 배치되어 있어야 합니다.", this);
    }

    private void UnwireTilePresentationApplier()
    {
        if (Model is TileMapModel tileModel && _presentationApplier != null)
            tileModel.OnTileOcclusionPresentationDelta -= _presentationApplier.ApplyOcclusionDelta;

        _presentationApplier?.ResetFloorVisibilityState();
        _presentationApplier = null;
        _presentationSystem?.ClearLootContainerHighlight();
        _streamingVisualizer?.SetPresentationApplier(null);
        _nonStreamingVisualizer?.SetPresentationApplier(null);
    }

    private Camera ResolveFloorVisibilityCamera()
    {
        if (_visibilityCamera != null)
            return _visibilityCamera;

        if (_chunkStreamer != null)
            return _chunkStreamer.ResolveStreamingCamera();

        return Camera.main;
    }

    void SetupMapCollisionServices()
    {
        if (Model is not TileMapModel tileModel)
            return;

        if (_mapCacheHub == null)
            SetupMapRuntimeCache(tileModel);
        if (_mapCacheHub == null)
            return;

        _mapCollisionServices = MapCollisionServices.Create(_mapCacheHub, _gridCellSize);
    }

    private TileObjFactory CreateTileFactory(Transform tileContainer, bool chunkStreaming)
    {
        TileViewPoolRegistry pool = null;
        if (chunkStreaming && _enableTilePooling && _loader.LastLoadedDto != null)
        {
            var poolSettings = new TilePoolSettings(
                _maxPooledInstances,
                _maxPoolMemoryMb,
                _estimatedBytesPerTile,
                _poolReserveRatio,
                _minPoolPerPrefab,
                _maxPoolPerPrefab,
                _streamingPeakOverride);

            var streamEstimate = _chunkStreamer.CreatePoolStreamEstimate(_worldGrid);

            var caps = TilePoolBudgetBuilder.Build(
                _loader.LastLoadedDto,
                poolSettings,
                streamEstimate);

            pool = new TileViewPoolRegistry(tileContainer, _prefabDB);
            foreach (var kv in caps)
                pool.RegisterCap(kv.Key, kv.Value);
        }

        return new TileObjFactory(tileContainer, _prefabDB, pool);
    }

    void SetupMapRuntimeCache(TileMapModel tileModel)
    {
        if (tileModel == null)
        {
            _floorPolicy = null;
            _mapCacheHub = null;
            _buildingGroupBuilder = null;
            _boundTileModel = null;
            return;
        }

        bool hubCreated = _mapCacheHub == null || !ReferenceEquals(_boundTileModel, tileModel);
        if (hubCreated)
        {
            _boundTileModel = tileModel;
            _floorPolicy = null;

            var registry = new BuildingGroupRegistry();
            _mapCacheHub = TileMapCacheHub.Create(tileModel, registry);
            tileModel.SetMapCacheHub(_mapCacheHub);
            _buildingGroupBuilder = new BuildingGroupBuilder(tileModel, _mapCacheHub);
            _mapCacheHub.BindRoomBakeBuilder(_buildingGroupBuilder);
            tileModel.SetBuildingGroupBuilder(_buildingGroupBuilder);
            _buildingGroupBuilder.AssignAll();
        }

        if (_floorPolicy == null)
        {
            _floorPolicy = PlayerFloorVisibilityPolicy.Build(
                _mapCacheHub,
                _gridCellSize,
                _mapCacheHub.Buildings.Registry,
                cellEpsilonWorld: 0f);
            _presentationApplier?.ConfigureFloorVisibility(
                _floorPolicy,
                _mapCacheHub.Buildings.Registry,
                _mapCacheHub,
                _structuralHidePresentationMode);
        }
    }

    private IMapViewBuilder CreateViewBuilder(TileObjFactory factory, bool chunkStreaming)
    {
        if (!chunkStreaming)
        {
            _streamingVisualizer = null;
            _nonStreamingVisualizer = new TileMapVisualizer(factory, _worldGrid);
            return _nonStreamingVisualizer;
        }

        _nonStreamingVisualizer = null;

        _streamingVisualizer = new TileMapStreamingVisualizer(
            factory, _worldGrid, _chunkStreamer.ChunkSize);
        _chunkStreamer.Attach(_streamingVisualizer, _worldGrid);
        return _streamingVisualizer;
    }

    public void Load() => _loader.Load();
    public void Save() => _saver.Save();

#if UNITY_EDITOR
    [ContextMenu("Load Editor")]
    private void LoadEditor()
    {
        if (_tileContainer.childCount > 0)
            DestroyImmediate(_tileContainer.GetChild(0).gameObject);

        _chunkStreamer?.Shutdown();

        _loader.Load();
        Model = _loader.Model;

        if (Model == null)
        {
            Debug.LogError("[TileMapManager] LoadEditor: 맵 로드 실패 — 파일 경로나 JSON을 확인하세요.");
            return;
        }

        _worldGrid.ApplyFromMap(_loader.LastLoadedDto, _gridCellSize);

        if (Model is TileMapModel runtimeTileModel)
            SetupMapRuntimeCache(runtimeTileModel);

        Transform tileContainer = new GameObject("TileContainer").transform;
        tileContainer.SetParent(_tileContainer);

        var factory = CreateTileFactory(tileContainer, chunkStreaming: false);
        IMapViewBuilder viewBuilder = CreateViewBuilder(factory, chunkStreaming: false);
        WireTilePresentationApplier();
        _controller.Init(Model, viewBuilder);

        SetupMapCollisionServices();

        Debug.Log("[TileMapManager] LoadEditor 완료.");
    }
#endif
}
