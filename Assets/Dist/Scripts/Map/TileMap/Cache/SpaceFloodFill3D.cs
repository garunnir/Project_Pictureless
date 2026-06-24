// ============================================================
// SpaceFloodFill3D — room floor seed에서 3D floor-graph Space flood
// ============================================================
using System.Collections.Generic;
using UnityEngine;

namespace IsoTilemap
{
    public static class SpaceFloodFill3D
    {
        static readonly Vector3Int[] CardinalDirs =
        {
            Vector3Int.right, Vector3Int.back, Vector3Int.left, Vector3Int.forward
        };

        public static SpaceFloodResult Run(
            FloorMapIndex index,
            SpaceRegistry registry,
            Vector3Int seedFloorCell,
            int buildingId)
        {
            if (index == null || registry == null || buildingId <= 0)
                return SpaceFloodResult.Empty;

            if (registry.TryGetSpaceAtFloorCell(seedFloorCell, out _))
                return SpaceFloodResult.Empty;

            Vector3Int start = index.ResolveFloorBfsStart(
                seedFloorCell.y, seedFloorCell.x, seedFloorCell.z);

            if (!index.CellHasFloor(start.x, start.y, start.z) ||
                !FloorRoomFloodFill.CellFloorMatchesBuilding(index, start.x, start.y, start.z, buildingId))
            {
                return SpaceFloodResult.Empty;
            }

            var visitedFloor = new HashSet<Vector3Int>();
            var boundarySpaceIds = new HashSet<int>();
            var visited = new HashSet<Vector3Int> { start };
            var q = new Queue<Vector3Int>();
            q.Enqueue(start);
            visitedFloor.Add(start);

            int safetyLimit = 200000;
            int steps = 0;

            while (q.Count > 0)
            {
                if (++steps > safetyLimit)
                    break;

                Vector3Int cur = q.Dequeue();

                TryExpandCardinal(index, registry, buildingId, cur, visited, visitedFloor, boundarySpaceIds, q);
                TryExpandUp(index, registry, buildingId, cur, visited, visitedFloor, boundarySpaceIds, q);
            }

            return new SpaceFloodResult(visitedFloor, boundarySpaceIds);
        }

        static void TryExpandCardinal(
            FloorMapIndex index,
            SpaceRegistry registry,
            int buildingId,
            Vector3Int cur,
            HashSet<Vector3Int> visited,
            HashSet<Vector3Int> visitedFloor,
            HashSet<int> boundarySpaceIds,
            Queue<Vector3Int> q)
        {
            int cellY = cur.y;
            foreach (var d in CardinalDirs)
            {
                int nx = cur.x + d.x;
                int nz = cur.z + d.z;
                var neighbor = new Vector3Int(nx, cellY, nz);

                if (index.EdgeSeparatesRoom(cur, neighbor))
                    continue;

                if (!index.CellHasFloor(nx, cellY, nz))
                    continue;

                if (index.TryGetCellTiles(nx, nz, cellY, out var list) &&
                    FloorMapIndex.CellHasSolidWall(list))
                    continue;

                if (!FloorRoomFloodFill.CellFloorMatchesBuilding(index, nx, cellY, nz, buildingId))
                    continue;

                if (registry.TryGetSpaceAtFloorCell(neighbor, out int existingId))
                {
                    if (!index.EdgeSeparatesRoom(cur, neighbor) &&
                        !(index.TryGetCellTiles(nx, nz, cellY, out var nList) &&
                          FloorMapIndex.CellHasSolidWall(nList)))
                    {
                        boundarySpaceIds.Add(existingId);
                    }

                    continue;
                }

                if (!visited.Add(neighbor))
                    continue;

                visitedFloor.Add(neighbor);
                q.Enqueue(neighbor);
            }
        }

        static void TryExpandUp(
            FloorMapIndex index,
            SpaceRegistry registry,
            int buildingId,
            Vector3Int cur,
            HashSet<Vector3Int> visited,
            HashSet<Vector3Int> visitedFloor,
            HashSet<int> boundarySpaceIds,
            Queue<Vector3Int> q)
        {
            int aboveY = cur.y + 1;
            var neighbor = new Vector3Int(cur.x, aboveY, cur.z);

            if (!index.CellHasFloor(cur.x, aboveY, cur.z))
                return;

            if (!FloorRoomFloodFill.CellFloorMatchesBuilding(index, cur.x, aboveY, cur.z, buildingId))
                return;

            if (registry.TryGetSpaceAtFloorCell(neighbor, out int existingId))
            {
                boundarySpaceIds.Add(existingId);
                return;
            }

            if (!visited.Add(neighbor))
                return;

            visitedFloor.Add(neighbor);
            q.Enqueue(neighbor);
        }
    }
}
