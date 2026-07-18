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

    /// <summary>맵 타일 외부(컨테이너 등) TileView 조회용 registry 마커.</summary>
    public interface IExternalTileViewRegistry : ITileViewRegistry
    {
    }

    /// <summary>루팅 등 월드 타일 선택 하이라이트 sink.</summary>
    public interface ITileLootHighlightSink
    {
        void SetLootHighlight(Guid presentationTileId, bool highlighted);
        void ClearLootHighlight();
        bool IsLootHighlightActive(Guid presentationTileId);
    }
}
