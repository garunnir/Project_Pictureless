// ============================================================
// BuildingExtent — bake된 buildingId별 slice footprint·AABB·maxStructuralY
// ============================================================
using System;
using System.Collections.Generic;
using UnityEngine;

namespace IsoTilemap
{
    /// <summary>
    /// <see cref="BuildingGroupRegistry.RebuildIndicesFromTiles"/> 산출. §6 TILEMAP_BUILDING_BAKE.md.
    /// </summary>
    public sealed class BuildingExtent
    {
        public static readonly BuildingExtent Empty = new BuildingExtent(0);

        readonly Dictionary<int, HashSet<(int x, int z)>> _floorFootprintByCellY;

        public int BuildingId { get; }
        public int MinX { get; }
        public int MaxX { get; }
        public int MinZ { get; }
        public int MaxZ { get; }
        public int MinOccupiedY { get; }
        public int MaxOccupiedY { get; }
        public int MaxStructuralY { get; }
        public bool HasBounds { get; }

        BuildingExtent(
            int buildingId,
            int minX,
            int maxX,
            int minZ,
            int maxZ,
            int minOccupiedY,
            int maxOccupiedY,
            int maxStructuralY,
            Dictionary<int, HashSet<(int x, int z)>> floorFootprintByCellY)
        {
            BuildingId = buildingId;
            MinX = minX;
            MaxX = maxX;
            MinZ = minZ;
            MaxZ = maxZ;
            MinOccupiedY = minOccupiedY;
            MaxOccupiedY = maxOccupiedY;
            MaxStructuralY = maxStructuralY;
            HasBounds = buildingId > 0 && minX <= maxX;
            _floorFootprintByCellY = floorFootprintByCellY ?? new Dictionary<int, HashSet<(int x, int z)>>();
        }

        BuildingExtent(int buildingId)
            : this(buildingId, 0, -1, 0, -1, 0, -1, -1, null)
        {
        }

        public bool ContainsAabb(int x, int y, int z) =>
            HasBounds &&
            x >= MinX && x <= MaxX &&
            z >= MinZ && z <= MaxZ &&
            y >= MinOccupiedY && y <= MaxOccupiedY;

        public bool ContainsFloorFootprint(int cellY, int x, int z)
        {
            if (!_floorFootprintByCellY.TryGetValue(cellY, out var set))
                return false;

            return set.Contains((x, z));
        }

        public bool TryGetFloorFootprint(int cellY, out IReadOnlyCollection<(int x, int z)> footprint)
        {
            if (_floorFootprintByCellY.TryGetValue(cellY, out var set))
            {
                footprint = set;
                return true;
            }

            footprint = Array.Empty<(int x, int z)>();
            return false;
        }

        public IReadOnlyDictionary<int, HashSet<(int x, int z)>> FloorFootprintByCellY => _floorFootprintByCellY;

        internal sealed class Builder
        {
            readonly int _buildingId;
            readonly Dictionary<int, HashSet<(int x, int z)>> _floorFootprintByCellY = new();
            readonly List<Vector3Int> _cellScratch = new(16);

            int _minX = int.MaxValue;
            int _maxX = int.MinValue;
            int _minZ = int.MaxValue;
            int _maxZ = int.MinValue;
            int _minOccupiedY = int.MaxValue;
            int _maxOccupiedY = int.MinValue;
            int _maxStructuralY = int.MinValue;
            int _minFloorY = int.MaxValue;
            readonly HashSet<Guid> _minFloorTileIds = new();

            public Builder(int buildingId) => _buildingId = buildingId;

            public int MinFloorY => _minFloorY;

            public IReadOnlyCollection<Guid> MinFloorTileIds => _minFloorTileIds;

            public void IncludeTile(in TileData tile)
            {
                if (tile.identity.buildingId != _buildingId)
                    return;

                _cellScratch.Clear();
                AppendOccupiedCells(tile, _cellScratch);

                bool isStructural = TileIdentityUtil.IsStructural(tile.identity);
                for (int i = 0; i < _cellScratch.Count; i++)
                    IncludeOccupiedCell(_cellScratch[i], isStructural);

                if (TileIdentityUtil.IsFloorTile(tile.identity))
                    IncludeFloorFootprint(tile);
            }

            public BuildingExtent Build()
            {
                if (_minX > _maxX)
                    return new BuildingExtent(_buildingId);

                return new BuildingExtent(
                    _buildingId,
                    _minX,
                    _maxX,
                    _minZ,
                    _maxZ,
                    _minOccupiedY,
                    _maxOccupiedY,
                    _maxStructuralY < int.MinValue ? _maxOccupiedY : _maxStructuralY,
                    _floorFootprintByCellY);
            }

            void IncludeOccupiedCell(Vector3Int cell, bool isStructural)
            {
                if (cell.x < _minX) _minX = cell.x;
                if (cell.x > _maxX) _maxX = cell.x;
                if (cell.z < _minZ) _minZ = cell.z;
                if (cell.z > _maxZ) _maxZ = cell.z;
                if (cell.y < _minOccupiedY) _minOccupiedY = cell.y;
                if (cell.y > _maxOccupiedY) _maxOccupiedY = cell.y;

                if (isStructural && cell.y > _maxStructuralY)
                    _maxStructuralY = cell.y;
            }

            void IncludeFloorFootprint(in TileData tile)
            {
                var key = FloorFaceKey.FromFloorTileIdentity(tile.identity);
                int sy = tile.identity.sizeUnit.y;
                if (sy < 1) sy = 1;

                for (int dy = 0; dy < sy; dy++)
                {
                    int cellY = key.CellAbove.y + dy;
                    if (cellY < _minFloorY)
                    {
                        _minFloorY = cellY;
                        _minFloorTileIds.Clear();
                    }

                    if (cellY == _minFloorY)
                        _minFloorTileIds.Add(tile.tileDefId);

                    if (!_floorFootprintByCellY.TryGetValue(cellY, out var set))
                    {
                        set = new HashSet<(int x, int z)>();
                        _floorFootprintByCellY[cellY] = set;
                    }

                    set.Add((key.CellAbove.x, key.CellAbove.z));
                }
            }

            static void AppendOccupiedCells(in TileData tile, List<Vector3Int> cells)
            {
                var id = tile.identity;
                switch (TileIdentityUtil.GetPlacementSlot(id))
                {
                    case TilePlacementSlot.VerticalFace:
                        TileIdentityUtil.AppendWallIncidentCells(
                            TileIdentityUtil.ToWallEdgeKey(id), id.sizeUnit.y, cells);
                        break;
                    case TilePlacementSlot.HorizontalFace:
                        TileIdentityUtil.AppendFloorIncidentCells(
                            TileIdentityUtil.ToFloorFaceKey(id), id.sizeUnit.y, cells);
                        break;
                    default:
                        AppendOccupiedCellBox(id.GridPos, id.sizeUnit, cells);
                        break;
                }
            }

            static void AppendOccupiedCellBox(Vector3Int basePos, Vector3Int sizeUnit, List<Vector3Int> cells)
            {
                int sx = Mathf.Max(1, sizeUnit.x);
                int sy = Mathf.Max(1, sizeUnit.y);
                int sz = Mathf.Max(1, sizeUnit.z);

                for (int dx = 0; dx < sx; dx++)
                {
                    for (int dy = 0; dy < sy; dy++)
                    {
                        for (int dz = 0; dz < sz; dz++)
                        {
                            cells.Add(new Vector3Int(
                                basePos.x + dx,
                                basePos.y + dy,
                                basePos.z + dz));
                        }
                    }
                }
            }
        }
    }
}
