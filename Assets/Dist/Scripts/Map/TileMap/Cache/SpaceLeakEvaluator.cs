// ============================================================
// SpaceLeakEvaluator — Space bake 후 topology 누수 → isOutdoor
// collisionFlags leak 금지 — TILEMAP_BUILDING_BAKE.md 대전제 §1·§7.3
// ============================================================
using System.Collections.Generic;
using UnityEngine;

namespace IsoTilemap
{
    public static class SpaceLeakEvaluator
    {
        static readonly Vector3Int[] CardinalDirs =
        {
            Vector3Int.right, Vector3Int.back, Vector3Int.left, Vector3Int.forward
        };

        const int CeilingProbeMaxSteps = 64;

        public static bool Evaluate(
            IReadOnlyCollection<Vector3Int> floorCells,
            int buildingId,
            BuildingExtent extent,
            FloorMapIndex index)
        {
            if (floorCells == null || floorCells.Count == 0 || index == null || !extent.HasBounds)
                return true;

            if (!BuildingIdBakeRules.CanPropagateBuildingIdFrom(buildingId))
                return true;

            return EvaluateCeilingLeak(floorCells, buildingId, extent, index) ||
                   EvaluateLateralLeak(floorCells, buildingId, extent, index);
        }

        /// <summary>디버그·진단용. leak 분리 (topology만).</summary>
        public static void EvaluateComponents(
            IReadOnlyCollection<Vector3Int> floorCells,
            int buildingId,
            BuildingExtent extent,
            FloorMapIndex index,
            out bool ceilingLeak,
            out bool lateralLeak)
        {
            ceilingLeak = false;
            lateralLeak = false;
            if (floorCells == null || floorCells.Count == 0 || index == null || !extent.HasBounds)
            {
                ceilingLeak = true;
                lateralLeak = true;
                return;
            }

            if (!BuildingIdBakeRules.CanPropagateBuildingIdFrom(buildingId))
            {
                ceilingLeak = true;
                lateralLeak = true;
                return;
            }

            ceilingLeak = EvaluateCeilingLeak(floorCells, buildingId, extent, index);
            lateralLeak = EvaluateLateralLeak(floorCells, buildingId, extent, index);
        }

        static bool EvaluateCeilingLeak(
            IReadOnlyCollection<Vector3Int> floorCells,
            int buildingId,
            BuildingExtent extent,
            FloorMapIndex index)
        {
            var columnMaxY = new Dictionary<(int x, int z), int>();
            foreach (var cell in floorCells)
            {
                var key = (cell.x, cell.z);
                if (!columnMaxY.TryGetValue(key, out int maxY) || cell.y > maxY)
                    columnMaxY[key] = cell.y;
            }

            int capY = extent.MaxStructuralY;
            foreach (var kv in columnMaxY)
            {
                int x = kv.Key.x;
                int z = kv.Key.z;
                int startY = kv.Value + 1;

                for (int step = 0; step < CeilingProbeMaxSteps; step++)
                {
                    int y = startY + step;
                    if (OccupiedCellHasBuildingId(index, x, y, z, buildingId))
                        break;

                    if (index.CellHasFloor(x, y, z) &&
                        FloorRoomFloodFill.CellFloorMatchesBuilding(index, x, y, z, buildingId))
                        break;

                    if (y > capY)
                        return true;
                }
            }

            return false;
        }

        static bool EvaluateLateralLeak(
            IReadOnlyCollection<Vector3Int> floorCells,
            int buildingId,
            BuildingExtent extent,
            FloorMapIndex index)
        {
            foreach (var cell in floorCells)
            {
                if (IsLateralLeakAtCell(cell, buildingId, extent, index))
                    return true;
            }

            return false;
        }

        static bool IsLateralLeakAtCell(
            Vector3Int cell,
            int buildingId,
            BuildingExtent extent,
            FloorMapIndex index)
        {
            int cellY = cell.y;
            foreach (var d in CardinalDirs)
            {
                int nx = cell.x + d.x;
                int nz = cell.z + d.z;

                if (extent.ContainsFloorFootprint(cellY, nx, nz))
                    continue;

                var neighbor = new Vector3Int(nx, cellY, nz);
                if (EdgeHasBuildingId(index, cell, neighbor, buildingId))
                    continue;

                if (OccupiedCellHasBuildingId(index, nx, cellY, nz, buildingId))
                    continue;

                if (!index.CellHasFloor(nx, cellY, nz))
                    return true;

                if (FloorRoomFloodFill.CellFloorMatchesBuilding(index, nx, cellY, nz, buildingId))
                    continue;

                if (!TryGetWalkableFloorBuildingId(index, nx, cellY, nz, out int neighborBuildingId))
                    return true;

                if (neighborBuildingId == TileIdentity.BuildingIdOutdoor)
                    return true;

                if (neighborBuildingId <= 0)
                    return true;

                if (neighborBuildingId != buildingId)
                    return true;
            }

            return false;
        }

        static bool EdgeHasBuildingId(
            FloorMapIndex index,
            Vector3Int cellA,
            Vector3Int cellB,
            int buildingId)
        {
            if (!index.TryGetEdgeBetween(cellA, cellB, out var edge))
                return false;

            return edge.identity.buildingId == buildingId;
        }

        static bool OccupiedCellHasBuildingId(FloorMapIndex index, int x, int y, int z, int buildingId)
        {
            if (!index.TryGetCellTiles(x, z, y, out var list) || list == null || list.Count == 0)
                return false;

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].identity.buildingId == buildingId)
                    return true;
            }

            return false;
        }

        static bool TryGetWalkableFloorBuildingId(
            FloorMapIndex index,
            int x,
            int cellY,
            int z,
            out int buildingId)
        {
            buildingId = 0;
            if (!index.TryGetFloorFaceForWalkableCell(x, cellY, z, out var face))
                return false;

            buildingId = face.identity.buildingId;
            return true;
        }
    }
}
