// ============================================================
// ProximitySightLineBlendPipeline — 시선 XZ 반경 내 타일 가림 강도 산출
// 실내 Wall·EdgeWall 포함. BFS와 병렬 evaluate — 합성 우선순위는 applier entry(BFS 100 > proximity 50).
// ============================================================
using System;
using System.Collections.Generic;
using UnityEngine;

namespace IsoTilemap
{
    public sealed class ProximitySightLineBlendPipeline
    {
        readonly TileMapCacheHub _hub;
        readonly HashSet<Vector3Int> _blendCells = new();
        readonly List<TileData> _cellTilesScratch = new();
        readonly Dictionary<Guid, float> _scratch = new();
        readonly List<(Guid tileId, float occlusion01)> _applyScratch = new();
        readonly List<Guid> _clearScratch = new();
        readonly List<ProximityEvaluatedHit> _evaluatedHitsScratch = new();
        readonly List<Vector3Int> _blendCellsListScratch = new();

        public ProximitySightLineBlendPipeline(TileMapCacheHub hub) =>
            _hub = hub ?? throw new ArgumentNullException(nameof(hub));

        public ProximityBlendEvaluationResult Evaluate(
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
            _evaluatedHitsScratch.Clear();
            _blendCellsListScratch.Clear();

            if (_hub == null)
            {
                return new ProximityBlendEvaluationResult(
                    new TileOcclusionPresentationDelta(_applyScratch, _clearScratch),
                    ProximityBlendEvaluationSnapshot.Empty);
            }

            SightLineSegmentSampler.CollectBlendCells(
                _hub, cameraWorld, playerWorld, playerCell, settings, _blendCells);

            foreach (Vector3Int cell in _blendCells)
                _blendCellsListScratch.Add(cell);

            float cellSize = settings.CellSize > 0f ? settings.CellSize : 1f;
            float eps = Mathf.Max(0f, settings.ApplyEpsilon);

            foreach (Vector3Int cell in _blendCells)
            {
                if (!SightLineOcclusionStrength.PassesPlayerDownXQuadrant(cell, playerCell))
                    continue;

                _cellTilesScratch.Clear();
                if (!_hub.TryCollectTilesAtOccupiedCell(cell, _cellTilesScratch))
                    continue;

                for (int i = 0; i < _cellTilesScratch.Count; i++)
                {
                    AccumulateOcclusion(
                        _cellTilesScratch[i], cell, cameraWorld, playerWorld,
                        playerCell, playerFloorCellY, cellSize, settings, eps);
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

            var snapshot = new ProximityBlendEvaluationSnapshot(
                _blendCellsListScratch,
                _evaluatedHitsScratch,
                cameraWorld,
                playerWorld);

            return new ProximityBlendEvaluationResult(
                new TileOcclusionPresentationDelta(_applyScratch, _clearScratch),
                snapshot);
        }

        void AccumulateOcclusion(
            in TileData tile,
            Vector3Int occupiedCell,
            Vector3 cameraWorld,
            Vector3 playerWorld,
            Vector3Int playerCell,
            int playerFloorCellY,
            float cellSize,
            in SightLineBlendSettings settings,
            float eps)
        {
            if (!SightLineOcclusionStrength.PassesPlayerDownXQuadrantForOccluder(tile, occupiedCell, playerCell))
                return;

            if (SightLineOcclusionStrength.ShouldExemptFloor(tile, occupiedCell, playerCell, playerFloorCellY))
                return;

            RecordEvaluatedHit(tile, occupiedCell);

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

        void RecordEvaluatedHit(in TileData tile, Vector3Int occupiedCell)
        {
            Guid id = tile.tileDefId;
            for (int i = 0; i < _evaluatedHitsScratch.Count; i++)
            {
                ProximityEvaluatedHit hit = _evaluatedHitsScratch[i];
                if (hit.Tile.tileDefId == id && hit.OccupiedCell == occupiedCell)
                    return;
            }

            _evaluatedHitsScratch.Add(new ProximityEvaluatedHit(tile, occupiedCell));
        }

    }
}

