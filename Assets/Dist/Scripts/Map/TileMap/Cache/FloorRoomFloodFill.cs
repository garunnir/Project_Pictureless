// ============================================================
// FloorRoomFloodFill — 셀 Y별 (x,z) 방 BFS (오클루전·컬링 공용)
// ============================================================
using System.Collections.Generic;
using UnityEngine;

namespace IsoTilemap
{
    public readonly struct FloorBfsResult
    {
        public HashSet<(int x, int z)> Visited { get; }
        public HashSet<(int x, int z)> EmptyDiscovered { get; }

        public FloorBfsResult(
            HashSet<(int x, int z)> visited,
            HashSet<(int x, int z)> emptyDiscovered)
        {
            Visited = visited;
            EmptyDiscovered = emptyDiscovered;
        }
    }

    public static class FloorRoomFloodFill
    {
        private static readonly Vector3Int[] CardinalNeighbors =
        {
            Vector3Int.right, Vector3Int.back, Vector3Int.left, Vector3Int.forward
        };

        public static FloorBfsResult Run(
            FloorMapIndex index,
            int cellY,
            int startX,
            int startZ,
            bool collectEmptyNeighbors,
            int restrictBuildingId = -1,
            HashSet<(int x, int z)> excludeCells = null)
        {
            Vector3Int start = index.ResolveFloorBfsStart(cellY, startX, startZ);
            var visited = new HashSet<(int x, int z)>();
            var emptyDiscovered = collectEmptyNeighbors
                ? new HashSet<(int x, int z)>()
                : null;

            if (!index.CellHasFloor(start.x, cellY, start.z) ||
                (restrictBuildingId >= 0 && !CellFloorMatchesBuilding(index, start.x, cellY, start.z, restrictBuildingId)))
            {
                if (Config.DebugMode.FloorAlgorithm)
                    Debug.LogWarning($"FloorRoomFloodFill: empty start cell {start}");
                return new FloorBfsResult(visited, emptyDiscovered ?? new HashSet<(int x, int z)>());
            }

            var visitedCells = new HashSet<Vector3Int> { start };
            var q = new Queue<Vector3Int>();
            q.Enqueue(start);
            visited.Add((start.x, start.z));

            int safetyLimit = 200000;
            int steps = 0;
            while (q.Count > 0)
            {
                if (++steps > safetyLimit)
                    break;

                Vector3Int cur = q.Dequeue();
                foreach (var d in CardinalNeighbors)
                {
                    int nx = cur.x + d.x;
                    int nz = cur.z + d.z;
                    var neighbor = new Vector3Int(nx, cellY, nz);

                    if (index.EdgeSeparatesRoom(cur, neighbor))
                        continue;

                    if (visitedCells.Contains(neighbor))
                        continue;

                    if (!index.CellHasFloor(nx, cellY, nz))
                    {
                        if (collectEmptyNeighbors)
                            emptyDiscovered.Add((nx, nz));
                        continue;
                    }

                    if (index.TryGetCellTiles(nx, nz, cellY, out var list) &&
                        FloorMapIndex.CellHasSolidWall(list))
                        continue;

                    if (excludeCells != null && excludeCells.Contains((nx, nz)))
                        continue;

                    if (restrictBuildingId >= 0 &&
                        !CellFloorMatchesBuilding(index, nx, cellY, nz, restrictBuildingId))
                        continue;

                    visitedCells.Add(neighbor);
                    visited.Add((nx, nz));
                    q.Enqueue(neighbor);
                }
            }

            if (Config.DebugMode.FloorAlgorithm)
                Debug.Log($"FloorRoomFloodFill cellY={cellY} visited={visited.Count} empty={emptyDiscovered?.Count ?? 0}");

            return new FloorBfsResult(visited, emptyDiscovered ?? new HashSet<(int x, int z)>());
        }

        public static bool CellFloorMatchesBuilding(FloorMapIndex index, int x, int cellY, int z, int buildingId)
        {
            if (!index.TryGetFloorFaceForWalkableCell(x, cellY, z, out var face))
                return false;

            return face.identity.buildingId == buildingId;
        }

        public static HashSet<Vector3Int> ToVector3IntSet(HashSet<(int x, int z)> xzSet, int cellY)
        {
            var result = new HashSet<Vector3Int>(xzSet.Count);
            foreach (var (x, z) in xzSet)
                result.Add(new Vector3Int(x, cellY, z));
            return result;
        }
    }
}
