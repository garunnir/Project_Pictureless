// ============================================================
// CompositeTileViewRegistry — 맵 타일 + 컨테이너 TileView 통합 조회
// ============================================================

using System;
using System.Collections.Generic;

namespace IsoTilemap
{
    public sealed class CompositeTileViewRegistry : ITileViewRegistry
    {
        readonly ITileViewRegistry _mapRegistry;
        readonly ContainerTileViewRegistry _containerRegistry;

        public CompositeTileViewRegistry(
            ITileViewRegistry mapRegistry,
            ContainerTileViewRegistry containerRegistry)
        {
            _mapRegistry = mapRegistry ?? throw new ArgumentNullException(nameof(mapRegistry));
            _containerRegistry = containerRegistry ?? throw new ArgumentNullException(nameof(containerRegistry));
        }

        public bool TryGetView(Guid tileId, out TileView view)
        {
            if (_mapRegistry.TryGetView(tileId, out view))
                return true;

            return _containerRegistry.TryGetView(tileId, out view);
        }

        public void CollectSpawnedTileIds(List<Guid> into)
        {
            if (into == null)
                return;

            _mapRegistry.CollectSpawnedTileIds(into);
            _containerRegistry.CollectTileIds(into);
        }
    }
}
