// ============================================================
// BuildingBlockingController — 시선 차단 buildingId 델타 추적
// ============================================================
using System;
using System.Collections.Generic;

namespace IsoTilemap
{
    public sealed class BuildingBlockingController
    {
        readonly HashSet<int> _appliedBuildingIds = new();
        readonly List<int> _addedScratch = new();
        readonly List<int> _removedScratch = new();

        public IReadOnlyList<int> LastAdded => _addedScratch;
        public IReadOnlyList<int> LastRemoved => _removedScratch;
        public bool HasAnyBlocked => _appliedBuildingIds.Count > 0;

        public bool IsBlocked(int buildingId) =>
            buildingId > 0 && _appliedBuildingIds.Contains(buildingId);

        public void Reset()
        {
            _appliedBuildingIds.Clear();
            _addedScratch.Clear();
            _removedScratch.Clear();
        }

        public void ApplyDelta(
            HashSet<int> nextBlocking,
            Action<int, FloorVisibilityContext> onAdded,
            Action<int, FloorVisibilityContext> onRemoved,
            in FloorVisibilityContext ctx)
        {
            nextBlocking ??= new HashSet<int>();

            _addedScratch.Clear();
            foreach (int id in nextBlocking)
            {
                if (!_appliedBuildingIds.Contains(id))
                    _addedScratch.Add(id);
            }

            _removedScratch.Clear();
            foreach (int id in _appliedBuildingIds)
            {
                if (!nextBlocking.Contains(id))
                    _removedScratch.Add(id);
            }

            for (int i = 0; i < _addedScratch.Count; i++)
                onAdded?.Invoke(_addedScratch[i], ctx);

            for (int i = 0; i < _removedScratch.Count; i++)
                onRemoved?.Invoke(_removedScratch[i], ctx);

            _appliedBuildingIds.Clear();
            foreach (int id in nextBlocking)
                _appliedBuildingIds.Add(id);
        }
    }
}
