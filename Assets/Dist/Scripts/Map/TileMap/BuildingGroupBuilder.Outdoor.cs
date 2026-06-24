// ============================================================
// BuildingGroupBuilder.Outdoor — outdoor/plaza BFS 및 cellY 범위
// ============================================================
using System.Collections.Generic;

namespace IsoTilemap
{
    public sealed partial class BuildingGroupBuilder
    {
        public void RecomputeOutdoorFromMin()
        {
            var oldOutdoor = new HashSet<(int x, int z)>(_registry.PlazaFloorXZ);
            var newOutdoor = ComputeOutdoorXZ();
            _registry.SetPlazaOutdoor(_minCellY, newOutdoor);

            foreach (var (x, z) in newOutdoor)
                SetFloorBuildingRoom(x, _minCellY, z, TileIdentity.BuildingIdOutdoor, 0);

            foreach (var (x, z) in oldOutdoor)
            {
                if (newOutdoor.Contains((x, z)))
                    continue;

                if (_topology.Index.CellHasFloor(x, _minCellY, z))
                    SetFloorBuildingRoom(x, _minCellY, z, TileIdentity.BuildingIdUnassigned, 0);
            }
        }
        HashSet<(int x, int z)> ComputeOutdoorXZ()
        {
            if (!TryFindOutdoorSeed(out int seedX, out int seedZ))
                return new HashSet<(int x, int z)>();

            var outdoor = FloorRoomFloodFill.Run(
                _topology.Index, _minCellY, seedX, seedZ, collectEmptyNeighbors: false).Visited;

            return outdoor;
        }
        bool TryFindOutdoorSeed(out int seedX, out int seedZ)
        {
            seedX = int.MaxValue;
            seedZ = int.MaxValue;
            bool found = false;

            foreach (var (x, cellY, z) in _topology.Index.EnumerateWalkableFloorCells())
            {
                if (cellY != _minCellY)
                    continue;

                if (x < seedX || (x == seedX && z < seedZ))
                {
                    seedX = x;
                    seedZ = z;
                    found = true;
                }
            }

            if (!found)
            {
                seedX = 0;
                seedZ = 0;
            }

            return found;
        }
        void ComputeCellYRange()
        {
            _minCellY = int.MaxValue;
            _maxCellY = int.MinValue;

            foreach (var tile in _model.TilesSnapshot)
            {
                if (!TileIdentityUtil.IsStructural(tile.identity))
                    continue;

                int y = TileIdentityUtil.IsFloorTile(tile.identity)
                    ? FloorFaceKey.FromFloorTileIdentity(tile.identity).CellAbove.y
                    : tile.identity.GridPos.y;

                if (y < _minCellY) _minCellY = y;
                if (y > _maxCellY) _maxCellY = y;
            }

            if (_minCellY == int.MaxValue)
            {
                _minCellY = 0;
                _maxCellY = 0;
            }
        }
        void ResetStructuralIds()
        {
            _model.ForEachRuntimeTileMutating(tile =>
            {
                if (!TileIdentityUtil.IsStructural(tile.identity))
                    return;

                _model.PatchTileIdentity(tile.tileDefId, TileIdentity.BuildingIdUnassigned, 0);
            });
        }
    }
}
