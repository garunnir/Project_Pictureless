// ============================================================
// ComponentBakeRules — component bake 단계 규칙 (buildingId 할당 전)
// ============================================================
namespace IsoTilemap
{
    public static class ComponentBakeRules
    {
        public static bool CanPropagateComponentFrom(int componentRoot) => componentRoot > 0;

        public static bool IsConflictingComponentRoot(int floorComponentRoot, int propagatingRoot) =>
            CanPropagateComponentFrom(floorComponentRoot) && floorComponentRoot != propagatingRoot;

        /// <summary>incident 타일 <see cref="TileIdentity.buildingId"/>가 outdoor(-1)이면 flood traverse·tag·seed 제외.</summary>
        public static bool ShouldBlockComponentFloodFromIncidentTile(in TileIdentity id) =>
            id.buildingId == TileIdentity.BuildingIdOutdoor;

        public static bool ShouldOverwriteComponentForPropagation(int existingRoot, int propagatingRoot) =>
            !IsConflictingComponentRoot(existingRoot, propagatingRoot);
    }
}

