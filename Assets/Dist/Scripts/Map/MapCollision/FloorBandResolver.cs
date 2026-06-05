using System;
using System.Collections.Generic;
using UnityEngine;

namespace IsoTilemap
{
    public sealed class FloorBandResolver
    {
        readonly int _minBand;
        readonly int[] _distinctBands;
        readonly float _cellSize;
        readonly float _bandEpsilonWorld;

        public FloorBandResolver(int[] distinctBands, float cellSize, float bandEpsilonWorld = 0f)
        {
            if (distinctBands == null || distinctBands.Length == 0)
                distinctBands = new[] { 0 };

            _distinctBands = distinctBands;
            Array.Sort(_distinctBands);
            _minBand = _distinctBands[0];
            _cellSize = cellSize > 0f ? cellSize : 1f;
            _bandEpsilonWorld = bandEpsilonWorld;
        }

        public int MinBand => _minBand;

        public IReadOnlyList<int> DistinctBands => _distinctBands;

        public int Resolve(float worldY)
        {
            int floorBand = _minBand;
            float ceiling = worldY + _bandEpsilonWorld;

            for (int i = 0; i < _distinctBands.Length; i++)
            {
                int band = _distinctBands[i];
                if (band * _cellSize <= ceiling)
                    floorBand = band;
            }

            return floorBand;
        }

        public static FloorBandResolver FromTiles(IReadOnlyList<TileData> tiles, float cellSize, float bandEpsilonWorld = 0f)
        {
            var bandSet = new System.Collections.Generic.HashSet<int>();
            if (tiles != null)
            {
                for (int i = 0; i < tiles.Count; i++)
                    bandSet.Add(tiles[i].identity.GridPos.y);
            }

            if (bandSet.Count == 0)
                bandSet.Add(0);

            var distinct = new int[bandSet.Count];
            bandSet.CopyTo(distinct);
            return new FloorBandResolver(distinct, cellSize, bandEpsilonWorld);
        }
    }
}
