using IsoTilemap;
using System.IO;
using UnityEngine;

[DisallowMultipleComponent]
public class MapFileLoader : MonoBehaviour
{
    [Header("Map file")]
    [SerializeField] private string fileName = "map01.json";
    [SerializeField] private bool usePersistentPath = false;

    public IMapModel Model { get; private set; }
    public MapSaveJsonDto LastLoadedDto { get; private set; }

    private IMapSerializer _serializer;
    private IMapModelBuilder _modelBuilder;
    private IMapMapper _mapper;

    void Awake()
    {
        _serializer = new TileMapSerializer();
        _modelBuilder = new TileMapModelBuilder();
        _mapper = new TileMapDtoMapper();
    }

    public void Load()
    {
        // Awake가 호출되지 않은 에디터 환경에서도 동작하도록 lazy 초기화
        _serializer ??= new TileMapSerializer();
        _modelBuilder ??= new TileMapModelBuilder();
        _mapper ??= new TileMapDtoMapper();

        if (GameSaveSlotSession.TryConsumePendingLoad(out int slotIndex))
        {
            Load(GameSaveSlotPaths.MapPath(slotIndex));
            return;
        }

        Load(GetFullPath());
    }

    public void Load(string path)
    {
        Debug.Log($"[MapFileLoader] 로드 시도 경로: {path}");
        var result = new MapLoadPipeline(
            serializer: _serializer,
            modelBuilder: _modelBuilder,
            mapper: _mapper).Load(path);
        Model = result.Model;
        LastLoadedDto = result.Dto;
    }

    public string FileName
    {
        get => fileName;
        set => fileName = value;
    }

    public bool UsePersistentPath
    {
        get => usePersistentPath;
        set => usePersistentPath = value;
    }

    public string ResolveFullPath() => GetFullPath();

    private string GetFullPath()
    {
        if (usePersistentPath)
            return Path.Combine(Application.persistentDataPath, fileName);
        else
            return Path.Combine(Application.dataPath, "..", fileName);
    }

#if UNITY_EDITOR
    [ContextMenu("Load Map In Scene")]
    void LoadMapInSceneFromEditor()
    {
        TileMapManager manager = GetComponent<TileMapManager>();
        if (manager == null)
            manager = GetComponentInParent<TileMapManager>();

        if (manager == null)
        {
            Debug.LogError("[MapFileLoader] Load Map In Scene: TileMapManager를 찾을 수 없습니다.", this);
            return;
        }

        manager.LoadInEditor();
    }
#endif
}
