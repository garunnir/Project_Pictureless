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

            if (!index.TryGetCellTiles(start.x, start.z, cellY, out var startList) ||
                startList == null || startList.Count == 0 ||
                (restrictBuildingId >= 0 && !CellFloorMatchesBuilding(startList, restrictBuildingId)))
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

                    if (!index.TryGetCellTiles(nx, nz, cellY, out var list))
                    {
                        if (collectEmptyNeighbors)
                            emptyDiscovered.Add((nx, nz));
                        continue;
                    }

                    bool hasSolidWall = FloorMapIndex.CellHasSolidWall(list);
                    bool isFloor = FloorMapIndex.CellHasFloor(list);

                    if (hasSolidWall)
                        continue;

                    if (!isFloor)
                        continue;

                    if (excludeCells != null && excludeCells.Contains((nx, nz)))
                        continue;

                    if (restrictBuildingId >= 0 &&
                        !CellFloorMatchesBuilding(list, restrictBuildingId))
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

        public static bool CellFloorMatchesBuilding(IReadOnlyList<TileData> list, int buildingId)
        {
            if (list == null)
                return false;

            for (int i = 0; i < list.Count; i++)
            {
                if ((TileView.TileType)list[i].identity.tileType != TileView.TileType.Floor)
                    continue;

                if (list[i].identity.buildingId == buildingId)
                    return true;
            }

            return false;
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
