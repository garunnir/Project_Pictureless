// ============================================================
// SpaceLeakEvaluator — Space bake 후 천장·측면 누수 → isOutdoor
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

            return EvaluateCeilingLeak(floorCells, extent, index) ||
                   EvaluateLateralLeak(floorCells, buildingId, extent, index);
        }

        static bool EvaluateCeilingLeak(
            IReadOnlyCollection<Vector3Int> floorCells,
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
                    if (IsBlockedForCeilingProbe(index, x, y, z))
                        break;

                    if (y > capY)
                        return true;
                }
            }

            return false;
        }

        static bool IsBlockedForCeilingProbe(FloorMapIndex index, int x, int y, int z)
        {
            if (!index.TryGetCellTiles(x, z, y, out var list) || list == null || list.Count == 0)
                return false;

            return FloorMapIndex.CellHasSolidWall(list);
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
                var neighbor = new Vector3Int(nx, cellY, nz);

                if (extent.ContainsFloorFootprint(cellY, nx, nz))
                    continue;

                if (index.EdgeSeparatesRoom(cell, neighbor))
                    continue;

                if (index.TryGetCellTiles(nx, nz, cellY, out var list) &&
                    FloorMapIndex.CellHasSolidWall(list))
                    continue;

                if (index.CellHasFloor(nx, cellY, nz) &&
                    FloorRoomFloodFill.CellFloorMatchesBuilding(index, nx, cellY, nz, buildingId))
                    continue;

                return true;
            }

            return false;
        }
    }
}
