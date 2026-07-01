// ============================================================
// StructuralVisibilityHiddenSetComputer — 후보 타일만 policy 판정해 구조물 숨김 집합 산출
// ============================================================
using System;
using System.Collections.Generic;

namespace IsoTilemap
{
    public sealed class StructuralVisibilityHiddenSetComputer
    {
        readonly PlayerFloorVisibilityPolicy _policy;
        readonly IMapModelReadOnly _model;

        public StructuralVisibilityHiddenSetComputer(
            PlayerFloorVisibilityPolicy policy,
            IMapModelReadOnly model)
        {
            _policy = policy ?? throw new ArgumentNullException(nameof(policy));
            _model = model ?? throw new ArgumentNullException(nameof(model));
        }

        public void Compute(
            in FloorVisibilityContext ctx,
            IReadOnlyCollection<Guid> candidates,
            HashSet<Guid> newHidden)
        {
            newHidden.Clear();
            if (candidates == null || candidates.Count == 0)
                return;

            foreach (Guid tileId in candidates)
            {
                if (!_model.TryGetTileById(tileId, out TileData tile))
                    continue;

                if (!_policy.IsTileVisible(tile, in ctx))
                    newHidden.Add(tileId);
            }
        }
    }
}
