using System;
using System.Collections.Generic;

namespace IsoTilemap
{
    /// <summary>화면에 스폰된 <see cref="TileView"/>만 조회합니다. GetComponent·전체 스캔 금지.</summary>
    public interface ITileViewRegistry
    {
        bool TryGetView(Guid tileId, out TileView view);

        void CollectSpawnedTileIds(List<Guid> into);
    }

    /// <summary>층 가시성 컨텍스트를 스폰된 뷰 presentation에 반영합니다.</summary>
    public interface IFloorVisibilitySync
    {
        void SyncFloorVisibility(in FloorVisibilityContext ctx);
    }
}
