// ============================================================
// BuildingVerticalLink — band 간 floor footprint XZ 겹침으로 상향 연결 판정
// ============================================================
using System.Collections.Generic;
using UnityEngine;

namespace IsoTilemap
{
    public static class BuildingVerticalLink
    {
        public static HashSet<int> CollectConnectedBands(
            TopologyLayer topology,
            HashSet<(int x, int z)> seedFootprint,
            int startBand,
            int maxBand)
        {
            var bands = new HashSet<int> { startBand };
            if (seedFootprint == null || seedFootprint.Count == 0)
                return bands;

            var occupied = CollectOccupiedXZ(topology, seedFootprint, startBand);
            int band = startBand;

            while (band < maxBand)
            {
                int above = band + 1;
                var aboveOccupied = CollectBandFloorOccupiedXZ(topology, above);
                if (!OccupiedXZOverlaps(occupied, aboveOccupied))
                    break;

                bands.Add(above);
                foreach (var cell in aboveOccupied)
                    occupied.Add(cell);

                band = above;
            }

            return bands;
        }

        public static HashSet<(int x, int z)> CollectOccupiedXZ(
            TopologyLayer topology,
            HashSet<(int x, int z)> anchors,
            int band)
        {
            var occupied = new HashSet<(int x, int z)>();
            if (anchors == null)
                return occupied;

            foreach (var (ax, az) in anchors)
                AddFloorTileFootprint(topology, ax, az, band, occupied);

            return occupied;
        }

        public static HashSet<(int x, int z)> CollectBandFloorOccupiedXZ(TopologyLayer topology, int band)
        {
            var occupied = new HashSet<(int x, int z)>();
            foreach (var (x, z, b) in topology.EnumerateOccupiedCells())
            {
                if (b != band)
                    continue;

                if (!topology.TryGetCellTiles(x, z, band, out var list) || !FloorMapIndex.CellHasFloor(list))
                    continue;

                AddFloorTileFootprint(topology, x, z, band, occupied);
            }

            return occupied;
        }

        public static bool OccupiedXZOverlaps(
            HashSet<(int x, int z)> a,
            HashSet<(int x, int z)> b)
        {
            if (a == null || b == null || a.Count == 0 || b.Count == 0)
                return false;

            foreach (var cell in a)
            {
                if (b.Contains(cell))
                    return true;
            }

            return false;
        }

        static void AddFloorTileFootprint(
            TopologyLayer topology,
            int anchorX,
            int anchorZ,
            int band,
            HashSet<(int x, int z)> occupied)
        {
            if (!topology.TryGetCellTiles(anchorX, anchorZ, band, out var list))
                return;

            for (int i = 0; i < list.Count; i++)
            {
                var tile = list[i];
                if ((TileView.TileType)tile.identity.tileType != TileView.TileType.Floor)
                    continue;

                var pos = tile.identity.GridPos;
                var size = tile.identity.sizeUnit;
                if (size.x < 1) size.x = 1;
                if (size.z < 1) size.z = 1;

                for (int dx = 0; dx < size.x; dx++)
                {
                    for (int dz = 0; dz < size.z; dz++)
                        occupied.Add((pos.x + dx, pos.z + dz));
                }
            }
        }
    }
}
