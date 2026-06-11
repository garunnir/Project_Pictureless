using IsoTilemap;
using UnityEngine;

/// <summary>
/// 타일맵 생명주기 조율자.
/// 로드 → Factory / ViewBuilder / Controller / Saver 조립.
/// <see cref="IsoWorldGrid"/>가 그리드 규칙의 단일 출처입니다.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(-50)]
public class TileMapManager : MonoBehaviour
{
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
    [SerializeField] private SightLineProximityBlendDriver _proximityBlendDriver;
    [SerializeField] private CharacterOcclusionDisplayDriver _occlusionDisplayDriver;

    [Header("Floor Visibility (chunk streaming only)")]
    [SerializeField] private PlayerFloorVisibilityDriver _floorVisibilityDriver;

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
    private PlayerFloorVisibilityPolicy _floorPolicy;
    private TileMapCacheHub _mapCacheHub;
    private BuildingGroupBuilder _buildingGroupBuilder;
    private MapCollisionServices _mapCollisionServices;
    private TileMapModel _boundTileModel;

    public IMapModel Model { get; private set; }
    public TileViewPresentationApplier PresentationApplier => _presentationApplier;
    public TilePrefabDB PrefabDB => _prefabDB;
    public IWorldGrid WorldGrid => _worldGrid;

    /// <summary>층 가시성과 동일한 playerFloorCellY (몸 높이 기준).</summary>
    public int ResolvePlayerFloorCellY(float playerHeightWorldY)
    {
        if (_floorPolicy != null)
            return _floorPolicy.ResolvePlayerFloorCellY(playerHeightWorldY);

        return TileHelper.ConvertWorldToGrid(new Vector3(0f, playerHeightWorldY, 0f), _gridCellSize).y;
    }

    private bool UseChunkStreaming => _chunkStreamer != null;

    void Start()
    {
        _loader.Load();
        Model = _loader.Model;

        _worldGrid.ApplyFromMap(_loader.LastLoadedDto, _gridCellSize);
        BindWorldGridToCharacters();

        if (Model is TileMapModel runtimeTileModel)
            SetupMapRuntimeCache(runtimeTileModel);

        Transform tileContainer = new GameObject("TileContainer").transform;
        tileContainer.SetParent(_tileContainer);

        var factory = CreateTileFactory(tileContainer, UseChunkStreaming);
        IMapViewBuilder viewBuilder = CreateViewBuilder(factory, UseChunkStreaming);
        WireTilePresentationApplier();

        _controller.Init(Model, viewBuilder);

        if (UseChunkStreaming && _floorVisibilityDriver != null && _floorPolicy != null && _streamingVisualizer != null)
        {
            _floorVisibilityDriver.Init(_floorPolicy, _streamingVisualizer);
            _floorVisibilityDriver.ApplyNow();
        }

        if (_proximityBlendDriver != null &&
            _presentationApplier != null &&
            _floorPolicy != null &&
            _mapCacheHub != null)
        {
            _proximityBlendDriver.Init(
                _mapCacheHub,
                _presentationApplier,
                _floorPolicy,
                ResolveFloorVisibilityCamera);
        }

        _occlusionDisplayDriver ??= GetComponent<CharacterOcclusionDisplayDriver>();
        if (_occlusionDisplayDriver != null && _presentationApplier != null)
            _occlusionDisplayDriver.Init(_presentationApplier);
        else if (_presentationApplier != null)
        {
            Debug.LogWarning(
                "[TileMapManager] CharacterOcclusionDisplayDriver가 없어 character occlusion display 보간이 비활성화됩니다.");
        }

        _chunkStreamer?.SyncNow();
        _saver.Init(Model, _worldGrid);
        BindMapCollisionServicesOnly();
    }

    private void OnDestroy()
    {
        UnwireTilePresentationApplier();
        _floorVisibilityDriver?.Shutdown();
        _proximityBlendDriver?.Shutdown();
        _occlusionDisplayDriver?.Shutdown();
    }

    private void WireTilePresentationApplier()
    {
        UnwireTilePresentationApplier();

        if (Model is not TileMapModel tileModel)
            return;

        ITileViewRegistry registry = UseChunkStreaming
            ? _streamingVisualizer
            : _nonStreamingVisualizer;

        if (registry == null)
            return;

        _presentationApplier = new TileViewPresentationApplier(registry, tileModel);
        tileModel.OnTileOcclusionPresentationDelta += _presentationApplier.ApplyOcclusionDelta;
        if (_floorPolicy != null && _mapCacheHub != null)
        {
            _presentationApplier.ConfigureSightLinePresentation(
                _mapCacheHub.Buildings.Registry,
                _floorPolicy.MinCellY);
            _presentationApplier.ConfigureFloorVisibility(_floorPolicy);
        }

        _streamingVisualizer?.SetPresentationApplier(_presentationApplier);
        _nonStreamingVisualizer?.SetPresentationApplier(_presentationApplier);
    }

    private void UnwireTilePresentationApplier()
    {
        if (Model is TileMapModel tileModel && _presentationApplier != null)
            tileModel.OnTileOcclusionPresentationDelta -= _presentationApplier.ApplyOcclusionDelta;

        _presentationApplier = null;
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

    private void BindWorldGridToCharacters()
    {
        var states = FindObjectsByType<CharacterState>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < states.Length; i++)
            states[i].BindWorldGrid(_worldGrid);
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
                tileModel.TilesSnapshot,
                _mapCacheHub,
                _gridCellSize,
                ResolveFloorVisibilityCamera,
                cellEpsilonWorld: 0f);
            _presentationApplier?.ConfigureFloorVisibility(_floorPolicy);
        }
    }

    void BindMapCollisionServicesOnly()
    {
        if (Model is not TileMapModel tileModel)
            return;

        if (_mapCacheHub == null)
            SetupMapRuntimeCache(tileModel);
        if (_mapCacheHub == null)
            return;

        _mapCollisionServices = MapCollisionServices.Create(_mapCacheHub, _gridCellSize);

        var movements = FindObjectsByType<PlayerMovement>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        for (int i = 0; i < movements.Length; i++)
            movements[i].BindMapCollision(_mapCollisionServices);

        var aimControllers = FindObjectsByType<PlayerAimController>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        for (int i = 0; i < aimControllers.Length; i++)
        {
            aimControllers[i].BindMapCollision(_mapCollisionServices.LineCast);
        }

        var raycasters = FindObjectsByType<DirectionalRaycaster>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        for (int i = 0; i < raycasters.Length; i++)
        {
            var state = raycasters[i].GetComponent<CharacterState>();
            if (state == null)
                continue;

            raycasters[i].BindMapCollision(_mapCollisionServices.LineCast, state);
        }
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
        BindWorldGridToCharacters();

        if (Model is TileMapModel runtimeTileModel)
            SetupMapRuntimeCache(runtimeTileModel);

        Transform tileContainer = new GameObject("TileContainer").transform;
        tileContainer.SetParent(_tileContainer);

        var factory = CreateTileFactory(tileContainer, chunkStreaming: false);
        IMapViewBuilder viewBuilder = CreateViewBuilder(factory, chunkStreaming: false);
        WireTilePresentationApplier();
        _controller.Init(Model, viewBuilder);

        BindMapCollisionServicesOnly();

        Debug.Log("[TileMapManager] LoadEditor 완료.");
    }
#endif
}
