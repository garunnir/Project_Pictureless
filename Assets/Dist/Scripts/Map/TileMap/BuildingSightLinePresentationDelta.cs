using System.Collections.Generic;

namespace IsoTilemap
{
    /// <summary>야외 시선 차단 building 집합 변경분.</summary>
    public readonly struct BuildingSightLinePresentationDelta
    {
        public IReadOnlyList<int> AddedBuildingIds { get; }
        public IReadOnlyList<int> RemovedBuildingIds { get; }

        public BuildingSightLinePresentationDelta(
            IReadOnlyList<int> addedBuildingIds,
            IReadOnlyList<int> removedBuildingIds)
        {
            AddedBuildingIds = addedBuildingIds ?? System.Array.Empty<int>();
            RemovedBuildingIds = removedBuildingIds ?? System.Array.Empty<int>();
        }

        public bool IsEmpty => AddedBuildingIds.Count == 0 && RemovedBuildingIds.Count == 0;
    }
}
