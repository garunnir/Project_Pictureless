// ============================================================
// BuildingIdBakeRules — buildingId bake 값·전파 원점 규칙 (SSOT)
// ============================================================
namespace IsoTilemap
{
    public static class BuildingIdBakeRules
    {
        /// <summary>bake 전파·merge·flood 시드 원점. 0·-1은 확장 원점 아님 — 0은 수신만.</summary>
        public static bool CanPropagateBuildingIdFrom(int buildingId) => buildingId > 0;

        /// <summary>다른 양수 building floor — flood 통과·patch 덮어쓰기 차단.</summary>
        public static bool IsConflictingPropagableBuildingId(int floorBuildingId, int propagatingBuildingId) =>
            CanPropagateBuildingIdFrom(floorBuildingId) && floorBuildingId != propagatingBuildingId;

        public static bool ShouldPatchBuildingIdAtOccupiedCell(in TileIdentity id) =>
            TileIdentityUtil.IsStructural(id);

        /// <summary>plaza(-1)는 merge·flood 모두 덮어쓰지 않음.</summary>
        public static bool ShouldOverwriteBuildingIdForPropagation(int existing, int targetBuildingId)
        {
            if (existing == TileIdentity.BuildingIdOutdoor)
                return false;

            if (IsConflictingPropagableBuildingId(existing, targetBuildingId))
                return false;

            return existing != targetBuildingId;
        }
    }
}
