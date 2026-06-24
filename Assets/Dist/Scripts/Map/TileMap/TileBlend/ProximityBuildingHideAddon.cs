// ============================================================
// ProximityBuildingHideAddon — 근접 Evaluate 스냅샷에서 야외 blocking buildingId 수집
// ============================================================
using System.Collections.Generic;
using UnityEngine;

namespace IsoTilemap
{
    public static class ProximityBuildingHideAddon
    {
        public static void CollectBlockingBuildingIds(
            in ProximityBlendEvaluationSnapshot snapshot,
            int excludeBuildingId,
            bool isPlayerOutdoor,
            bool outdoorHideEnabled,
            HashSet<int> output,
            HashSet<Vector3Int> blockingCellsScratch)
        {
            output.Clear();
            blockingCellsScratch?.Clear();

            if (!isPlayerOutdoor || !outdoorHideEnabled)
                return;

            IReadOnlyList<ProximityEvaluatedHit> hits = snapshot.EvaluatedHits;
            for (int i = 0; i < hits.Count; i++)
            {
                int buildingId = hits[i].Tile.identity.buildingId;
                if (buildingId <= 0 || buildingId == excludeBuildingId)
                    continue;

                output.Add(buildingId);
                blockingCellsScratch?.Add(hits[i].OccupiedCell);
            }
        }

        public static SightLineBuildingDebugSnapshot BuildDebugSnapshot(
            in ProximityBlendEvaluationSnapshot snapshot,
            HashSet<int> blockingBuildingIds,
            HashSet<Vector3Int> blockingCellsScratch,
            bool isPlayerOutdoor)
        {
            if (!isPlayerOutdoor || snapshot.BlendCells.Count == 0)
                return SightLineBuildingDebugSnapshot.Empty;

            IReadOnlyCollection<Vector3Int> blockingCells = blockingCellsScratch != null && blockingCellsScratch.Count > 0
                ? blockingCellsScratch
                : BuildBlockingCells(snapshot.EvaluatedHits, blockingBuildingIds);

            return new SightLineBuildingDebugSnapshot(
                true,
                snapshot.CameraWorld,
                snapshot.PlayerWorld,
                snapshot.BlendCells,
                blockingCells,
                blockingBuildingIds);
        }

        static HashSet<Vector3Int> BuildBlockingCells(
            IReadOnlyList<ProximityEvaluatedHit> hits,
            HashSet<int> blockingBuildingIds)
        {
            var cells = new HashSet<Vector3Int>();
            if (blockingBuildingIds == null || blockingBuildingIds.Count == 0)
                return cells;

            for (int i = 0; i < hits.Count; i++)
            {
                int buildingId = hits[i].Tile.identity.buildingId;
                if (buildingId > 0 && blockingBuildingIds.Contains(buildingId))
                    cells.Add(hits[i].OccupiedCell);
            }

            return cells;
        }
    }
}
