using System;
using System.Collections.Generic;

namespace IsoTilemap
{
    /// <summary>오클루전 BFS·거리 계산 결과. 모델은 <see cref="TileState"/>에 쓰지 않고 뷰 applier로만 전달합니다.</summary>
    public readonly struct TileOcclusionPresentationDelta
    {
        public readonly IReadOnlyList<(Guid tileId, float occlusion01)> ApplyEntries;
        public readonly IReadOnlyList<Guid> ClearIds;

        public TileOcclusionPresentationDelta(
            IReadOnlyList<(Guid tileId, float occlusion01)> applyEntries,
            IReadOnlyList<Guid> clearIds)
        {
            ApplyEntries = applyEntries ?? Array.Empty<(Guid, float)>();
            ClearIds = clearIds ?? Array.Empty<Guid>();
        }

        public bool IsEmpty => ApplyEntries.Count == 0 && ClearIds.Count == 0;
    }
}
