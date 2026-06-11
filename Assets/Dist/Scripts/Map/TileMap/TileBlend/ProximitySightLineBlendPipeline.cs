// ============================================================
// ProximitySightLineBlendPipeline — 시선 밴드 내 타일 가림 강도 산출
// ============================================================
using System;
using System.Collections.Generic;
using UnityEngine;

namespace IsoTilemap
{
    public sealed class ProximitySightLineBlendPipeline
    {
        static readonly Vector3Int[] CardinalNeighbors =
        {
            Vector3Int.right, Vector3Int.back, Vector3Int.left, Vector3Int.forward
        };

        readonly TileMapCacheHub _hub;
        readonly HashSet<Vector3Int> _bandCells = new();
        readonly List<TileData> _cellTilesScratch = new();
        readonly List<TileData> _edgeTilesScratch = new();
        readonly List<TileData> _faceTilesScratch = new();
        readonly Dictionary<Guid, float> _scratch = new();
        readonly List<(Guid tileId, float occlusion01)> _applyScratch = new();
        readonly List<Guid> _clearScratch = new();

        public ProximitySightLineBlendPipeline(TileMapCacheHub hub) =>
            _hub = hub ?? throw new ArgumentNullException(nameof(hub));

        public TileOcclusionPresentationDelta Evaluate(
            Vector3 cameraWorld,
            Vector3 playerWorld,
            Vector3Int playerCell,
            int playerFloorCellY,
            bool isPlayerOutdoor,
            in SightLineBlendSettings settings,
            IReadOnlyDictionary<Guid, float> previous)
        {
            _scratch.Clear();
            _applyScratch.Clear();
            _clearScratch.Clear();

            if (_hub == null)
                return new TileOcclusionPresentationDelta(_applyScratch, _clearScratch);

            SightLineSegmentSampler.CollectBlendBandCells(
                cameraWorld, playerWorld, playerCell, settings, _bandCells);

            float cellSize = settings.CellSize > 0f ? settings.CellSize : 1f;
            float eps = Mathf.Max(0f, settings.ApplyEpsilon);

            foreach (Vector3Int cell in _bandCells)
            {
                if (!SightLineOcclusionStrength.PassesPlayerDownXQuadrant(cell, playerCell))
                    continue;

                _cellTilesScratch.Clear();
                _edgeTilesScratch.Clear();
                _faceTilesScratch.Clear();

                if (_hub.TryGetCellTiles(cell.x, cell.z, cell.y, out var tiles))
                {
                    for (int i = 0; i < tiles.Count; i++)
                        _cellTilesScratch.Add(tiles[i]);
                }

                AppendIncidentEdges(cell, _edgeTilesScratch);
                AppendIncidentFaces(cell, _faceTilesScratch);

                if (_cellTilesScratch.Count == 0 &&
                    _edgeTilesScratch.Count == 0 &&
                    _faceTilesScratch.Count == 0)
                    continue;

                for (int i = 0; i < _cellTilesScratch.Count; i++)
                {
                    AccumulateOcclusion(
                        _cellTilesScratch[i], cell, cameraWorld, playerWorld,
                        playerCell, playerFloorCellY, isPlayerOutdoor, cellSize, settings, eps);
                }

                for (int i = 0; i < _edgeTilesScratch.Count; i++)
                {
                    AccumulateOcclusion(
                        _edgeTilesScratch[i], cell, cameraWorld, playerWorld,
                        playerCell, playerFloorCellY, isPlayerOutdoor, cellSize, settings, eps);
                }

                for (int i = 0; i < _faceTilesScratch.Count; i++)
                {
                    AccumulateOcclusion(
                        _faceTilesScratch[i], cell, cameraWorld, playerWorld,
                        playerCell, playerFloorCellY, isPlayerOutdoor, cellSize, settings, eps);
                }
            }

            foreach (var kv in _scratch)
            {
                if (previous != null &&
                    previous.TryGetValue(kv.Key, out float prev) &&
                    Math.Abs(kv.Value - prev) <= eps)
                    continue;

                _applyScratch.Add((kv.Key, kv.Value));
            }

            if (previous != null)
            {
                foreach (var kv in previous)
                {
                    if (_scratch.ContainsKey(kv.Key))
                        continue;

                    _clearScratch.Add(kv.Key);
                }
            }

            return new TileOcclusionPresentationDelta(_applyScratch, _clearScratch);
        }

        void AppendIncidentEdges(Vector3Int cell, List<TileData> appendTo)
        {
            for (int i = 0; i < CardinalNeighbors.Length; i++)
            {
                Vector3Int neighbor = cell + CardinalNeighbors[i];
                if (!_hub.TryGetEdgeBetween(cell, neighbor, out TileData edge))
                    continue;

                appendTo.Add(edge);
            }
        }

        void AppendIncidentFaces(Vector3Int cell, List<TileData> appendTo)
        {
            if (_hub.TryGetHorizontalFaceBetween(cell + Vector3Int.down, cell, out TileData belowFace))
                appendTo.Add(belowFace);

            if (_hub.TryGetHorizontalFaceBetween(cell, cell + Vector3Int.up, out TileData aboveFace))
                appendTo.Add(aboveFace);
        }

        void AccumulateOcclusion(
            in TileData tile,
            Vector3Int occupiedCell,
            Vector3 cameraWorld,
            Vector3 playerWorld,
            Vector3Int playerCell,
            int playerFloorCellY,
            bool isPlayerOutdoor,
            float cellSize,
            in SightLineBlendSettings settings,
            float eps)
        {
            if (!SightLineOcclusionStrength.PassesPlayerDownXQuadrantForOccluder(tile, occupiedCell, playerCell))
                return;

            if (SightLineOcclusionStrength.ShouldSkipProximityForIndoorStructural(isPlayerOutdoor, tile))
                return;

            if (SightLineOcclusionStrength.ShouldExemptFloor(tile, occupiedCell, playerCell, playerFloorCellY))
                return;

            Vector3 world = TileWorldPointUtil.GetOcclusionWorldPoint(tile.identity, occupiedCell, cellSize);
            float occ = SightLineOcclusionStrength.Evaluate(
                cameraWorld, playerWorld, world, cellSize, settings);

            if (occ <= eps)
                return;

            Guid id = tile.tileDefId;
            if (_scratch.TryGetValue(id, out float prevOcc))
                _scratch[id] = Math.Max(prevOcc, occ);
            else
                _scratch[id] = occ;
        }

    }
}

