// ============================================================
// SpaceFloodResult — SpaceFloodFill3D 3D floor-graph flood 산출
// ============================================================
using System.Collections.Generic;
using UnityEngine;

namespace IsoTilemap
{
    public readonly struct SpaceFloodResult
    {
        public HashSet<Vector3Int> VisitedFloor { get; }
        public HashSet<int> BoundarySpaceIds { get; }

        public SpaceFloodResult(
            HashSet<Vector3Int> visitedFloor,
            HashSet<int> boundarySpaceIds)
        {
            VisitedFloor = visitedFloor ?? new HashSet<Vector3Int>();
            BoundarySpaceIds = boundarySpaceIds ?? new HashSet<int>();
        }

        public static SpaceFloodResult Empty { get; } =
            new SpaceFloodResult(new HashSet<Vector3Int>(), new HashSet<int>());
    }
}
