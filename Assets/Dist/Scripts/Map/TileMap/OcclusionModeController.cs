// ============================================================
// OcclusionModeController — 전역 오클루전 모드 상태전이/델타 계산
// ============================================================
using System;
using System.Collections.Generic;

namespace IsoTilemap
{
    public sealed class OcclusionModeController
    {
        readonly HashSet<int> _appliedBuildingIds = new();
        readonly List<int> _addedScratch = new();
        readonly List<int> _removedScratch = new();
        OcclusionMode _currentMode = OcclusionMode.LegacyCompatible;

        public OcclusionMode CurrentMode => _currentMode;
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
            _currentMode = OcclusionMode.LegacyCompatible;
        }

        public void ApplyDelta(
            HashSet<int> nextBlocking,
            OcclusionMode nextMode,
            FloorVisibilityContext ctx,
            Action<int, OcclusionMode, FloorVisibilityContext> onAdded,
            Action<int, OcclusionMode, FloorVisibilityContext> onRemoved)
        {
            nextBlocking ??= new HashSet<int>();

            if (_currentMode != nextMode && _appliedBuildingIds.Count > 0)
            {
                _removedScratch.Clear();
                foreach (int id in _appliedBuildingIds)
                    _removedScratch.Add(id);

                for (int i = 0; i < _removedScratch.Count; i++)
                    onRemoved?.Invoke(_removedScratch[i], _currentMode, ctx);

                _appliedBuildingIds.Clear();
            }

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
                onAdded?.Invoke(_addedScratch[i], nextMode, ctx);

            for (int i = 0; i < _removedScratch.Count; i++)
                onRemoved?.Invoke(_removedScratch[i], nextMode, ctx);

            _appliedBuildingIds.Clear();
            foreach (int id in nextBlocking)
                _appliedBuildingIds.Add(id);

            _currentMode = nextMode;
        }
    }
}
