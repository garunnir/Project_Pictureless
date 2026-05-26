using System;

namespace IsoTilemap
{
    /// <summary>화면에 스폰된 <see cref="TileView"/>만 조회합니다. GetComponent·전체 스캔 금지.</summary>
    public interface ITileViewRegistry
    {
        bool TryGetView(Guid tileId, out TileView view);
    }
}
