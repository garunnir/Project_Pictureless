
public static class Config
{
    public static class DebugMode
    {
        // Default values are boot-time fallbacks.
        // Runtime values are assigned by DebugLogController.Start().
        public static bool Player = false;
        public static bool PlayerInteraction = false;
        public static bool PlayerMovement = false;
        public static bool PlayerSight = false;
        public static bool PlayerPosUpdate = false;

        public static bool TileMapRuntime = true;
        public static bool FloorAlgorithm = true;   // Algorithm logs/diagnostics
        public static bool TileBfsSceneOverlay = true; // SceneView BFS/occlusion lines/legend
        public static bool TileBuildingIdLabels = false; // SceneView per-structural-tile buildingId labels
    }
}
