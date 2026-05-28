// ============================================================
// DebugLogController — Config 디버그 플래그를 적용하고 에디터 디버그 로그를 중계하는 컴포넌트
// ============================================================
using UnityEngine;

public class DebugLogController : MonoBehaviour
{
    [SerializeField] bool isDebugMode = false;
    [SerializeField] bool floorAlgorithm = false;
    [SerializeField] bool tileBfsSceneOverlay = false;
    [SerializeField] bool tileBuildingIdLabels = false;
    [SerializeField] bool tileIndoorOutdoorOverlay = false;
    [SerializeField] bool player = false;
    [SerializeField] bool playerInteraction = false;
    [SerializeField] bool playerMovement = false;
    [SerializeField] bool playerPosUpdate = false;
    [SerializeField] bool tileMapRuntime = false;
    [SerializeField] bool playerSight = false;

    void Start()
    {
        ApplyDebugFlags();
    }

    private void ApplyDebugFlags()
    {
        // Master switch is evaluated at startup only (no runtime hot-apply requirement).
        bool globalEnabled = isDebugMode;
        bool tileRuntimeEnabled = globalEnabled && tileMapRuntime;
        bool playerEnabled = globalEnabled && player;

        Config.DebugMode.TileMapRuntime = tileRuntimeEnabled;
        Config.DebugMode.FloorAlgorithm = tileRuntimeEnabled && floorAlgorithm;
        Config.DebugMode.TileBfsSceneOverlay = tileRuntimeEnabled && tileBfsSceneOverlay;
        Config.DebugMode.TileBuildingIdLabels = tileRuntimeEnabled && tileBuildingIdLabels;
        Config.DebugMode.TileIndoorOutdoorOverlay = tileRuntimeEnabled && tileIndoorOutdoorOverlay;

        Config.DebugMode.Player = playerEnabled;
        Config.DebugMode.PlayerInteraction = playerEnabled && playerInteraction;
        Config.DebugMode.PlayerMovement = playerEnabled && playerMovement;
        Config.DebugMode.PlayerPosUpdate = playerEnabled && playerPosUpdate;
        Config.DebugMode.PlayerSight = playerEnabled && playerSight;
    }

    public static void LogPlayerRun(bool isRun)
    {
        if (!Config.DebugMode.PlayerMovement) return;
        Debug.Log("PlayerMovement: isRun = " + isRun);
    }

    public static void LogPlayerStuck()
    {
        if (!Config.DebugMode.PlayerMovement) return;
        Debug.LogError("PlayerMovement: Stuck!");
    }

    public static void LogPlayerSliding(float lastSlideSqrMagnitude)
    {
        if (!Config.DebugMode.PlayerMovement || lastSlideSqrMagnitude <= 0f) return;
        Debug.Log("PlayerMovement: Sliding");
    }
}
