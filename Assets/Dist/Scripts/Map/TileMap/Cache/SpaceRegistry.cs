// ============================================================
// SpaceRegistry — floor cell → SpaceId 역인덱스 및 bake 결과
// ============================================================
using System;
using System.Collections.Generic;
using UnityEngine;

namespace IsoTilemap
{
    public sealed class SpaceRegistry
    {
        readonly Dictionary<Vector3Int, int> _floorCellToSpaceId = new();
        readonly Dictionary<int, SpaceBakeResult> _spacesById = new();
        readonly Dictionary<int, HashSet<Vector3Int>> _floorCellsBySpaceId = new();
        int _nextSpaceId = 1;

        public void Clear()
        {
            _floorCellToSpaceId.Clear();
            _spacesById.Clear();
            _floorCellsBySpaceId.Clear();
            _nextSpaceId = 1;
        }

        public int AllocateSpaceId() => _nextSpaceId++;

        public bool TryGetSpaceAtFloorCell(Vector3Int floorCell, out int spaceId) =>
            _floorCellToSpaceId.TryGetValue(floorCell, out spaceId);

        public bool TryGetSpaceAtFloorCell(int cellY, int x, int z, out int spaceId) =>
            TryGetSpaceAtFloorCell(new Vector3Int(x, cellY, z), out spaceId);

        public bool TryGetSpace(int spaceId, out SpaceBakeResult result) =>
            _spacesById.TryGetValue(spaceId, out result);

        public bool IsOutdoorSpace(int spaceId) =>
            _spacesById.TryGetValue(spaceId, out var result) && result.IsOutdoor;

        public IReadOnlyCollection<int> SpaceIds => _spacesById.Keys;

        public IReadOnlyCollection<Vector3Int> GetFloorCells(int spaceId)
        {
            if (_floorCellsBySpaceId.TryGetValue(spaceId, out var set))
                return set;

            return Array.Empty<Vector3Int>();
        }

        public void AssignNew(
            int spaceId,
            int buildingId,
            RoomKey seedRoom,
            IEnumerable<Vector3Int> floorCells)
        {
            if (spaceId <= 0)
                return;

            var result = new SpaceBakeResult(spaceId, buildingId, seedRoom);
            _spacesById[spaceId] = result;

            var cellSet = new HashSet<Vector3Int>();
            if (floorCells != null)
            {
                foreach (var cell in floorCells)
                {
                    cellSet.Add(cell);
                    _floorCellToSpaceId[cell] = spaceId;
                }
            }

            _floorCellsBySpaceId[spaceId] = cellSet;
        }

        public void Absorb(int canonicalSpaceId, IEnumerable<Vector3Int> floorCells)
        {
            if (canonicalSpaceId <= 0 || floorCells == null)
                return;

            if (!_floorCellsBySpaceId.TryGetValue(canonicalSpaceId, out var cellSet))
            {
                cellSet = new HashSet<Vector3Int>();
                _floorCellsBySpaceId[canonicalSpaceId] = cellSet;
            }

            foreach (var cell in floorCells)
            {
                cellSet.Add(cell);
                _floorCellToSpaceId[cell] = canonicalSpaceId;
            }
        }

        public void SetOutdoor(int spaceId, bool isOutdoor)
        {
            if (_spacesById.TryGetValue(spaceId, out var result))
                result.IsOutdoor = isOutdoor;
        }
    }
}
