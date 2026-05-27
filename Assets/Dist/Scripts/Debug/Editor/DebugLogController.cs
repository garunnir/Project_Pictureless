using UnityEngine;

public class DebugLogController : MonoBehaviour
{
    [SerializeField] bool isDebugMode = false;
    [SerializeField] bool floorAlgorithm = false;
    [SerializeField] bool tileBfsSceneOverlay = false;
    [SerializeField] bool tileBuildingIdLabels = false;
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

        Config.DebugMode.Player = playerEnabled;
        Config.DebugMode.PlayerInteraction = playerEnabled && playerInteraction;
        Config.DebugMode.PlayerMovement = playerEnabled && playerMovement;
        Config.DebugMode.PlayerPosUpdate = playerEnabled && playerPosUpdate;
        Config.DebugMode.PlayerSight = playerEnabled && playerSight;
    }
}
