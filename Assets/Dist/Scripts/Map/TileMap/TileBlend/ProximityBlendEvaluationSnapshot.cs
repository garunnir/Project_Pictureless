// ============================================================
// ProximityBlendEvaluationSnapshot — 근접 Evaluate가 본 셀·타일 (에드온 입력)
// ============================================================
using System;
using System.Collections.Generic;
using UnityEngine;

namespace IsoTilemap
{
    public readonly struct ProximityEvaluatedHit
    {
        public TileData Tile { get; }
        public Vector3Int OccupiedCell { get; }

        public ProximityEvaluatedHit(TileData tile, Vector3Int occupiedCell)
        {
            Tile = tile;
            OccupiedCell = occupiedCell;
        }
    }

    public readonly struct ProximityBlendEvaluationSnapshot
    {
        public static ProximityBlendEvaluationSnapshot Empty { get; } = new(
            Array.Empty<Vector3Int>(),
            Array.Empty<ProximityEvaluatedHit>(),
            Vector3.zero,
            Vector3.zero);

        public IReadOnlyList<Vector3Int> BlendCells { get; }
        public IReadOnlyList<ProximityEvaluatedHit> EvaluatedHits { get; }
        public Vector3 CameraWorld { get; }
        public Vector3 PlayerWorld { get; }

        public ProximityBlendEvaluationSnapshot(
            IReadOnlyList<Vector3Int> blendCells,
            IReadOnlyList<ProximityEvaluatedHit> evaluatedHits,
            Vector3 cameraWorld,
            Vector3 playerWorld)
        {
            BlendCells = blendCells ?? Array.Empty<Vector3Int>();
            EvaluatedHits = evaluatedHits ?? Array.Empty<ProximityEvaluatedHit>();
            CameraWorld = cameraWorld;
            PlayerWorld = playerWorld;
        }
    }

    public readonly struct ProximityBlendEvaluationResult
    {
        public TileOcclusionPresentationDelta Delta { get; }
        public ProximityBlendEvaluationSnapshot Snapshot { get; }

        public ProximityBlendEvaluationResult(
            TileOcclusionPresentationDelta delta,
            ProximityBlendEvaluationSnapshot snapshot)
        {
            Delta = delta;
            Snapshot = snapshot;
        }
    }
}
